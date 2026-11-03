import { Api, expect, test, unico } from "./helpers";

/** F08: the public web against the running API (Development seed: 4 courses with a seat deposit). */

function telefono(): string {
  return `6${String(Math.floor(Math.random() * 1e8)).padStart(8, "0")}`;
}

test("reserva con transferencia/Bizum hasta las instrucciones; WhatsApp lleva el código", async ({ page, request }) => {
  const api = await Api.login(request);
  const edicion = await api.crearEdicion();

  await page.goto(`/reservar/${edicion.curso.slug}?edicion=${edicion.id}`);
  await expect(page.getByRole("heading", { name: edicion.curso.nombre })).toBeVisible();
  await expect(page.getByLabel("Edición")).toHaveValue(edicion.id);
  await page.getByLabel("Nombre y apellidos").fill(`Prueba ${unico()}`);
  await page.getByRole("textbox", { name: "WhatsApp" }).fill(telefono());
  await page.getByLabel("Email").fill(`e2e-${unico()}@example.com`);
  await page.getByRole("radio", { name: /Transferencia o Bizum/ }).check();
  await page.getByLabel(/Acepto las condiciones de reserva/).check();
  await page.getByRole("button", { name: "Continuar" }).click();

  await expect(page).toHaveURL(/\/reserva\/CUR-[A-Z0-9]{6}$/);
  const codigo = page.url().split("/").pop()!;
  await expect(page.getByRole("heading", { name: "Tu plaza está pre-reservada" })).toBeVisible();
  await expect(page.getByText(codigo, { exact: true })).toBeVisible();
  const whatsapp = page.getByRole("link", { name: /Enviar comprobante por WhatsApp/ });
  expect(decodeURIComponent((await whatsapp.getAttribute("href")) ?? "")).toContain(codigo);
});

test("formulario vacío: errores en cada campo sin llamar a la API", async ({ page, request }) => {
  const api = await Api.login(request);
  const edicion = await api.crearEdicion();
  await page.goto(`/reservar/${edicion.curso.slug}`);
  await page.getByRole("button", { name: "Continuar" }).click();
  await expect(page.getByText("Escribe tu nombre.")).toBeVisible();
  await expect(page.getByText("Debes aceptar las condiciones de reserva y la política de privacidad.")).toBeVisible();
  await expect(page).toHaveURL(new RegExp(`/reservar/${edicion.curso.slug}`));
});

test("reserva con tarjeta hasta Stripe Checkout", async ({ page, request }) => {
  test.skip(!process.env.E2E_STRIPE, "Necesita la API con claves de Stripe en modo test (E2E_STRIPE=1).");
  const api = await Api.login(request);
  const edicion = await api.crearEdicion();
  await page.goto(`/reservar/${edicion.curso.slug}?edicion=${edicion.id}`);
  await page.getByLabel("Nombre y apellidos").fill(`Prueba ${unico()}`);
  await page.getByRole("textbox", { name: "WhatsApp" }).fill(telefono());
  await page.getByLabel("Email").fill(`e2e-${unico()}@example.com`);
  await page.getByLabel(/Acepto las condiciones de reserva/).check();
  await page.getByRole("button", { name: "Continuar" }).click();
  await expect(page).toHaveURL(/checkout\.stripe\.com/, { timeout: 20_000 });
});

test("lista de espera desde Formaciones con el curso preseleccionado", async ({ page, request }) => {
  const api = await Api.login(request);
  const curso = await api.primerCurso();

  await page.goto(`/formaciones?curso=${curso.slug}#lista-espera`);
  const form = page.locator("#lista-espera");
  await expect(form.getByLabel("¿Qué formación te interesa?")).toHaveValue(curso.id);
  await form.getByLabel("Nombre y apellidos").fill(`Prueba ${unico()}`);
  await form.getByRole("textbox", { name: "WhatsApp" }).fill(telefono());
  await form.getByRole("textbox", { name: "Email", exact: true }).fill(`e2e-${unico()}@example.com`);
  await form.getByLabel(/Acepto la política de privacidad/).check();
  await form.getByRole("button", { name: "Quiero entrar en la lista" }).click();
  await expect(form.getByText("¡Ya estás en la lista!")).toBeVisible();
});

test("contacto", async ({ page }) => {
  await page.goto("/contacto");
  const form = page.locator("form");
  await form.getByLabel("Nombre").fill(`Prueba ${unico()}`);
  await form.getByLabel("Email").fill(`e2e-${unico()}@example.com`);
  await form.getByLabel("¿Sobre qué nos escribes?").selectOption("Academia online");
  await form.getByLabel("Mensaje").fill("Hola, quería saber cuándo empieza el próximo curso.");
  await form.getByLabel(/Acepto la política de privacidad/).check();
  await form.getByRole("button", { name: "Enviar mensaje" }).click();
  await expect(page.getByText("¡Mensaje enviado!")).toBeVisible();
});

test("las páginas públicas no tienen scroll horizontal", async ({ page }) => {
  for (const ruta of ["/", "/formaciones", "/contacto", "/condiciones", "/ruta-que-no-existe"]) {
    await page.goto(ruta);
    const ancho = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
    expect(ancho, ruta).toBeLessThanOrEqual(0);
  }
});
