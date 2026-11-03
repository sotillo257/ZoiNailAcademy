import { ApiError, SessionExpiredError } from "./errors";
import type { LoginResponse, UserDto } from "./types";

/**
 * Backoffice session in the browser (F09).
 *
 * - The access token lives only in this object's memory: never in localStorage/sessionStorage or a cookie. The
 *   refresh token is the API's `rt` cookie (HttpOnly), which the browser sends by itself to /api/auth/*.
 * - One refresh in flight at a time (single-flight), shared by every request that gets a 401; each request is
 *   retried once with the new token. The API rotates the refresh token and treats a replayed one as theft (it
 *   closes every session), so two refreshes with the same cookie must never race: inside a tab the shared
 *   promise prevents it, across tabs a Web Lock serializes them.
 * - Proactive refresh one minute before `expiresIn`.
 * - Logout is propagated to the other tabs with BroadcastChannel('auth').
 */

export interface Sesion {
  readonly accessToken: string;
  /** Epoch ms. */
  readonly expiresAt: number;
  readonly user: UserDto;
}

export type MotivoCambio = "login" | "refresh" | "logout" | "logout-remoto" | "expirada";
export type SessionListener = (sesion: Sesion | null, motivo: MotivoCambio) => void;

interface CanalAuth {
  postMessage(message: unknown): void;
  addEventListener(type: "message", listener: (event: MessageEvent) => void): void;
  close(): void;
}

export interface SessionClientOptions {
  fetch?: typeof fetch;
  now?: () => number;
  setTimer?: (callback: () => void, ms: number) => unknown;
  clearTimer?: (handle: unknown) => void;
  /** Cross-tab channel; `null` disables it. Defaults to BroadcastChannel('auth') when available. */
  channel?: CanalAuth | null;
  /** Runs `fn` while holding a lock shared by every tab. Defaults to the Web Locks API when available. */
  lock?: <T>(fn: () => Promise<T>) => Promise<T>;
}

/** Proactive refresh this long before the access token expires. */
export const REFRESCO_ANTICIPADO_MS = 60_000;
const REFRESCO_MINIMO_MS = 5_000;
const LOCK_NAME = "zoi-auth-refresh";

export class SessionClient {
  private sesion: Sesion | null = null;
  private refreshEnVuelo: Promise<Sesion | null> | null = null;
  private timer: unknown = null;
  private readonly listeners = new Set<SessionListener>();
  private readonly fetchImpl: typeof fetch;
  private readonly now: () => number;
  private readonly setTimer: (callback: () => void, ms: number) => unknown;
  private readonly clearTimer: (handle: unknown) => void;
  private readonly channel: CanalAuth | null;
  private readonly lock: <T>(fn: () => Promise<T>) => Promise<T>;

  constructor(options: SessionClientOptions = {}) {
    this.fetchImpl = options.fetch ?? ((input, init) => globalThis.fetch(input, init));
    this.now = options.now ?? (() => Date.now());
    this.setTimer = options.setTimer ?? ((callback, ms) => setTimeout(callback, ms));
    this.clearTimer = options.clearTimer ?? ((handle) => clearTimeout(handle as ReturnType<typeof setTimeout>));
    this.channel = options.channel === undefined ? crearCanal() : options.channel;
    this.lock = options.lock ?? webLock;
    this.channel?.addEventListener("message", (event) => {
      if (isLogoutMessage(event.data) && this.sesion) this.terminar("logout-remoto");
    });
  }

  get actual(): Sesion | null {
    return this.sesion;
  }

  subscribe(listener: SessionListener): () => void {
    this.listeners.add(listener);
    return () => this.listeners.delete(listener);
  }

  async login(email: string, password: string): Promise<Sesion> {
    const res = await this.fetchImpl("/api/auth/login", {
      method: "POST",
      credentials: "same-origin",
      cache: "no-store",
      headers: { "Content-Type": "application/json", Accept: "application/json" },
      body: JSON.stringify({ email, password }),
    });
    if (!res.ok) throw new ApiError(res.status, await leerJson(res));
    return this.establecer((await res.json()) as LoginResponse, "login");
  }

  /**
   * Gets a new access token with the `rt` cookie. Resolves `null` when the session is no longer valid (and ends it);
   * rejects on network errors or unexpected answers (e.g. 429), which do not end the session.
   */
  refresh(): Promise<Sesion | null> {
    if (!this.refreshEnVuelo) {
      this.refreshEnVuelo = this.lock(() => this.pedirRefresh()).finally(() => {
        this.refreshEnVuelo = null;
      });
    }
    return this.refreshEnVuelo;
  }

  async logout(): Promise<void> {
    try {
      await this.fetchImpl("/api/auth/logout", { method: "POST", credentials: "same-origin", cache: "no-store" });
    } catch {
      // The local session ends anyway; the cookie expires on its own.
    }
    this.terminar("logout");
    this.channel?.postMessage({ type: "logout" });
  }

  /**
   * `fetch` for the backoffice API: adds the Bearer token, and on a 401 refreshes once (shared with every other
   * request) and retries once. Throws SessionExpiredError when the session cannot be renewed.
   */
  async fetch(path: string, init: RequestInit = {}): Promise<Response> {
    if (this.refreshEnVuelo) await this.refreshEnVuelo.catch(() => null);

    let token = this.sesion?.accessToken;
    if (!token) {
      const renovada = await this.refresh();
      if (!renovada) throw new SessionExpiredError();
      token = renovada.accessToken;
    }

    const res = await this.enviar(path, init, token);
    if (res.status !== 401) return res;

    // Another request may have already renewed the token while this one was on its way.
    const actual = this.sesion;
    const renovada = actual && actual.accessToken !== token ? actual : await this.refresh();
    if (!renovada) throw new SessionExpiredError();

    const reintento = await this.enviar(path, init, renovada.accessToken);
    if (reintento.status === 401) {
      this.terminar("expirada");
      throw new SessionExpiredError();
    }
    return reintento;
  }

  /** JSON helper over `fetch`: throws ApiError on any non-2xx answer; resolves `undefined` on 204. */
  async json<T>(path: string, init: RequestInit = {}): Promise<T> {
    const res = await this.fetch(path, init);
    if (!res.ok) throw new ApiError(res.status, await leerJson(res));
    if (res.status === 204) return undefined as T;
    return (await res.json()) as T;
  }

  /** Stops timers and listeners (tests, hot reload). */
  dispose(): void {
    this.cancelarTimer();
    this.listeners.clear();
    this.channel?.close();
  }

  private async pedirRefresh(): Promise<Sesion | null> {
    const res = await this.fetchImpl("/api/auth/refresh", {
      method: "POST",
      credentials: "same-origin",
      cache: "no-store",
      headers: { Accept: "application/json" },
    });
    if (res.ok) return this.establecer((await res.json()) as LoginResponse, "refresh");
    if (res.status === 401 || res.status === 400) {
      this.terminar("expirada");
      return null;
    }
    throw new ApiError(res.status, await leerJson(res));
  }

  private enviar(path: string, init: RequestInit, token: string): Promise<Response> {
    const headers = new Headers(init.headers);
    headers.set("Authorization", `Bearer ${token}`);
    if (!headers.has("Accept")) headers.set("Accept", "application/json");
    if (typeof init.body === "string" && !headers.has("Content-Type")) headers.set("Content-Type", "application/json");
    return this.fetchImpl(path, { ...init, headers, cache: "no-store", credentials: "same-origin" });
  }

  private establecer(respuesta: LoginResponse, motivo: MotivoCambio): Sesion {
    const sesion: Sesion = {
      accessToken: respuesta.accessToken,
      expiresAt: this.now() + respuesta.expiresIn * 1000,
      user: respuesta.user,
    };
    this.sesion = sesion;
    this.programarRefresco(respuesta.expiresIn * 1000);
    this.notificar(motivo);
    return sesion;
  }

  private programarRefresco(vidaMs: number): void {
    this.cancelarTimer();
    const espera = Math.max(vidaMs - REFRESCO_ANTICIPADO_MS, Math.min(REFRESCO_MINIMO_MS, vidaMs / 2));
    this.timer = this.setTimer(() => {
      this.timer = null;
      // A failure here is not fatal: the next request gets a 401 and refreshes again.
      this.refresh().catch(() => undefined);
    }, espera);
  }

  private terminar(motivo: MotivoCambio): void {
    this.cancelarTimer();
    this.sesion = null;
    this.notificar(motivo);
  }

  private cancelarTimer(): void {
    if (this.timer !== null) {
      this.clearTimer(this.timer);
      this.timer = null;
    }
  }

  private notificar(motivo: MotivoCambio): void {
    for (const listener of [...this.listeners]) listener(this.sesion, motivo);
  }
}

async function leerJson(res: Response): Promise<unknown> {
  try {
    return await res.json();
  } catch {
    return null;
  }
}

function isLogoutMessage(data: unknown): boolean {
  return typeof data === "object" && data !== null && (data as { type?: unknown }).type === "logout";
}

function crearCanal(): CanalAuth | null {
  return typeof BroadcastChannel === "undefined" ? null : new BroadcastChannel("auth");
}

function webLock<T>(fn: () => Promise<T>): Promise<T> {
  const locks = typeof navigator === "undefined" ? undefined : navigator.locks;
  return locks ? (locks.request(LOCK_NAME, fn) as Promise<T>) : fn();
}

let instancia: SessionClient | null = null;

/** The tab's single session (one in-flight refresh per tab needs one instance). */
export function getSessionClient(): SessionClient {
  instancia ??= new SessionClient();
  return instancia;
}
