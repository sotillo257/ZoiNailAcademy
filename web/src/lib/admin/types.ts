// Backoffice DTOs, hand-written from Docs/00-contrato-y-convenciones.md and the API records (F02, F04, F05, F07).
// `npm run api:types` generates src/lib/api/schema.d.ts from the Swagger; keep both in sync when the API changes.

export type Rol = "Admin" | "SuperAdmin";
export type EstadoReserva = "Pendiente" | "Pagada" | "Expirada" | "Cancelada";
export type MetodoPago = "Stripe" | "Manual";
export type EstadoEdicion = "Programada" | "Oculta" | "Cancelada";

export interface UserDto {
  id: string;
  email: string;
  nombre: string;
  rol: Rol;
}

export interface LoginResponse {
  accessToken: string;
  /** Seconds. */
  expiresIn: number;
  user: UserDto;
}

export interface EdicionAdminDto {
  id: string;
  cursoId: string;
  curso: string;
  inicio: string;
  plazas: number;
  pagadas: number;
  pendientes: number;
  libres: number;
  estado: EstadoEdicion;
  reservable: boolean;
  notas: string | null;
  version: string;
}

export interface EdicionEditadaDto {
  edicion: EdicionAdminDto;
  avisos: string[];
}

export interface ReservaAdminDto {
  id: string;
  codigo: string;
  edicionId: string;
  curso: string;
  inicio: string;
  metodo: MetodoPago;
  estado: EstadoReserva;
  nombre: string;
  email: string;
  telefono: string;
  importe: number;
  creadaEn: string;
  pagadaEn: string | null;
  expiraEn: string;
  requiereRevision: boolean;
  notaInterna: string | null;
  stripePaymentIntentId: string | null;
  version: string;
  /** Only on expired bookings of the pending list. */
  plazaDisponible?: boolean;
}

export interface ReservaAccionDto {
  reserva: ReservaAdminDto;
  avisos: string[];
}

export interface ReservasEdicionDto {
  edicion: EdicionAdminDto;
  reservas: ReservaAdminDto[];
}

export interface ResumenDto {
  pendientesDePago: number;
  pendientesQueCaducanHoy: number;
  requierenRevision: number;
  listaEsperaSinContactar: number;
  proximasEdiciones: EdicionAdminDto[];
}

export interface CursoOpcionDto {
  id: string;
  nombre: string;
  slug: string;
  activo: boolean;
}

export interface CursoAdminDto {
  id: string;
  nombre: string;
  slug: string;
  descripcionCorta: string;
  descripcion: string;
  precio: number;
  imagen: string;
  ubicacion: string | null;
  duracionTexto: string | null;
  activo: boolean;
  orden: number;
}

export type CursoCampos = Omit<CursoAdminDto, "id">;

export interface CursoEditadoDto {
  curso: CursoAdminDto;
  avisos: string[];
}

export interface ListaEsperaAdminDto {
  id: string;
  cursoId: string;
  curso: string;
  /** Preferred edition, already formatted by the API in Madrid time. */
  edicionPreferida: string | null;
  fechaPreferida: string | null;
  nombre: string;
  email: string;
  telefono: string;
  contactado: boolean;
  contactadoEn: string | null;
  /** Unsubscribed from the new-dates notices (F11). */
  baja: boolean;
  notas: string | null;
  creadoEn: string;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

/** Limits mirrored from the domain so the forms fail before the API does. */
export const LIMITES = {
  plazasMinimas: 1,
  plazasMaximas: 200,
  notasEdicion: 500,
  notaReserva: 500,
  horasAmpliacionMaximas: 168,
  notasListaEspera: 500,
} as const;

/** Email types of GET /api/admin/emails/preview/{tipo} (Application/Emails/TiposEmail.cs). */
export const TIPOS_EMAIL = [
  { tipo: "ReservaManual.Comprador", nombre: "Reserva por transferencia (comprador)" },
  { tipo: "ReservaManual.Clienta", nombre: "Reserva por transferencia (aviso a la clienta)" },
  { tipo: "ReservaPagada.Comprador", nombre: "Pago confirmado (comprador)" },
  { tipo: "ReservaPagada.Clienta", nombre: "Pago confirmado (aviso a la clienta)" },
  { tipo: "ReservaExpirada.Comprador", nombre: "Reserva caducada (comprador)" },
  { tipo: "PagoSinPlaza.Clienta", nombre: "Pago tras caducar sin plaza (aviso a la clienta)" },
  { tipo: "ListaEspera.Clienta", nombre: "Alta en lista de espera (aviso a la clienta)" },
  { tipo: "Contacto.Clienta", nombre: "Mensaje de contacto (aviso a la clienta)" },
  { tipo: "Contacto.Acuse", nombre: "Acuse de contacto (remitente)" },
  { tipo: "AvisoNuevasEdiciones", nombre: "Nuevas fechas (lista de espera)" },
  { tipo: "AvisoNuevasEdiciones.Clienta", nombre: "Nuevas fechas (aviso a la clienta)" },
] as const;
