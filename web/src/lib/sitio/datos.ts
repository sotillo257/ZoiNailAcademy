/**
 * Contact details, external links and texts of the public web that the client provides. Values in square brackets
 * are placeholders from the design: replace them before going live (see Docs/pendientes/README.md).
 */
export const SITIO = {
  nombre: "Zoila Buzetta",
  subtitulo: "Nail Artist & Educadora",
  ciudad: "Barcelona",
  direccion: "[Dirección]",
  codigoPostal: "[Código postal]",
  /** Shown as text. */
  whatsappTexto: "[Número de WhatsApp]",
  /** International format without "+", for wa.me links. Empty until the client provides it. */
  whatsappNumero: "",
  email: "[email de contacto]",
  instagramUsuario: "[usuario]",
  instagramUrl: "https://www.instagram.com/",
  horarioSemana: "[horario]",
  horarioSabado: "[horario]",
  /** Response time promised in the contact form. */
  respuestaEn: "[24 h]",
  googleMapsEmbedUrl: "",
  googleMapsUrl: "https://maps.google.com/",
  googleResenasUrl: "https://www.google.com/maps",
  /** External: the online academy and the spa booking system live outside this web. */
  academiaOnlineUrl: "https://zoinailartist.online/",
  spaUrl: "[url del spa]",
  reservarCitaUrl: "[url de reserva de citas]",
  /** PDF or image of the 2027 calendar for the "Descargar calendario" button; empty hides the button. */
  calendarioDescargable: "",
} as const;

export function whatsappUrl(mensaje?: string): string {
  const base = `https://wa.me/${SITIO.whatsappNumero}`;
  return mensaje ? `${base}?text=${encodeURIComponent(mensaje)}` : base;
}

/** A placeholder URL from the design (still in brackets) is not a link yet. */
export function esEnlace(url: string): boolean {
  return /^https?:\/\//.test(url);
}

/** External link, or the contact page while the client has not given the URL yet. */
export function enlace(url: string): string {
  return esEnlace(url) ? url : "/contacto";
}

export const NAV = [
  { href: "/", label: "Inicio", propio: true },
  { href: "/formaciones", label: "Formaciones presenciales", propio: true },
  { href: enlace(SITIO.academiaOnlineUrl), label: "Academia online", propio: false },
  { href: enlace(SITIO.spaUrl), label: "Spa", propio: false },
  { href: "/contacto", label: "Contacto", propio: true },
] as const;
