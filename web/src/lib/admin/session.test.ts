import { describe, expect, it, vi } from "vitest";
import { SessionExpiredError } from "./errors";
import { REFRESCO_ANTICIPADO_MS, SessionClient } from "./session";

const user = { id: "u1", email: "admin@local.test", nombre: "Admin", rol: "Admin" as const };

function json(status: number, body: unknown): Response {
  return new Response(body === undefined ? null : JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

function auth(token: string, expiresIn = 900) {
  return { accessToken: token, expiresIn, user };
}

/** Fake API: `tokenValido` is the only accepted Bearer; every refresh issues the next token. */
function crearApi({ refreshFalla = false, retrasoRefresh = 10 } = {}) {
  let tokenValido = "t0";
  let n = 0;
  const llamadas: string[] = [];
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    llamadas.push(url);
    if (url === "/api/auth/refresh") {
      await new Promise((r) => setTimeout(r, retrasoRefresh));
      if (refreshFalla) return json(401, { detail: "La sesión no es válida o ha expirado." });
      tokenValido = `t${++n}`;
      return json(200, auth(tokenValido));
    }
    if (url === "/api/auth/logout") return new Response(null, { status: 204 });
    const authorization = new Headers(init?.headers).get("Authorization");
    await new Promise((r) => setTimeout(r, 1));
    return authorization === `Bearer ${tokenValido}` ? json(200, { ok: true, url }) : json(401, null);
  });
  return {
    fetchMock,
    llamadas,
    refreshes: () => llamadas.filter((u) => u === "/api/auth/refresh").length,
    caducarToken: () => {
      tokenValido = `otro-${Math.random()}`;
    },
  };
}

function crearCliente(fetchMock: typeof fetch, extra: Partial<ConstructorParameters<typeof SessionClient>[0]> = {}) {
  return new SessionClient({ fetch: fetchMock, channel: null, setTimer: () => 0, clearTimer: () => undefined, ...extra });
}

describe("SessionClient.fetch", () => {
  it("fetch_ThreeSimultaneous401_RefreshesOnceAndRetriesEach", async () => {
    const api = crearApi();
    const cliente = crearCliente(api.fetchMock);
    await cliente.refresh(); // t1
    expect(api.refreshes()).toBe(1);
    api.caducarToken();

    const respuestas = await Promise.all([
      cliente.json<{ url: string }>("/api/admin/a"),
      cliente.json<{ url: string }>("/api/admin/b"),
      cliente.json<{ url: string }>("/api/admin/c"),
    ]);

    expect(respuestas.map((r) => r.url)).toEqual(["/api/admin/a", "/api/admin/b", "/api/admin/c"]);
    expect(api.refreshes()).toBe(2); // the initial one + a single shared one
  });

  it("fetch_WithoutSession_RefreshesWithTheCookieFirst", async () => {
    const api = crearApi();
    const cliente = crearCliente(api.fetchMock);

    await Promise.all([cliente.json("/api/admin/a"), cliente.json("/api/admin/b")]);

    expect(api.refreshes()).toBe(1);
    expect(cliente.actual?.user).toEqual(user);
  });

  it("fetch_RefreshFails_EndsTheSessionAndThrowsSessionExpired", async () => {
    const api = crearApi({ refreshFalla: true });
    const cliente = crearCliente(api.fetchMock);
    const motivos: string[] = [];
    cliente.subscribe((_, motivo) => motivos.push(motivo));

    await expect(cliente.json("/api/admin/a")).rejects.toBeInstanceOf(SessionExpiredError);
    expect(cliente.actual).toBeNull();
    expect(motivos).toContain("expirada");
  });

  it("fetch_SendsBearerAndNeverStoresTheToken", async () => {
    const api = crearApi();
    const cliente = crearCliente(api.fetchMock);
    const setItem = vi.spyOn(Storage.prototype, "setItem");

    await cliente.json("/api/admin/a");

    const [, init] = api.fetchMock.mock.calls.find(([url]) => String(url) === "/api/admin/a") ?? [];
    expect(new Headers(init?.headers).get("Authorization")).toBe("Bearer t1");
    expect(setItem).not.toHaveBeenCalled();
    expect(document.cookie).not.toContain("t1");
  });

  it("fetch_TokenRenewedWhileInFlight_RetriesWithoutAnotherRefresh", async () => {
    const api = crearApi();
    const cliente = crearCliente(api.fetchMock);
    await cliente.refresh(); // t1
    api.caducarToken();

    const primera = cliente.json("/api/admin/a"); // 401 → refresh (t2)
    await primera;
    const refreshesTrasPrimera = api.refreshes();
    await cliente.json("/api/admin/b"); // t2 is valid: no refresh

    expect(api.refreshes()).toBe(refreshesTrasPrimera);
  });
});

describe("SessionClient refresh", () => {
  it("refresh_ConcurrentCalls_ShareOneRequest", async () => {
    const api = crearApi();
    const cliente = crearCliente(api.fetchMock);

    const [a, b] = await Promise.all([cliente.refresh(), cliente.refresh()]);

    expect(a).toBe(b);
    expect(api.refreshes()).toBe(1);
  });

  it("refresh_RunsInsideTheCrossTabLock", async () => {
    const api = crearApi();
    const lock = vi.fn((fn: () => Promise<unknown>) => fn());
    const cliente = crearCliente(api.fetchMock, { lock: lock as <T>(fn: () => Promise<T>) => Promise<T> });

    await cliente.refresh();

    expect(lock).toHaveBeenCalledTimes(1);
  });

  it("establecer_SchedulesTheRefreshOneMinuteBeforeExpiry", async () => {
    const api = crearApi();
    const esperas: number[] = [];
    const cliente = crearCliente(api.fetchMock, {
      setTimer: (_cb, ms) => {
        esperas.push(ms);
        return 1;
      },
    });

    await cliente.refresh();

    expect(esperas).toEqual([900_000 - REFRESCO_ANTICIPADO_MS]);
  });

  it("proactiveTimer_Fires_RefreshesTheToken", async () => {
    const api = crearApi();
    let disparar: (() => void) | undefined;
    const cliente = crearCliente(api.fetchMock, {
      setTimer: (cb) => {
        disparar = cb;
        return 1;
      },
    });
    await cliente.refresh();

    disparar?.();
    await vi.waitFor(() => expect(cliente.actual?.accessToken).toBe("t2"));
    expect(api.refreshes()).toBe(2);
  });
});

describe("SessionClient logout", () => {
  it("logout_CallsTheApiClearsTheSessionAndTellsTheOtherTabs", async () => {
    const api = crearApi();
    const posted: unknown[] = [];
    const channel = { postMessage: (m: unknown) => posted.push(m), addEventListener: () => undefined, close: () => undefined };
    const cliente = crearCliente(api.fetchMock, { channel });
    await cliente.refresh();

    await cliente.logout();

    expect(api.llamadas).toContain("/api/auth/logout");
    expect(cliente.actual).toBeNull();
    expect(posted).toEqual([{ type: "logout" }]);
  });

  it("logoutMessageFromAnotherTab_EndsThisSessionWithoutCallingTheApi", async () => {
    const api = crearApi();
    let onMessage: ((e: MessageEvent) => void) | undefined;
    const channel = {
      postMessage: () => undefined,
      addEventListener: (_: "message", l: (e: MessageEvent) => void) => {
        onMessage = l;
      },
      close: () => undefined,
    };
    const cliente = crearCliente(api.fetchMock, { channel });
    await cliente.refresh();
    const motivos: string[] = [];
    cliente.subscribe((_, m) => motivos.push(m));

    onMessage?.(new MessageEvent("message", { data: { type: "logout" } }));

    expect(cliente.actual).toBeNull();
    expect(motivos).toEqual(["logout-remoto"]);
    expect(api.llamadas).not.toContain("/api/auth/logout");
  });
});
