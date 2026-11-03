import { readFile } from "node:fs/promises";
import { Api, ADMIN, expect, loginUi, SUPERADMIN, test } from "./helpers";

test("1. login → crear edición → aparece en la web pública", async ({ page, request }) => {
  const api = await Api.login(request);
  const curso = await api.primerCurso();
  await loginUi(page);

  await page.goto("/admin/ediciones");
  await page.getByRole("link", { name: "Nueva fecha" }).click();
  await expect(page.getByRole("heading", { name: "Nueva fecha" })).toBeVisible();
  await page.getByLabel("Curso").selectOption(curso.id);
  const dia = new Date(Date.now() + (60 + Math.floor(Math.random() * 200)) * 86_400_000).toISOString().slice(0, 10);
  await page.getByLabel("Fecha", { exact: true }).fill(dia);
  await page.getByLabel("Hora (Madrid)").fill("10:00");
  await page.getByLabel("Plazas").fill("6");
  await page.getByRole("button", { name: "Crear fecha" }).click();

  await expect(page.getByText("Fecha creada y publicada")).toBeVisible();
  await expect(page).toHaveURL(/\/admin\/ediciones\/[0-9a-f-]{36}$/);
  const id = page.url().split("/").pop();
  // Until F08 exists, "the public site" is the public API it renders from.
  expect((await api.edicionesPublicas(curso.slug)).map((e) => e.id)).toContain(id);
});

test("2. reserva manual → pendientes → buscar en minúsculas sin guion → confirmar → inscritos", async ({ page, request }) => {
  const api = await Api.login(request);
  const edicion = await api.crearEdicion();
  const reserva = await api.reservarManual(edicion.id);
  await loginUi(page);

  await page.goto("/admin/reservas");
  await expect(page.locator(`[data-codigo="${reserva.codigo}"]`)).toBeVisible();

  await page.goto("/admin");
  await page.getByLabel("Buscar reserva por código, nombre o teléfono").fill(reserva.codigo.toLowerCase().replace(/-/g, ""));
  await page.keyboard.press("Enter");
  await expect(page).toHaveURL(/\/admin\/reservas\?q=/);
  const tarjeta = page.locator(`[data-codigo="${reserva.codigo}"]`);
  await expect(tarjeta).toBeVisible();

  await tarjeta.getByRole("button", { name: "Confirmar pago" }).click();
  const dialogo = page.getByRole("dialog");
  await expect(dialogo).toContainText(reserva.codigo);
  await dialogo.getByRole("button", { name: /Sí, confirmar/ }).click();
  await expect(page.getByText("Pago confirmado, se ha enviado el email")).toBeVisible();
  await expect(tarjeta).toHaveCount(0);

  await page.goto(`/admin/ediciones/${edicion.id}`);
  await expect(page.getByTestId("inscrito").filter({ hasText: reserva.nombre }).locator("visible=true")).toBeVisible();
});

test("3. bajar plazas por debajo de las ocupadas → error en el campo", async ({ page, request }) => {
  const api = await Api.login(request);
  const edicion = await api.crearEdicion(4);
  await api.reservarManual(edicion.id);
  await api.reservarManual(edicion.id);
  await loginUi(page);

  await page.goto(`/admin/ediciones/${edicion.id}/editar`);
  await page.getByLabel("Plazas").fill("1");
  await page.getByRole("button", { name: "Guardar cambios" }).click();

  await expect(page.getByLabel("Plazas")).toHaveAttribute("aria-invalid", "true");
  await expect(page.getByText("Hay 2 plazas ocupadas: el mínimo es 2.")).toBeVisible();
});

test("4. cambiar la fecha con inscritos → diálogo de confirmación", async ({ page, request }) => {
  const api = await Api.login(request);
  const edicion = await api.crearEdicion();
  await api.reservarManual(edicion.id);
  await loginUi(page);

  await page.goto(`/admin/ediciones/${edicion.id}/editar`);
  await page.getByLabel("Hora (Madrid)").fill("17:30");
  await page.getByRole("button", { name: "Guardar cambios" }).click();

  const dialogo = page.getByRole("dialog");
  await expect(dialogo).toContainText("Esta edición tiene 1 inscritos, tendrás que avisarles.");
  await dialogo.getByRole("button", { name: "Sí, cambiar la fecha" }).click();
  await expect(page.getByText("Fecha guardada")).toBeVisible();
  await expect(page).toHaveURL(new RegExp(`/admin/ediciones/${edicion.id}$`));
  await expect(page.getByText(/17:30/).first()).toBeVisible();
});

test("5. descargar CSV de asistentes", async ({ page, request }) => {
  const api = await Api.login(request);
  const edicion = await api.crearEdicion();
  await loginUi(page);

  await page.goto(`/admin/ediciones/${edicion.id}`);
  const [descarga] = await Promise.all([
    page.waitForEvent("download"),
    page.getByRole("button", { name: "Descargar asistentes (CSV)" }).click(),
  ]);
  expect(descarga.suggestedFilename()).toMatch(/\.csv$/);
  const ruta = await descarga.path();
  const contenido = await readFile(ruta);
  expect(contenido.subarray(0, 3)).toEqual(Buffer.from([0xef, 0xbb, 0xbf])); // BOM for Excel
});

test("6. recargar sigue logueado; logout en una pestaña cierra la otra", async ({ page, context }) => {
  await loginUi(page);
  await page.goto("/admin/reservas");
  await page.reload();
  await expect(page).toHaveURL(/\/admin\/reservas$/);
  await expect(page.getByRole("heading", { name: "Pendientes de pago" })).toBeVisible();

  // The access token never touches browser storage.
  const almacenado = await page.evaluate(() => JSON.stringify({ ...localStorage }) + JSON.stringify({ ...sessionStorage }));
  expect(almacenado).toBe("{}{}");
  expect(await page.evaluate(() => document.cookie)).toBe("");

  const otra = await context.newPage();
  await otra.goto("/admin/ediciones");
  await expect(otra.getByRole("heading", { name: "Fechas" })).toBeVisible();

  // Logout from the first tab (sidebar on desktop, menu on a phone).
  const menu = page.getByRole("button", { name: "Más opciones" });
  if (await menu.isVisible()) await menu.click();
  await page.getByRole("button", { name: "Salir" }).locator("visible=true").click();
  await expect(page).toHaveURL(/\/admin\/login$/);
  await expect(otra).toHaveURL(/\/admin\/login/);
});

test("7. Admin no ve Cursos y /admin/cursos muestra «Sin permiso» (la API responde 403)", async ({ page, request }) => {
  const superApi = await Api.login(request, SUPERADMIN);
  const curso = await superApi.primerCurso();
  await loginUi(page, ADMIN);

  await expect(page.getByRole("link", { name: "Cursos" })).toHaveCount(0);
  await page.goto("/admin/cursos");
  await expect(page.getByRole("heading", { name: "Sin permiso" })).toBeVisible();

  const respuesta = page.waitForResponse((r) => r.url().endsWith(`/api/admin/cursos/${curso.id}`));
  await page.goto(`/admin/cursos/${curso.id}/editar`);
  expect((await respuesta).status()).toBe(403);
  await expect(page.getByRole("heading", { name: "Sin permiso" })).toBeVisible();
});

test("SuperAdmin ve Cursos y Emails", async ({ page }) => {
  await loginUi(page, SUPERADMIN);
  const menu = page.getByRole("button", { name: "Más opciones" });
  if (await menu.isVisible()) await menu.click();
  await expect(page.getByRole("link", { name: "Cursos" }).locator("visible=true")).toBeVisible();
  await page.goto("/admin/emails");
  await expect(page.frameLocator("iframe").locator("body")).toContainText(/CUR-7KQ4XM|reserva/i);
});

test("todas las pantallas sin scroll horizontal", async ({ page, request }) => {
  const api = await Api.login(request, SUPERADMIN);
  const edicion = await api.crearEdicion();
  await api.reservarManual(edicion.id);
  await loginUi(page, SUPERADMIN);
  for (const ruta of [
    "/admin",
    "/admin/reservas",
    "/admin/ediciones",
    "/admin/ediciones/nueva",
    `/admin/ediciones/${edicion.id}`,
    `/admin/ediciones/${edicion.id}/editar`,
    "/admin/lista-espera",
    "/admin/cursos",
    `/admin/cursos/${edicion.curso.id}/editar`,
    "/admin/cuenta",
    "/admin/emails",
  ]) {
    await page.goto(ruta);
    await page.waitForLoadState("networkidle");
    const desborde = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(desborde, ruta).toBeLessThanOrEqual(0);
  }
});
