import { describe, expect, it } from "vitest";
import { ApiError } from "@/lib/admin/errors";
import { horasRestantes, textoDatosPago } from "@/components/sitio/reserva/estado-reserva";
import type { EdicionPublicaDto, ReservaPublicaDto } from "./api";
import { edicionInicial, estadoCurso, etiquetaEdicion, fechaCorta, fechaEdicion, plazasTexto } from "./ediciones";
import { errorDeFormulario, errorDeReserva } from "./errores";
import { contactoEsquema, listaEsperaEsquema, mensajeConAsunto, pareceTelefono, reservaEsquema } from "./esquemas";

const ed = (id: string, plazasLibres: number, estado: EdicionPublicaDto["estado"], inicio = "2027-01-18T08:00:00Z"): EdicionPublicaDto => ({
  id,
  inicio,
  plazasTotales: 10,
  plazasLibres,
  estado,
});

describe("fechas de las ediciones (Madrid)", () => {
  it("muestra la hora de Madrid en invierno (+01:00) y en verano (+02:00)", () => {
    expect(fechaEdicion("2027-01-18T08:00:00Z")).toBe("lun 18 ene 2027, 09:00");
    expect(fechaEdicion("2027-07-05T08:00:00Z")).toBe("lun 5 jul 2027, 10:00");
  });

  it("respeta los cambios de hora de marzo y octubre", () => {
    // 28 Mar 2027: 02:00 → 03:00. 01:30 UTC is already summer time.
    expect(fechaEdicion("2027-03-28T00:30:00Z")).toBe("dom 28 mar 2027, 01:30");
    expect(fechaEdicion("2027-03-28T01:30:00Z")).toBe("dom 28 mar 2027, 03:30");
    // 31 Oct 2027: 03:00 → 02:00.
    expect(fechaEdicion("2027-10-31T00:30:00Z")).toBe("dom 31 oct 2027, 02:30");
    expect(fechaEdicion("2027-10-31T01:30:00Z")).toBe("dom 31 oct 2027, 02:30");
  });

  it("usa el día de Madrid aunque en UTC aún sea el anterior", () => {
    expect(fechaCorta("2027-02-07T23:30:00Z")).toBe("8 feb");
  });
});

describe("estados del calendario", () => {
  it("describe las plazas de cada estado", () => {
    expect(plazasTexto(ed("a", 6, "Disponible"))).toBe("6 plazas");
    expect(plazasTexto(ed("a", 2, "UltimasPlazas"))).toBe("¡Últimas 2 plazas!");
    expect(plazasTexto(ed("a", 1, "UltimasPlazas"))).toBe("¡Última plaza!");
    expect(plazasTexto(ed("a", 0, "Completo"))).toBe("Completo");
    expect(etiquetaEdicion(ed("a", 6, "Disponible"))).toBe("18 ene 2027, 09:00 · 6 plazas");
  });

  it("la insignia del curso es el mejor estado de sus fechas", () => {
    expect(estadoCurso([])).toBe("SinFechas");
    expect(estadoCurso([ed("a", 0, "Completo"), ed("b", 1, "UltimasPlazas")])).toBe("UltimasPlazas");
    expect(estadoCurso([ed("a", 1, "UltimasPlazas"), ed("b", 5, "Disponible")])).toBe("Disponible");
    expect(estadoCurso([ed("a", 0, "Completo")])).toBe("Completo");
  });

  it("preselecciona la fecha de la URL si se puede reservar; si no, la primera con plazas", () => {
    const ediciones = [ed("llena", 0, "Completo"), ed("libre", 4, "Disponible"), ed("otra", 3, "Disponible")];
    expect(edicionInicial(ediciones, "otra")).toBe("otra");
    expect(edicionInicial(ediciones, "llena")).toBe("libre");
    expect(edicionInicial(ediciones, "inventada")).toBe("libre");
    expect(edicionInicial([ed("llena", 0, "Completo")])).toBe("");
  });
});

describe("errores de la reserva por code", () => {
  const problema = (status: number, cuerpo: Record<string, unknown>) => new ApiError(status, cuerpo);

  it("sin_plazas recarga las fechas y ofrece la lista de espera", () => {
    const e = errorDeReserva(problema(409, { code: "sin_plazas" }));
    expect(e).toMatchObject({ mensaje: "Alguien ha reservado la última plaza justo ahora.", recargarFechas: true, ofrecerListaEspera: true });
  });

  it("edicion_no_reservable usa el mensaje del backend y recarga", () => {
    const e = errorDeReserva(problema(409, { code: "edicion_no_reservable", detail: "Para esta fecha solo se puede pagar con tarjeta" }));
    expect(e).toMatchObject({ mensaje: "Para esta fecha solo se puede pagar con tarjeta", recargarFechas: true });
  });

  it("limite_pendientes, 429 y errores de red", () => {
    expect(errorDeReserva(problema(409, { code: "limite_pendientes" })).mensaje).toMatch(/reservas pendientes de pago/);
    expect(errorDeReserva(problema(429, {})).mensaje).toBe("Demasiados intentos, espera unos minutos.");
    expect(errorDeReserva(new TypeError("fetch failed")).mensaje).toMatch(/No hay conexión/);
  });

  it("un 400 lleva cada error a su campo en camelCase", () => {
    const e = errorDeReserva(problema(400, { errors: { Telefono: ["El teléfono no es válido."], AceptaCondiciones: ["Acepta"] } }));
    expect(e.campos).toEqual({ telefono: "El teléfono no es válido.", aceptaCondiciones: "Acepta" });
    expect(errorDeFormulario(problema(400, { errors: { Mensaje: ["Corto"] } })).campos).toEqual({ mensaje: "Corto" });
  });
});

describe("validaciones de los formularios", () => {
  const reserva = { edicionId: "e1", nombre: "Lucía", telefono: "612 345 678", email: "lucia@mail.com", metodo: "Manual", aceptaCondiciones: true };

  it("acepta una reserva completa y rechaza la que no acepta las condiciones", () => {
    expect(reservaEsquema.safeParse(reserva).success).toBe(true);
    expect(reservaEsquema.safeParse({ ...reserva, aceptaCondiciones: false }).success).toBe(false);
    expect(reservaEsquema.safeParse({ ...reserva, email: "no-es-email" }).success).toBe(false);
  });

  it("teléfonos españoles e internacionales", () => {
    expect(pareceTelefono("612 345 678")).toBe(true);
    expect(pareceTelefono("+593 99 123 4567")).toBe(true);
    expect(pareceTelefono("0034612345678")).toBe(true);
    expect(pareceTelefono("1234")).toBe(false);
  });

  it("contacto: teléfono opcional, mensaje de al menos 10 caracteres y el asunto delante", () => {
    const base = { nombre: "Lucía", telefono: "", email: "l@m.com", asunto: "Academia online", mensaje: "Hola, una duda", aceptaPrivacidad: true };
    expect(contactoEsquema.safeParse(base).success).toBe(true);
    expect(contactoEsquema.safeParse({ ...base, mensaje: "Hola" }).success).toBe(false);
    expect(mensajeConAsunto("Academia online", " Hola, una duda ")).toBe("[Academia online] Hola, una duda");
  });

  it("lista de espera exige el consentimiento", () => {
    const base = { cursoId: "c1", nombre: "Lucía", telefono: "612345678", email: "l@m.com", fechaPreferida: "", aceptaPrivacidad: true };
    expect(listaEsperaEsquema.safeParse(base).success).toBe(true);
    expect(listaEsperaEsquema.safeParse({ ...base, aceptaPrivacidad: false }).success).toBe(false);
  });
});

describe("instrucciones de pago", () => {
  const reserva: ReservaPublicaDto = {
    codigo: "CUR-7KQ4XM",
    estado: "Pendiente",
    metodo: "Manual",
    curso: "Experto de Salón",
    inicio: "2027-01-18T08:00:00Z",
    importe: 150,
    precioCurso: 590,
    expiraEn: "2027-01-03T10:00:00Z",
    datosPago: { titular: "Zoila", iban: "ES12 3456", bizumTelefono: "600111222", concepto: "CUR-7KQ4XM" },
    whatsappUrl: "https://wa.me/34600111222?text=x",
  };

  it("cuenta las horas que le quedan a la plaza", () => {
    expect(horasRestantes("2027-01-03T10:00:00Z", Date.parse("2027-01-01T10:00:00Z"))).toBe(48);
    expect(horasRestantes("2027-01-03T10:00:00Z", Date.parse("2027-01-04T10:00:00Z"))).toBe(0);
  });

  it("copia la señal y el código como concepto", () => {
    const texto = textoDatosPago(reserva);
    expect(texto).toContain("Importe: 150,00");
    expect(texto).toContain("IBAN: ES12 3456");
    expect(texto).toContain("Concepto: CUR-7KQ4XM");
  });
});
