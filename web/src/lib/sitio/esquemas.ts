import { z } from "zod";

/** Same text as ConsentimientoListaEspera.V1 in the API. If one changes, change both and bump the version. */
export const CONSENTIMIENTO_LISTA_ESPERA_V1 =
  "Acepto la política de privacidad y que me contactéis por teléfono, WhatsApp o email para avisarme cuando haya nuevas fechas de este curso.";

/** Light check (the API validates for real with libphonenumber): Spanish 9 digits or international with prefix. */
export function pareceTelefono(valor: string): boolean {
  const limpio = valor.trim();
  const digitos = limpio.replace(/[\s.()-]/g, "");
  return /^(\+|00)?\d{9,15}$/.test(digitos);
}

const nombre = z.string().trim().min(1, "Escribe tu nombre.").max(120, "Máximo 120 caracteres.");
const email = z.string().trim().min(1, "Escribe tu email.").max(200, "Máximo 200 caracteres.").email("El email no es válido.");
const telefono = z.string().trim().min(1, "Escribe tu WhatsApp.").refine(pareceTelefono, "El teléfono no es válido.");

export const reservaEsquema = z.object({
  edicionId: z.string().min(1, "Elige una fecha."),
  nombre,
  telefono,
  email,
  metodo: z.enum(["Stripe", "Manual"]),
  aceptaCondiciones: z.literal(true, { error: "Debes aceptar las condiciones de reserva y la política de privacidad." }),
  website: z.string().optional(),
});
export type ReservaFormulario = z.infer<typeof reservaEsquema>;

export const contactoEsquema = z.object({
  nombre,
  telefono: z
    .string()
    .trim()
    .refine((v) => v === "" || pareceTelefono(v), "El teléfono no es válido."),
  email,
  asunto: z.string(),
  mensaje: z.string().trim().min(10, "Cuéntanos un poco más (mínimo 10 caracteres).").max(2900, "Máximo 2.900 caracteres."),
  aceptaPrivacidad: z.literal(true, { error: "Debes aceptar la política de privacidad." }),
  website: z.string().optional(),
});
export type ContactoFormulario = z.infer<typeof contactoEsquema>;

export const listaEsperaEsquema = z.object({
  cursoId: z.string().min(1, "Elige una formación."),
  nombre,
  telefono,
  email,
  fechaPreferida: z.string().trim().max(200, "Máximo 200 caracteres."),
  aceptaPrivacidad: z.literal(true, { error: "Debes aceptar para que podamos avisarte." }),
  website: z.string().optional(),
});
export type ListaEsperaFormulario = z.infer<typeof listaEsperaEsquema>;

export const ASUNTOS_CONTACTO = ["Formaciones presenciales", "Academia online", "Cita en el spa", "Otro"] as const;

/** The API has no subject field: it travels at the start of the message. */
export function mensajeConAsunto(asunto: string, mensaje: string): string {
  return asunto ? `[${asunto}] ${mensaje.trim()}` : mensaje.trim();
}
