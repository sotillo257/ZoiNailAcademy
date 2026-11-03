import { describe, expect, it } from "vitest";
import { formatearImporte, telefonoInternacional, whatsappUrl } from "./formato";
import { destinoSeguro } from "./admin/next";
import { nombreArchivo } from "./admin/descargar";

describe("telefonoInternacional", () => {
  it.each([
    ["612 345 678", "34612345678"],
    ["+34 612 345 678", "34612345678"],
    ["0034612345678", "34612345678"],
    ["+58 412 1234567", "584121234567"],
  ])("telefonoInternacional_%s_Returns%s", (telefono, esperado) => {
    expect(telefonoInternacional(telefono)).toBe(esperado);
  });

  it("whatsappUrl_WithMessage_EncodesIt", () => {
    expect(whatsappUrl("612345678", "Hola, CUR-7KQ4XM")).toBe("https://wa.me/34612345678?text=Hola%2C%20CUR-7KQ4XM");
  });

  it("formatearImporte_Euros", () => {
    expect(formatearImporte(149).replace(/\s/g, " ")).toBe("149,00 €");
  });
});

describe("destinoSeguro", () => {
  it.each([
    [null, "/admin"],
    ["/admin/reservas?q=abc", "/admin/reservas?q=abc"],
    ["https://evil.example", "/admin"],
    ["//evil.example/admin", "/admin"],
    ["/admin/login?next=/admin", "/admin"],
    ["/cursos", "/admin"],
  ])("destinoSeguro_%s_Returns%s", (next, esperado) => {
    expect(destinoSeguro(next)).toBe(esperado);
  });
});

describe("nombreArchivo", () => {
  it("nombreArchivo_PrefersTheUtf8Name", () => {
    expect(nombreArchivo(`attachment; filename=asistentes.csv; filename*=UTF-8''asistentes-u%C3%B1as.csv`)).toBe("asistentes-uñas.csv");
    expect(nombreArchivo(`attachment; filename="lista.csv"`)).toBe("lista.csv");
    expect(nombreArchivo(null)).toBeUndefined();
  });
});
