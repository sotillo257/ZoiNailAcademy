import { formatInTimeZone } from "date-fns-tz";
import { es } from "date-fns/locale";
import { ZONA } from "@/lib/fechas";
import type { EdicionPublicaDto, EstadoEdicionPublico } from "./api";

/** "sáb 5 dic 2026, 10:00" (Madrid). */
export function fechaEdicion(iso: string): string {
  return formatInTimeZone(iso, ZONA, "EEE d MMM yyyy, HH:mm", { locale: es });
}

/** "5 dic" (Madrid), for the calendar table. */
export function fechaCorta(iso: string): string {
  return formatInTimeZone(iso, ZONA, "d MMM", { locale: es });
}

export function plazasTexto(e: EdicionPublicaDto): string {
  if (e.estado === "Completo") return "Completo";
  if (e.estado === "UltimasPlazas") return e.plazasLibres === 1 ? "¡Última plaza!" : `¡Últimas ${e.plazasLibres} plazas!`;
  return `${e.plazasLibres} plazas`;
}

/** Option of the "Edición" select: "18 ene 2027, 09:00 · 6 plazas" (fits a phone). */
export function etiquetaEdicion(e: EdicionPublicaDto): string {
  return `${formatInTimeZone(e.inicio, ZONA, "d MMM yyyy, HH:mm", { locale: es })} · ${plazasTexto(e)}`;
}

export const ESTADO_CURSO: Record<EstadoEdicionPublico | "SinFechas", { texto: string; clase: string }> = {
  Disponible: { texto: "Plazas disponibles", clase: "bg-z-green-soft text-z-green" },
  UltimasPlazas: { texto: "Últimas plazas", clase: "bg-z-amber-soft text-z-amber" },
  Completo: { texto: "Completo", clase: "bg-z-red-soft text-z-burgundy" },
  SinFechas: { texto: "Lista de espera", clase: "bg-z-red-soft text-z-burgundy" },
};

/** Badge of a course: the best state among its dates (any free seat beats "últimas", which beats "completo"). */
export function estadoCurso(ediciones: EdicionPublicaDto[]): EstadoEdicionPublico | "SinFechas" {
  if (ediciones.length === 0) return "SinFechas";
  if (ediciones.some((e) => e.estado === "Disponible")) return "Disponible";
  if (ediciones.some((e) => e.estado === "UltimasPlazas")) return "UltimasPlazas";
  return "Completo";
}

/** Date preselected in the form: the one in the URL if it can be booked, else the first with seats, else none. */
export function edicionInicial(ediciones: EdicionPublicaDto[], pedida?: string | null): string {
  const reservable = (e: EdicionPublicaDto) => e.estado !== "Completo";
  const enUrl = pedida ? ediciones.find((e) => e.id === pedida && reservable(e)) : undefined;
  return (enUrl ?? ediciones.find(reservable))?.id ?? "";
}
