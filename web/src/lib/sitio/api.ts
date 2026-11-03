import { ApiError } from "@/lib/admin/errors";

/** Public DTOs of the API contract (Docs/00-contrato-y-convenciones.md). */
export type EstadoEdicionPublico = "Disponible" | "UltimasPlazas" | "Completo";
export type EstadoReserva = "Pendiente" | "Pagada" | "Expirada" | "Cancelada";
export type MetodoPago = "Stripe" | "Manual";

export interface EdicionPublicaDto {
  id: string;
  inicio: string;
  plazasTotales: number;
  plazasLibres: number;
  estado: EstadoEdicionPublico;
}

export interface CursoResumenDto {
  id: string;
  slug: string;
  nombre: string;
  descripcionCorta: string;
  /** Full price. */
  precio: number;
  /** Paid online when booking; the rest in person. */
  precioReserva: number;
  imagen: string;
  orden: number;
  ubicacion: string | null;
  duracionTexto: string | null;
  proximaEdicion: EdicionPublicaDto | null;
  ediciones: EdicionPublicaDto[];
}

export interface CursoDetalleDto extends CursoResumenDto {
  descripcion: string;
}

export interface DatosPagoDto {
  titular: string;
  iban: string;
  bizumTelefono: string;
  concepto: string;
}

export interface ReservaPublicaDto {
  codigo: string;
  estado: EstadoReserva;
  metodo: MetodoPago;
  curso: string;
  inicio: string;
  importe: number;
  precioCurso: number;
  expiraEn: string | null;
  datosPago: DatosPagoDto | null;
  whatsappUrl: string | null;
}

export interface ReservaTarjetaCreadaDto {
  codigo: string;
  checkoutUrl: string;
}

const API_INTERNAL_URL = process.env.API_INTERNAL_URL ?? "http://localhost:8080";

/**
 * Server-side read of the public API (SSR). `fresco` = never cached (seats); otherwise the page revalidates every
 * 60 s. Returns null on 404.
 */
export async function leerApi<T>(ruta: string, { fresco = false }: { fresco?: boolean } = {}): Promise<T | null> {
  const respuesta = await fetch(`${API_INTERNAL_URL}${ruta}`, {
    headers: { Accept: "application/json" },
    ...(fresco ? { cache: "no-store" as const } : { next: { revalidate: 60 } }),
  });
  if (respuesta.status === 404) return null;
  if (!respuesta.ok) throw new ApiError(respuesta.status, await cuerpo(respuesta));
  return (await respuesta.json()) as T;
}

/** Browser call to the API through the Next rewrite (always relative). Throws ApiError with the ProblemDetails. */
export async function llamarApi<T>(ruta: string, init: RequestInit = {}): Promise<T> {
  const respuesta = await fetch(ruta, {
    ...init,
    cache: "no-store",
    headers: { Accept: "application/json", ...(init.body ? { "Content-Type": "application/json" } : {}), ...init.headers },
  });
  if (!respuesta.ok) throw new ApiError(respuesta.status, await cuerpo(respuesta));
  if (respuesta.status === 204 || respuesta.status === 202 || respuesta.status === 201) {
    const texto = await respuesta.text();
    return (texto ? JSON.parse(texto) : undefined) as T;
  }
  return (await respuesta.json()) as T;
}

async function cuerpo(respuesta: Response): Promise<unknown> {
  try {
    return await respuesta.json();
  } catch {
    return null;
  }
}
