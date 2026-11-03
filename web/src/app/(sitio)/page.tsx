import type { Metadata } from "next";
import Link from "next/link";
import {
  CalendarCheckIcon,
  CheckIcon,
  ChevronRightIcon,
  ClockIcon,
  GlobeIcon,
  GraduationCapIcon,
  MonitorPlayIcon,
  TrophyIcon,
  UsersIcon,
} from "lucide-react";
import { BotonLink } from "@/components/sitio/boton";
import { CtaFinal } from "@/components/sitio/cta-final";
import { Encabezado } from "@/components/sitio/encabezado";
import { Foto } from "@/components/sitio/foto";
import { IconoEsmalte } from "@/components/sitio/iconos";
import { Pie } from "@/components/sitio/pie";
import { Contenedor, TituloSeccion } from "@/components/sitio/seccion";
import { FOTOS, TRAYECTORIA, TRAYECTORIA_MOVIL } from "@/lib/sitio/contenido";
import { SITIO, enlace } from "@/lib/sitio/datos";

export const metadata: Metadata = {
  title: { absolute: "Zoila Buzetta · Nail Artist & Educadora en Barcelona" },
  description:
    "17 años haciendo uñas, +5.000 alumnas formadas y Campeona Internacional. Formaciones presenciales en Barcelona, academia online y citas en el spa.",
  alternates: { canonical: "/" },
  openGraph: { title: "Zoila Buzetta · Nail Artist & Educadora", type: "website", locale: "es_ES" },
};

const DATOS = [
  { icono: ClockIcon, texto: "17 años de experiencia" },
  { icono: UsersIcon, texto: "+5.000 alumnas" },
  { icono: TrophyIcon, texto: "Campeona internacional" },
  { icono: GlobeIcon, texto: "Alumnas en +10 países" },
  { icono: IconoEsmalte, texto: "Especialista Dual y Gel", soloEscritorio: true },
];

const DESTINOS = [
  {
    titulo: "Cursos Presenciales",
    foto: FOTOS.claseAcademia,
    fotoAlt: "Foto de una clase en la academia",
    puntos: ["En la academia de Barcelona", "Grupos reducidos y práctica real", "Apúntate a la lista de espera"],
    boton: "Ver próximos cursos",
    href: "/formaciones",
  },
  {
    titulo: "Zoi Nail Online Academy",
    foto: FOTOS.grabandoClase,
    fotoAlt: "Foto de Zoila grabando una clase",
    puntos: ["+15 cursos profesionales", "Certificación y comunidad privada", "Prueba gratis 48 horas"],
    boton: "Entrar a la academia",
    href: enlace(SITIO.academiaOnlineUrl),
  },
  {
    titulo: "Citas en el Spa",
    foto: FOTOS.spa,
    fotoAlt: "Foto del spa o de un trabajo",
    puntos: ["Manicura y uñas de salón", "Técnicas Dual y Gel", "Reserva online en un minuto"],
    boton: "Reservar mi cita",
    href: enlace(SITIO.reservarCitaUrl),
  },
];

const ACCESOS_MOVIL = [
  { icono: GraduationCapIcon, titulo: "Cursos presenciales", detalle: "Lista de espera · Barcelona", href: "/formaciones", oscuro: false },
  { icono: MonitorPlayIcon, titulo: "Academia online", detalle: "+15 cursos · Prueba gratis 48 h", href: enlace(SITIO.academiaOnlineUrl), oscuro: false },
  { icono: CalendarCheckIcon, titulo: "Reservar cita en el spa", detalle: "Manicura, Dual y Gel", href: enlace(SITIO.reservarCitaUrl), oscuro: true },
];

function EnlaceAcceso({ href, className, children }: { href: string; className: string; children: React.ReactNode }) {
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

export default function InicioPage() {
  return (
    <>
      <Encabezado
        aviso={{
          texto: "Nuevo en la academia online: Curso Dual Sandwich",
          textoMovil: "Nuevo: Curso Dual Sandwich",
          enlace: { texto: "Descúbrelo aquí", href: enlace(SITIO.academiaOnlineUrl) },
        }}
      />
      <main className="flex-1">
        {/* Hero */}
        <section className="bg-z-cream pb-8 pt-6 md:py-[74px]">
          <Contenedor className="grid items-center gap-7 md:grid-cols-2 md:gap-16">
            <Foto
              src={FOTOS.zoila}
              alt="Foto de Zoila"
              nota="la misma línea que la portada de la academia"
              icono
              prioridad
              className="aspect-[354/298] w-full md:order-2 md:aspect-auto md:h-[496px]"
            />
            <div className="text-center md:order-1 md:text-left">
              <h1 className="text-[31px] font-bold leading-[1.15] md:text-[52px] md:leading-[1.08]">
                Hola, Soy Zoila Buzetta
              </h1>
              <p className="mt-3 text-[15px] font-bold leading-snug text-z-burgundy md:mt-8 md:text-[22px] md:leading-[1.4]">
                17 años haciendo uñas, +5.000 alumnas formadas y Campeona Internacional
              </p>
              <p className="mt-3 text-sm leading-relaxed text-z-text md:hidden">
                Cuido a mis clientas en mi spa y formo a manicuristas con técnicas reales y rentables.
              </p>
              <p className="mt-5 hidden text-base leading-[1.7] text-z-text md:block">
                En mi spa cuido a mis clientas y en mi academia formo a manicuristas para que trabajen con{" "}
                <strong className="font-bold text-z-ink">técnicas reales, rentables y actuales</strong>. Aquí encuentras
                todo lo que hago.
              </p>
              <div className="mt-8 hidden gap-3 md:flex">
                <BotonLink href="/formaciones">Ver cursos</BotonLink>
                <BotonLink href={enlace(SITIO.reservarCitaUrl)} variante="oscuro">
                  Reservar cita en el spa
                </BotonLink>
              </div>
              <p className="mt-6 hidden text-[11px] text-z-muted md:block">Barcelona, España · Alumnas en más de 10 países</p>
            </div>
          </Contenedor>
        </section>

        {/* Phone: three big shortcuts instead of the cards */}
        <nav aria-label="Qué estás buscando" className="px-[18px] pt-6 md:hidden">
          <ul className="flex flex-col gap-3.5">
            {ACCESOS_MOVIL.map(({ icono: Icono, ...a }) => (
              <li key={a.titulo}>
                <EnlaceAcceso
                  href={a.href}
                  className={`flex min-h-[66px] items-center gap-4 rounded-lg px-5 py-3 text-white ${a.oscuro ? "bg-z-ink" : "bg-z-burgundy"}`}
                >
                  <Icono className="size-6 shrink-0" strokeWidth={1.75} aria-hidden />
                  <span className="flex-1">
                    <span className="block text-[13px] font-bold uppercase tracking-[0.03em]">{a.titulo}</span>
                    <span className="block text-[11px] opacity-90">{a.detalle}</span>
                  </span>
                  <ChevronRightIcon className="size-5 shrink-0" aria-hidden />
                </EnlaceAcceso>
              </li>
            ))}
          </ul>
        </nav>

        {/* Key facts */}
        <section aria-label="Datos de Zoila" className="mx-[18px] mt-6 border-y border-z-line md:mx-0 md:mt-0 md:border-t-0">
          <Contenedor>
            <ul className="grid grid-cols-2 gap-y-6 py-6 md:grid-cols-5 md:py-[34px]">
              {DATOS.map(({ icono: Icono, texto, soloEscritorio }) => (
                <li key={texto} className={`flex flex-col items-center gap-2.5 text-center ${soloEscritorio ? "hidden md:flex" : ""}`}>
                  <Icono className="size-6 text-z-burgundy" strokeWidth={1.75} aria-hidden />
                  <span className="text-[11px] font-semibold md:text-xs">{texto}</span>
                </li>
              ))}
            </ul>
          </Contenedor>
        </section>

        {/* Desktop: what are you looking for */}
        <section aria-labelledby="buscando" className="hidden py-[72px] md:block">
          <Contenedor>
            <TituloSeccion id="buscando" titulo="¿Qué Estás Buscando?" subtitulo="Elige a dónde quieres ir y te llevo directo" />
            <ul className="mt-12 grid grid-cols-3 gap-6">
              {DESTINOS.map((d) => (
                <li key={d.titulo} className="flex flex-col overflow-hidden rounded-md">
                  <Foto src={d.foto} alt={d.fotoAlt} className="h-[220px] rounded-none" sizes="350px" />
                  <div className="flex flex-1 flex-col bg-z-burgundy px-7 pb-7 pt-8 text-white">
                    <h3 className="text-center text-xl font-bold">{d.titulo}</h3>
                    <ul className="mt-4 flex-1 space-y-2.5 text-[13px]">
                      {d.puntos.map((p) => (
                        <li key={p} className="flex items-start gap-2.5">
                          <CheckIcon className="mt-0.5 size-3.5 shrink-0" strokeWidth={2.5} aria-hidden />
                          {p}
                        </li>
                      ))}
                    </ul>
                    <BotonLink href={d.href} variante="blanco" ancho className="mt-5">
                      {d.boton}
                    </BotonLink>
                  </div>
                </li>
              ))}
            </ul>
          </Contenedor>
        </section>

        {/* About */}
        <section aria-labelledby="conoce" className="py-10 md:bg-z-cream md:py-[72px]">
          <Contenedor className="grid gap-6 md:grid-cols-[1fr_512px] md:gap-14">
            <div>
              <h2 id="conoce" className="text-center text-2xl font-bold leading-tight md:text-left md:text-[32px]">
                Conoce a Zoila Buzetta
              </h2>
              <p className="mt-2 text-center text-sm font-bold leading-snug text-z-burgundy md:mt-4 md:text-left md:text-lg">
                De hacer uñas en su mesa a formar a miles de manicuristas
                <span className="hidden md:inline"> en todo el mundo</span>
              </p>
              <Foto src={FOTOS.zoilaConAlumnas} alt="Zoila con sus alumnas" className="mt-5 aspect-[354/240] w-full md:hidden" />
              <ul className="mt-5 space-y-2 md:mt-6 md:space-y-3">
                {TRAYECTORIA.map((t, i) => (
                  <li key={t} className="flex items-start gap-3 text-sm text-z-text">
                    <CheckIcon className="mt-0.5 size-4 shrink-0 text-z-burgundy" strokeWidth={2.5} aria-hidden />
                    <span className="md:hidden">{TRAYECTORIA_MOVIL[i]}</span>
                    <span className="hidden md:inline">{t}</span>
                  </li>
                ))}
              </ul>
              <div className="mt-6 rounded-lg bg-z-cream p-5 md:bg-white md:px-6 md:py-6">
                <h3 className="text-sm font-bold md:text-base">¿Por qué confiar en Zoila?</h3>
                <p className="mt-2 text-[13px] leading-relaxed text-z-text md:hidden">
                  Porque enseña lo que hace cada día en su spa, para la que empieza desde cero y para la que quiere cobrar
                  más.
                </p>
                <p className="mt-2 hidden text-[13px] leading-[1.7] text-z-text md:block">
                  Porque vive la realidad de cada manicurista: la que empieza desde cero y la que ya trabaja y quiere cobrar
                  más. Enseña lo que hace cada día en su spa.
                </p>
              </div>
            </div>
            <div className="hidden grid-cols-2 grid-rows-2 gap-3 md:grid md:h-[450px]">
              <Foto src={FOTOS.zoilaConAlumnas} alt="Zoila con sus alumnas" className="row-span-2" sizes="250px" />
              <Foto src={FOTOS.competicion} alt="Competición o premio" sizes="250px" />
              <Foto src={FOTOS.dandoClase} alt="Dando clase" sizes="250px" />
            </div>
          </Contenedor>
        </section>

        {/* Works */}
        <section aria-labelledby="trabajos" className="pb-12 pt-2 md:py-[72px]">
          <Contenedor>
            <TituloSeccion id="trabajos" titulo="Mis Trabajos" subtitulo="Lo que hago cada día en el spa" />
            <ul className="mt-6 grid grid-cols-2 gap-2.5 md:mt-10 md:grid-cols-4 md:gap-3.5">
              {FOTOS.trabajos.map((src, i) => (
                <li key={i}>
                  <Foto src={src} alt={`Trabajo ${i + 1}`} className="aspect-square w-full" sizes="(min-width: 768px) 263px, 50vw" />
                </li>
              ))}
            </ul>
            <div className="mt-6 flex justify-center md:mt-9">
              <BotonLink href={SITIO.instagramUrl} variante="oscuro" className="w-full md:w-auto">
                Sígueme en Instagram
              </BotonLink>
            </div>
          </Contenedor>
        </section>

        <CtaFinal />
      </main>
      <Pie inicio />
    </>
  );
}
