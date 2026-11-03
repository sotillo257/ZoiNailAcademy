import { ApiError } from "@/lib/admin/errors";

/** What the public forms do with an API error: a message, field errors, and whether to reload the dates. */
export interface ErrorFormulario {
  mensaje: string;
  campos: Record<string, string>;
  recargarFechas: boolean;
  ofrecerListaEspera: boolean;
}

const GENERICO = "No hemos podido completar la reserva. Inténtalo de nuevo o escríbenos.";

/** Maps an API answer of POST /api/reservas/* to the form (F08 spec, "Errores"). */
export function errorDeReserva(error: unknown): ErrorFormulario {
  const base: ErrorFormulario = { mensaje: GENERICO, campos: {}, recargarFechas: false, ofrecerListaEspera: false };
  if (!(error instanceof ApiError)) {
    return { ...base, mensaje: "No hay conexión. Comprueba tu red e inténtalo de nuevo." };
  }
  switch (error.code) {
    case "sin_plazas":
      return { ...base, mensaje: "Alguien ha reservado la última plaza justo ahora.", recargarFechas: true, ofrecerListaEspera: true };
    case "edicion_no_reservable":
      return { ...base, mensaje: error.detail ?? "Esta fecha ya no se puede reservar.", recargarFechas: true };
    case "limite_pendientes":
      return { ...base, mensaje: "Ya tienes reservas pendientes de pago. Complétalas o escríbenos por WhatsApp." };
  }
  if (error.status === 429) return { ...base, mensaje: "Demasiados intentos, espera unos minutos." };
  if (error.status === 400) {
    const campos = camposDe(error);
    if (Object.keys(campos).length > 0) return { ...base, mensaje: "Revisa los datos marcados.", campos };
    return { ...base, mensaje: error.detail ?? GENERICO };
  }
  return base;
}

/** Same for the contact and waiting-list forms. */
export function errorDeFormulario(error: unknown): ErrorFormulario {
  const base: ErrorFormulario = {
    mensaje: "No hemos podido enviarlo. Inténtalo de nuevo en un momento.",
    campos: {},
    recargarFechas: false,
    ofrecerListaEspera: false,
  };
  if (!(error instanceof ApiError)) return { ...base, mensaje: "No hay conexión. Comprueba tu red e inténtalo de nuevo." };
  if (error.status === 429) return { ...base, mensaje: "Demasiados intentos, espera unos minutos." };
  if (error.status === 400) {
    const campos = camposDe(error);
    return Object.keys(campos).length > 0 ? { ...base, mensaje: "Revisa los datos marcados.", campos } : { ...base, mensaje: error.detail ?? base.mensaje };
  }
  return base;
}

/** API property names (PascalCase) → form field names (camelCase). */
function camposDe(error: ApiError): Record<string, string> {
  const campos: Record<string, string> = {};
  for (const [clave, mensajes] of Object.entries(error.errors)) {
    if (mensajes[0]) campos[clave.charAt(0).toLowerCase() + clave.slice(1)] = mensajes[0];
  }
  return campos;
}
