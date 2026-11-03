import type { Metadata } from "next";
import Link from "next/link";
import { ChevronRightIcon, ClockIcon, LockIcon, MailIcon, MapIcon, MapPinIcon } from "lucide-react";
import { BotonLink } from "@/components/sitio/boton";
import { ContactoForm } from "@/components/sitio/contacto-form";
import { Encabezado } from "@/components/sitio/encabezado";
import { IconoInstagram, IconoWhatsapp } from "@/components/sitio/iconos";
import { Pie } from "@/components/sitio/pie";
import { Contenedor } from "@/components/sitio/seccion";
import { SITIO, enlace, whatsappUrl } from "@/lib/sitio/datos";
import { cn } from "@/lib/utils";

export const metadata: Metadata = {
  title: "Contacto",
  description: "Escríbenos por WhatsApp, email o Instagram para dudas sobre las formaciones, la academia online o tu cita en el spa.",
  alternates: { canonical: "/contacto" },
};

const CANALES = [
  { icono: IconoWhatsapp, titulo: "WhatsApp", movil: "Escríbenos por WhatsApp", valor: SITIO.whatsappTexto, boton: "Escríbenos", href: whatsappUrl() },
  { icono: MailIcon, titulo: "Email", movil: "Email", valor: SITIO.email, boton: "Enviar email", href: `mailto:${SITIO.email}` },
  { icono: IconoInstagram, titulo: "Instagram", movil: "Instagram", valor: `@${SITIO.instagramUsuario}`, boton: "Seguir", href: SITIO.instagramUrl },
];

const ATAJOS = [
  { titulo: "Quiero reservar una cita", detalle: "Elige día y hora online", href: enlace(SITIO.reservarCitaUrl) },
  { titulo: "Fechas y precios de cursos", detalle: "Calendario 2027 completo", href: "/formaciones#calendario" },
  { titulo: "Academia online", detalle: "Prueba gratis 48 horas", href: enlace(SITIO.academiaOnlineUrl) },
];

function Enlace({ href, className, children }: { href: string; className?: string; children: React.ReactNode }) {
  return href.startsWith("/") ? (
    <Link href={href} className={className}>
      {children}
    </Link>
  ) : (
    <a href={href} className={className} target="_blank" rel="noopener noreferrer">
      {children}
    </a>
  );
}

function DondeEstamos() {
  return (
    <div>
      <h2 className="text-[22px] font-bold md:text-[26px]">Dónde Estamos</h2>
      <ul className="mt-4 space-y-4 md:mt-5">
        <li className="flex gap-3">
          <MapPinIcon className="mt-0.5 size-5 shrink-0 text-z-burgundy" strokeWidth={1.75} aria-hidden />
          <div>
            <p className="text-sm font-bold">Spa y academia</p>
            <p className="text-[13px] text-z-text">
              {SITIO.direccion}, {SITIO.codigoPostal} {SITIO.ciudad}
            </p>
          </div>
        </li>
        <li className="flex gap-3">
          <ClockIcon className="mt-0.5 size-5 shrink-0 text-z-burgundy" strokeWidth={1.75} aria-hidden />
          <div>
            <p className="text-sm font-bold">Horario</p>
            <p className="text-[13px] text-z-text">Lunes a viernes: {SITIO.horarioSemana}</p>
            <p className="text-[13px] text-z-text">Sábados: {SITIO.horarioSabado}</p>
          </div>
        </li>
        <li className="hidden gap-3 md:flex">
          <LockIcon className="mt-0.5 size-5 shrink-0 text-z-burgundy" strokeWidth={1.75} aria-hidden />
          <div>
            <p className="text-sm font-bold">Atención con cita previa</p>
            <p className="text-[13px] text-z-text">Para el spa, reserva antes tu hora online</p>
          </div>
        </li>
      </ul>
      {SITIO.googleMapsEmbedUrl ? (
        <iframe
          title="Mapa de Google Maps"
          src={SITIO.googleMapsEmbedUrl}
          loading="lazy"
          referrerPolicy="no-referrer-when-downgrade"
          className="mt-5 h-[255px] w-full rounded-lg border-0 md:mt-6 md:h-[300px]"
        />
      ) : (
        <div role="img" aria-label="Mapa de Google Maps" className="mt-5 flex h-[255px] flex-col items-center justify-center gap-2 rounded-lg bg-z-photo md:mt-6 md:h-[300px]">
          <MapIcon className="size-7 text-[#6b6766]" strokeWidth={1.5} aria-hidden />
          <span className="text-xs font-semibold text-[#4d4948]">Mapa de Google Maps</span>
        </div>
      )}
      <BotonLink href={SITIO.googleMapsUrl} variante="contorno" tamano="sm" className="mt-4 h-12 w-full md:mt-5 md:h-[42px] md:w-auto">
        Cómo llegar
      </BotonLink>
    </div>
  );
}

export default function ContactoPage() {
  return (
    <>
      <Encabezado aviso={{ texto: "Calendario 2027 ya disponible", enlace: { texto: "Ver fechas", href: "/formaciones#calendario" } }} />
      <main className="flex-1">
        <section className="bg-z-cream px-[18px] pb-8 pt-7 text-center md:py-[60px]">
          <h1 className="text-[34px] font-bold leading-tight md:text-[50px]">Contacto</h1>
          <p className="mx-auto mt-2 max-w-[300px] text-[15px] font-bold leading-snug text-z-burgundy md:mt-3 md:max-w-none md:text-xl">
            ¿Hablamos? Te respondemos lo antes posible
          </p>
          <p className="mx-auto mt-2 max-w-[300px] text-[13px] leading-relaxed text-z-text md:mt-4 md:max-w-[600px] md:text-base">
            <span className="md:hidden">Dudas sobre formaciones, academia online o tu cita en el spa.</span>
            <span className="hidden md:inline">
              Escríbenos por el canal que prefieras para dudas sobre las formaciones, la academia online o tu cita en el spa.
            </span>
          </p>
        </section>

        {/* Channels: three red cards on desktop, three rows on the phone (WhatsApp first and red) */}
        <Contenedor className="pt-5 md:pt-14">
          <ul className="hidden grid-cols-3 gap-6 md:grid">
            {CANALES.map(({ icono: Icono, ...c }) => (
              <li key={c.titulo} className="flex flex-col items-center rounded-md bg-z-burgundy px-6 py-8 text-center text-white">
                <Icono className="size-[30px]" aria-hidden />
                <h2 className="mt-4 text-xl font-bold">{c.titulo}</h2>
                <p className="mt-2 text-sm">{c.valor}</p>
                <BotonLink href={c.href} variante="blanco" tamano="sm" className="mt-5 h-[38px] px-6">
                  {c.boton}
                </BotonLink>
              </li>
            ))}
          </ul>
          <ul className="flex flex-col gap-2.5 md:hidden">
            {CANALES.map(({ icono: Icono, ...c }, i) => (
              <li key={c.titulo}>
                <a
                  href={c.href}
                  target="_blank"
                  rel="noopener noreferrer"
                  className={cn(
                    "flex min-h-[66px] items-center gap-4 rounded-lg px-4 py-3",
                    i === 0 ? "bg-z-burgundy text-white" : "border border-z-line bg-white text-z-ink",
                  )}
                >
                  <Icono className={cn("size-6 shrink-0", i !== 0 && "text-z-burgundy")} aria-hidden />
                  <span className="flex-1">
                    <span className="block text-[13px] font-bold uppercase tracking-[0.03em]">{c.movil}</span>
                    <span className={cn("block text-[11px]", i === 0 ? "opacity-90" : "text-z-text")}>{c.valor}</span>
                  </span>
                  <ChevronRightIcon className="size-5 shrink-0" aria-hidden />
                </a>
              </li>
            ))}
          </ul>
          <div className="mt-5 border-t border-z-line pt-5 md:hidden">
            <h2 className="text-center text-lg font-bold">Quizás ya tienes tu respuesta</h2>
            <ul className="mt-4 space-y-2.5">
              {ATAJOS.map((a) => (
                <li key={a.titulo}>
                  <Enlace href={a.href} className="flex items-center justify-between rounded-md bg-z-cream px-4 py-3.5 text-[13px] font-bold">
                    {a.titulo}
                    <ChevronRightIcon className="size-4 text-z-burgundy" aria-hidden />
                  </Enlace>
                </li>
              ))}
            </ul>
          </div>
        </Contenedor>

        {/* Form + where we are */}
        <section aria-labelledby="mensaje" className="mt-6 bg-z-cream py-6 md:mt-0 md:bg-white md:py-14">
          <Contenedor className="grid gap-8 md:grid-cols-[575px_1fr] md:gap-12">
            <div className="rounded-lg border border-z-line bg-white px-4 py-6 md:border-0 md:bg-z-cream md:px-9 md:py-9">
              <h2 id="mensaje" className="text-[22px] font-bold md:text-[26px]">
                Envíanos un Mensaje
              </h2>
              <p className="mt-1.5 text-[13px] text-z-text">
                Te contestamos <span className="hidden md:inline">por WhatsApp o email </span>en menos de {SITIO.respuestaEn}.
              </p>
              <ContactoForm />
            </div>
            <div className="hidden md:block">
              <DondeEstamos />
            </div>
          </Contenedor>
        </section>
        <section className="px-[18px] py-8 md:hidden">
          <DondeEstamos />
        </section>

        <section aria-labelledby="respuesta" className="hidden bg-z-cream py-16 md:block">
          <Contenedor>
            <div className="text-center">
              <h2 id="respuesta" className="text-[33px] font-bold">
                Quizás Ya Tienes Tu Respuesta Aquí
              </h2>
              <p className="mt-2 text-sm text-z-text">Ve directo a lo que buscas</p>
            </div>
            <ul className="mt-9 grid grid-cols-3 gap-4">
              {ATAJOS.map((a) => (
                <li key={a.titulo}>
                  <Enlace href={a.href} className="flex items-center justify-between rounded-md border border-z-line bg-white px-6 py-5 hover:border-z-burgundy">
                    <span>
                      <span className="block text-sm font-bold">{a.titulo}</span>
                      <span className="mt-1 block text-xs text-z-text">{a.detalle}</span>
                    </span>
                    <ChevronRightIcon className="size-5 text-z-burgundy" aria-hidden />
                  </Enlace>
                </li>
              ))}
            </ul>
          </Contenedor>
        </section>
      </main>
      <div className="hidden md:block">
        <Pie fondo="blanco" datos={false} />
      </div>
      <div className="md:hidden">
        <Pie datos={false} enlaces={false} />
      </div>
    </>
  );
}
