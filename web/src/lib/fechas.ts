import { differenceInMinutes, formatDistanceStrict } from "date-fns";
import { es } from "date-fns/locale";
import { formatInTimeZone, fromZonedTime } from "date-fns-tz";

/** Every date the users see or type is in Madrid, whatever the device's zone. */
export const ZONA = "Europe/Madrid";

export interface FechaLocal {
  /** yyyy-MM-dd */
  fecha: string;
  /** HH:mm */
  hora: string;
}

export class HoraInexistenteError extends Error {
  constructor() {
    super("Esa hora no existe en Madrid ese día (se adelanta el reloj). Elige otra.");
    this.name = "HoraInexistenteError";
  }
}

/**
 * Madrid wall-clock date and time → ISO 8601 with the right offset for that day (+01:00 in winter, +02:00 in summer).
 * The hour skipped when the clock goes forward in March does not exist and throws HoraInexistenteError; the hour
 * repeated in October resolves to its first occurrence (summer time).
 */
export function madridAIso({ fecha, hora }: FechaLocal): string {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(fecha) || !/^\d{2}:\d{2}$/.test(hora)) {
    throw new Error("Fecha u hora con formato incorrecto.");
  }
  const instante = fromZonedTime(`${fecha}T${hora}:00`, ZONA);
  if (Number.isNaN(instante.getTime())) throw new Error("Fecha u hora con formato incorrecto.");
  const vuelta = isoAMadrid(instante.toISOString());
  if (vuelta.fecha !== fecha || vuelta.hora !== hora) throw new HoraInexistenteError();
  return formatInTimeZone(instante, ZONA, "yyyy-MM-dd'T'HH:mm:ssXXX");
}

/** ISO instant → Madrid wall-clock date and time (for the edit form). */
export function isoAMadrid(iso: string): FechaLocal {
  return {
    fecha: formatInTimeZone(iso, ZONA, "yyyy-MM-dd"),
    hora: formatInTimeZone(iso, ZONA, "HH:mm"),
  };
}

/** "sáb 5 dic 2026, 10:00" */
export function formatearFechaHora(iso: string): string {
  return formatInTimeZone(iso, ZONA, "EEE d MMM yyyy, HH:mm", { locale: es });
}

/** "sábado, 5 de diciembre de 2026" */
export function formatearFechaLarga(iso: string): string {
  return formatInTimeZone(iso, ZONA, "EEEE, d 'de' MMMM 'de' yyyy", { locale: es });
}

/** "5 dic 2026" */
export function formatearFecha(iso: string): string {
  return formatInTimeZone(iso, ZONA, "d MMM yyyy", { locale: es });
}

export function formatearHora(iso: string): string {
  return formatInTimeZone(iso, ZONA, "HH:mm");
}

/** Time left until `iso`: "caduca en 5 h", "caduca en 35 min", "caducada". Urgent below 6 h. */
export function caducidad(iso: string, ahora: Date): { texto: string; urgente: boolean; caducada: boolean } {
  const minutos = differenceInMinutes(new Date(iso), ahora);
  if (minutos <= 0) return { texto: "caducada", urgente: true, caducada: true };
  const urgente = minutos < 6 * 60;
  if (minutos < 60) return { texto: `caduca en ${minutos} min`, urgente, caducada: false };
  const horas = Math.floor(minutos / 60);
  if (horas < 48) return { texto: `caduca en ${horas} h`, urgente, caducada: false };
  return { texto: `caduca en ${Math.floor(horas / 24)} días`, urgente, caducada: false };
}

/** "hace 12 días" */
export function antiguedad(iso: string, ahora: Date): string {
  return formatDistanceStrict(new Date(iso), ahora, { locale: es, addSuffix: true, roundingMethod: "floor" });
}
