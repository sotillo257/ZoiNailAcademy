import { test as base, expect, type APIRequestContext, type Page } from "@playwright/test";

export const ADMIN = {
  email: process.env.E2E_ADMIN_EMAIL ?? "admin@local.test",
  password: process.env.E2E_ADMIN_PASSWORD ?? "admin-dev-password",
};
export const SUPERADMIN = {
  email: process.env.E2E_SUPERADMIN_EMAIL ?? "superadmin@local.test",
  password: process.env.E2E_SUPERADMIN_PASSWORD ?? "superadmin-dev-password",
};

/**
 * Each test's browser gets its own client IP (X-Forwarded-For goes through the Next rewrite to the API), so the
 * per-IP rate limit of /api/auth/* does not throttle the suite.
 */
export const test = base.extend({
  context: async ({ context }, use) => {
    await context.setExtraHTTPHeaders({ "X-Forwarded-For": ip() });
    await use(context);
  },
});
export { expect };

/** A different client IP per call, so the API's per-IP rate limits do not throttle the suite. */
function ip(): string {
  return `10.${rand(255)}.${rand(255)}.${rand(254) + 1}`;
}
function rand(n: number): number {
  return Math.floor(Math.random() * n);
}
export function unico(): string {
  return `${Date.now().toString(36)}${rand(1e6).toString(36)}`;
}

export async function loginUi(page: Page, user = ADMIN): Promise<void> {
  await page.goto("/admin/login");
  await page.getByLabel("Email").fill(user.email);
  await page.getByLabel("Contraseña", { exact: true }).fill(user.password);
  await page.getByRole("button", { name: "Entrar" }).click();
  await expect(page).toHaveURL(/\/admin\/?$/);
}

export interface Edicion {
  id: string;
  cursoId: string;
  inicio: string;
  plazas: number;
  version: string;
}

/** Backoffice and public API calls made straight through the Next rewrite. */
export class Api {
  private constructor(
    private readonly request: APIRequestContext,
    private readonly token: string,
  ) {}

  static async login(request: APIRequestContext, user = ADMIN): Promise<Api> {
    const res = await request.post("/api/auth/login", { data: user, headers: { "X-Forwarded-For": ip() } });
    expect(res.status(), await res.text()).toBe(200);
    return new Api(request, ((await res.json()) as { accessToken: string }).accessToken);
  }

  private get auth() {
    return { Authorization: `Bearer ${this.token}` };
  }

  async primerCurso(): Promise<{ id: string; slug: string; nombre: string }> {
    const res = await this.request.get("/api/admin/cursos", { headers: this.auth });
    const cursos = (await res.json()) as { id: string; slug: string; nombre: string; activo: boolean }[];
    const curso = cursos.find((c) => c.activo);
    if (!curso) throw new Error("No hay cursos activos");
    return curso;
  }

  /** A future edition, on a random day so parallel runs do not collide. */
  async crearEdicion(plazas = 5): Promise<Edicion & { curso: { id: string; slug: string; nombre: string } }> {
    const curso = await this.primerCurso();
    const dia = new Date(Date.now() + (40 + rand(300)) * 86_400_000);
    dia.setUTCHours(9, rand(12) * 5, 0, 0);
    const res = await this.request.post("/api/admin/ediciones", {
      headers: this.auth,
      data: { cursoId: curso.id, inicio: dia.toISOString(), plazas, estado: "Programada", notas: `e2e ${unico()}` },
    });
    expect(res.status(), await res.text()).toBe(201);
    return { ...((await res.json()) as Edicion), curso };
  }

  /** The public manual booking (what the F08 page sends). */
  async reservarManual(edicionId: string): Promise<{ codigo: string; nombre: string }> {
    const id = unico();
    const nombre = `Prueba ${id}`;
    const res = await this.request.post("/api/reservas/manual", {
      headers: { "X-Forwarded-For": ip() },
      data: {
        edicionId,
        nombre,
        email: `e2e-${id}@example.com`,
        telefono: `6${String(rand(1e8)).padStart(8, "0")}`,
        aceptaCondiciones: true,
        website: "",
      },
    });
    expect(res.status(), await res.text()).toBe(200);
    return { codigo: ((await res.json()) as { codigo: string }).codigo, nombre };
  }

  async edicionesPublicas(slug: string): Promise<{ id: string }[]> {
    const res = await this.request.get(`/api/cursos/${slug}/ediciones`);
    expect(res.status()).toBe(200);
    return (await res.json()) as { id: string }[];
  }
}
