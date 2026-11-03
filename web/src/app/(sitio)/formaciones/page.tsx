import type { Metadata } from "next";
import Link from "next/link";
import { ArrowRightIcon, AwardIcon, DownloadIcon, HandIcon, PlayIcon, StarIcon, TrophyIcon, UsersIcon } from "lucide-react";
import { BotonLink } from "@/components/sitio/boton";
import { Carrusel } from "@/components/sitio/carrusel";
import { Encabezado } from "@/components/sitio/encabezado";
import { Foto } from "@/components/sitio/foto";
import { ListaEsperaDiferida } from "@/components/sitio/lista-espera-diferida";
import { Pie } from "@/components/sitio/pie";
import { Contenedor, TituloSeccion } from "@/components/sitio/seccion";
import { formatearImporte } from "@/lib/formato";
import { type CursoResumenDto, leerApi } from "@/lib/sitio/api";
import { FOTOS, GALERIAS, TESTIMONIOS } from "@/lib/sitio/contenido";
import { SITIO, whatsappUrl } from "@/lib/sitio/datos";
import { ESTADO_CURSO, estadoCurso, fechaCorta, fechaEdicion } from "@/lib/sitio/ediciones";
import { cn } from "@/lib/utils";

// Seats and states are never cached.
export const dynamic = "force-dynamic";

const TITULO = "Formaciones Presenciales en Barcelona";
const DESCRIPCION =
  "Aprende con Zoila Buzetta en persona y sal lista para trabajar con clientas reales. Grupos reducidos, práctica real y calendario 2027.";

export const metadata: Metadata = {
  title: TITULO,
  description: DESCRIPCION,
  alternates: { canonical: "/formaciones" },
  openGraph: { title: TITULO, description: DESCRIPCION, type: "website", locale: "es_ES" },
};

const VENTAJAS = [
  { icono: UsersIcon, texto: "Grupos reducidos" },
  { icono: HandIcon, texto: "Práctica real con modelos" },
  { icono: AwardIcon, texto: "Certificado de la academia" },
  { icono: TrophyIcon, texto: "Con una campeona internacional" },
];

type Props = { searchParams: Promise<Record<string, string | string[] | undefined>> };

function uno(valor: string | string[] | undefined): string | undefined {
  return typeof valor === "string" && valor ? valor : undefined;
}

/** JSON-LD Course + CourseInstance for each course and its upcoming dates. */
function datosEstructurados(cursos: CursoResumenDto[]) {
  const base = process.env.NEXT_PUBLIC_SITE_URL ?? "";
  return {
    "@context": "https://schema.org",
    "@graph": cursos.map((c) => ({
      "@type": "Course",
      name: c.nombre,
      description: c.descripcionCorta,
      provider: { "@type": "Person", name: SITIO.nombre },
      offers: { "@type": "Offer", price: c.precio, priceCurrency: "EUR", category: "Paid" },
      hasCourseInstance: c.ediciones.map((e) => ({
        "@type": "CourseInstance",
        courseMode: "Onsite",
        startDate: e.inicio,
        location: { "@type": "Place", name: SITIO.nombre, address: c.ubicacion ?? SITIO.ciudad },
        url: `${base}/reservar/${c.slug}?edicion=${e.id}`,
      })),
    })),
  };
}

function Insignia({ cursoEdiciones }: { cursoEdiciones: CursoResumenDto["ediciones"] }) {
  const estado = ESTADO_CURSO[estadoCurso(cursoEdiciones)];
  return <span className={cn("inline-block rounded-full px-2.5 py-1 text-[10px] font-bold", estado.clase)}>{estado.texto}</span>;
}

function fechasTexto(c: CursoResumenDto): string {
  return c.ediciones.length > 0 ? c.ediciones.map((e) => fechaCorta(e.inicio)).join(" · ") : "Próximamente";
}

export default async function FormacionesPage({ searchParams }: Props) {
  const query = await searchParams;
  const cursos = (await leerApi<CursoResumenDto[]>("/api/cursos", { fresco: true })) ?? [];
  const cursoPedido = cursos.find((c) => c.slug === uno(query.curso));
  const edicionPedida = cursoPedido?.ediciones.find((e) => e.id === uno(query.edicion));

  return (
    <>
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(datosEstructurados(cursos)) }} />
      <Encabezado aviso={{ texto: "Calendario 2027 ya disponible", enlace: { texto: "Ver fechas", href: "/formaciones#calendario" } }} />
      <main className="flex-1">
        {/* Hero */}
        <section className="bg-z-cream pb-8 pt-6 md:py-16">
          <Contenedor className="grid items-center gap-7 md:grid-cols-2 md:gap-12">
            <div className="grid grid-cols-2 gap-2.5 md:order-2 md:gap-3">
              <Foto src={FOTOS.grupoFormacion} alt="Foto de grupo en una formación" className="col-span-2 h-[200px] md:h-[277px]" prioridad />
              <Foto src={FOTOS.alumnasPracticando} alt="Alumnas practicando" className="h-[118px] md:h-[191px]" sizes="(min-width: 768px) 260px, 50vw" />
              <Foto src={FOTOS.entregaDiplomas} alt="Entrega de diplomas" className="h-[118px] md:h-[191px]" sizes="(min-width: 768px) 260px, 50vw" />
            </div>
            <div className="text-center md:order-1 md:text-left">
              <h1 className="text-[26px] font-bold leading-[1.15] md:text-[44px] md:leading-[1.1]">
                Formaciones Presenciales en Barcelona
              </h1>
              <p className="mt-3 text-sm font-bold leading-snug text-z-burgundy md:mt-6 md:text-lg">
                Aprende con Zoila en persona y sal lista para trabajar con clientas reales
              </p>
              <p className="mt-3 text-[13px] leading-relaxed text-z-text md:mt-4 md:text-[15px] md:leading-[1.7]">
                En la academia de Zoila te formas en grupos reducidos, con práctica real sobre modelos y las técnicas que usa
                cada día en su spa y que le han dado premios internacionales.
              </p>
              <div className="mt-6 flex flex-col gap-3 md:mt-8 md:flex-row">
                <BotonLink href="#formaciones">Ver formaciones</BotonLink>
                <BotonLink href="#calendario" variante="oscuro">
                  Calendario 2027
                </BotonLink>
              </div>
            </div>
          </Contenedor>
        </section>

        {/* Advantages */}
        <section aria-label="Por qué formarte con Zoila" className="border-b border-z-line">
          <Contenedor>
            <ul className="grid grid-cols-2 gap-y-6 py-6 md:grid-cols-4 md:py-8">
              {VENTAJAS.map(({ icono: Icono, texto }) => (
                <li key={texto} className="flex flex-col items-center gap-2.5 px-2 text-center">
                  <Icono className="size-6 text-z-burgundy" strokeWidth={1.75} aria-hidden />
                  <span className="text-[11px] font-semibold md:text-xs">{texto}</span>
                </li>
              ))}
            </ul>
          </Contenedor>
        </section>

        {/* Graduations */}
        <section aria-labelledby="graduaciones" className="py-10 md:py-16">
          <Contenedor>
            <TituloSeccion id="graduaciones" titulo="Nuestras Graduaciones" subtitulo="Cada promoción, una nueva manicurista lista para trabajar" />
            <div className="mt-6 grid grid-cols-2 gap-2.5 md:mt-10 md:grid-cols-4 md:grid-rows-2 md:gap-3">
              <Foto src={FOTOS.graduaciones[0] ?? null} alt="Graduación 1" className="col-span-2 h-[220px] md:row-span-2 md:h-[417px]" />
              {FOTOS.graduaciones.slice(1).map((src, i) => (
                <Foto
                  key={i}
                  src={src}
                  alt={`Graduación ${i + 2}`}
                  className={cn("h-[120px] md:h-[202px]", i >= 2 && "hidden md:flex")}
                  sizes="(min-width: 768px) 263px, 50vw"
                />
              ))}
            </div>
          </Contenedor>
        </section>

        {/* Calendar */}
        <section id="calendario" aria-labelledby="calendario-titulo" className="scroll-mt-4 bg-z-cream py-10 md:py-16">
          <Contenedor>
            <TituloSeccion id="calendario-titulo" titulo="Calendario de Clases 2027 · Barcelona" subtitulo="Elige tu fecha y reserva tu plaza antes de que se agote" />
            {/* Desktop: table */}
            <div className="mt-8 hidden overflow-hidden rounded-md bg-white md:block">
              <table className="w-full text-left text-[13px]">
                <thead className="bg-z-burgundy text-[11px] font-bold uppercase tracking-[0.06em] text-white">
                  <tr>
                    <th scope="col" className="px-5 py-3.5">Formación</th>
                    <th scope="col" className="px-5 py-3.5">Próximas fechas</th>
                    <th scope="col" className="px-5 py-3.5">Duración</th>
                    <th scope="col" className="px-5 py-3.5">Estado</th>
                  </tr>
                </thead>
                <tbody>
                  {cursos.map((c) => (
                    <tr key={c.id} className="border-t border-z-line">
                      <th scope="row" className="px-5 py-4 font-bold">
                        <Link href={`#curso-${c.slug}`} className="hover:text-z-burgundy">
                          {c.nombre}
                        </Link>
                      </th>
                      <td className="px-5 py-4 text-z-text">
                        {c.ediciones.length > 0
                          ? c.ediciones.map((e, i) => (
                              <span key={e.id}>
                                {i > 0 ? " · " : null}
                                <time dateTime={e.inicio} title={fechaEdicion(e.inicio)}>
                                  {fechaCorta(e.inicio)}
                                </time>
                              </span>
                            ))
                          : "Próximamente"}
                      </td>
                      <td className="px-5 py-4 text-z-text">{c.duracionTexto ?? "—"}</td>
                      <td className="px-5 py-4">
                        <Insignia cursoEdiciones={c.ediciones} />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            {/* Phone: cards */}
            <ul className="mt-6 space-y-2.5 md:hidden">
              {cursos.map((c) => (
                <li key={c.id}>
                  <Link href={`#curso-${c.slug}`} className="block rounded-md bg-white px-4 py-3.5">
                    <span className="flex items-start justify-between gap-3">
                      <span className="text-[13px] font-bold">{c.nombre}</span>
                      <Insignia cursoEdiciones={c.ediciones} />
                    </span>
                    <span className="mt-1 block text-xs text-z-text">
                      {fechasTexto(c)}
                      {c.duracionTexto ? ` · ${c.duracionTexto}` : ""}
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
            {cursos.length === 0 ? <p className="mt-6 text-center text-sm text-z-text">Estamos preparando las fechas. Vuelve pronto.</p> : null}
            {SITIO.calendarioDescargable ? (
              <div className="mt-6 flex justify-center md:mt-8">
                <BotonLink href={SITIO.calendarioDescargable} variante="contorno" tamano="sm" className="h-11 w-full md:w-auto" download>
                  <DownloadIcon className="size-4" aria-hidden />
                  Descargar calendario en imagen
                </BotonLink>
              </div>
            ) : null}
          </Contenedor>
        </section>

        {/* Courses */}
        <section id="formaciones" aria-labelledby="formaciones-titulo" className="scroll-mt-4 py-10 md:py-16">
          <Contenedor>
            <TituloSeccion id="formaciones-titulo" titulo="Nuestras Formaciones" subtitulo="Elige la que encaja contigo y reserva tu plaza" />
            <div className="mt-8 divide-y divide-z-line md:mt-12">
              {cursos.map((c, i) => {
                const reservable = c.ediciones.some((e) => e.estado !== "Completo");
                const listaEspera = `/formaciones?curso=${encodeURIComponent(c.slug)}#lista-espera`;
                const fotos = GALERIAS[c.slug] ?? Array(6).fill(null);
                return (
                  <article
                    key={c.id}
                    id={`curso-${c.slug}`}
                    aria-labelledby={`curso-${c.slug}-titulo`}
                    className="grid scroll-mt-4 items-center gap-5 py-8 first:pt-0 md:grid-cols-2 md:gap-12 md:py-12"
                  >
                    <Carrusel fotos={fotos} nombre={c.nombre} className={cn(i % 2 === 1 && "md:order-2")} />
                    <div>
                      <Insignia cursoEdiciones={c.ediciones} />
                      <h3 id={`curso-${c.slug}-titulo`} className="mt-3 text-[22px] font-bold leading-tight md:text-[26px]">
                        {c.nombre}
                      </h3>
                      <p className="mt-3 text-[13px] leading-relaxed text-z-text md:text-sm md:leading-[1.7]">{c.descripcionCorta}</p>
                      <ul className="mt-4 flex flex-wrap gap-2">
                        {c.duracionTexto ? <Chip>Duración: {c.duracionTexto}</Chip> : null}
                        <Chip>Precio: {formatearImporte(c.precio)}</Chip>
                        {c.precioReserva < c.precio ? <Chip>Reserva: {formatearImporte(c.precioReserva)}</Chip> : null}
                      </ul>
                      {reservable ? (
                        <>
                          <BotonLink href={`/reservar/${c.slug}`} className="mt-5 w-full md:w-auto">
                            Reservar plaza
                          </BotonLink>
                          <p className="mt-3 text-center text-xs md:text-left">
                            <Link href={listaEspera} className="font-semibold underline underline-offset-2">
                              ¿No te cuadran las fechas? Apúntate a la lista de espera
                            </Link>
                          </p>
                        </>
                      ) : (
                        <>
                          <BotonLink href={listaEspera} variante="oscuro" className="mt-5 w-full md:w-auto">
                            Apúntate a la lista de espera
                          </BotonLink>
                          <p className="mt-3 text-center text-xs text-z-text md:text-left">Te avisamos cuando abramos nuevas fechas.</p>
                        </>
                      )}
                    </div>
                  </article>
                );
              })}
            </div>
          </Contenedor>
        </section>

        {/* Testimonials */}
        <section aria-labelledby="opiniones" className="pb-10 md:pb-16">
          <Contenedor>
            <TituloSeccion id="opiniones" titulo="Lo Que Dicen Nuestras Alumnas" subtitulo="Ellas ya trabajan con lo que aprendieron" />
            <ul className="-mx-[18px] mt-6 flex snap-x snap-mandatory gap-3 overflow-x-auto px-[18px] pb-3 md:mx-0 md:mt-10 md:grid md:grid-cols-3 md:gap-4 md:overflow-visible md:px-0">
              {TESTIMONIOS.map((t, i) => (
                <li key={i} className="w-[85%] shrink-0 snap-start rounded-md border border-z-line bg-white p-5 md:w-auto">
                  <p role="img" className="flex gap-0.5 text-z-burgundy" aria-label="5 de 5 estrellas">
                    {Array.from({ length: 5 }, (_, s) => (
                      <StarIcon key={s} className="size-3.5 fill-current" aria-hidden />
                    ))}
                  </p>
                  <blockquote className="mt-3 text-[13px] leading-relaxed text-z-text">{t.texto}</blockquote>
                  <div className="mt-4 flex items-center gap-3">
                    <span aria-hidden className="inline-flex size-9 items-center justify-center rounded-full bg-z-photo text-[11px] font-bold text-[#4d4948]">
                      {t.nombre.replace(/[^A-Za-zÁÉÍÓÚÑáéíóúñ ]/g, "").split(" ").filter(Boolean).slice(0, 2).map((p) => p[0]).join("") || "··"}
                    </span>
                    <span>
                      <span className="block text-xs font-bold">{t.nombre}</span>
                      <span className="block text-[11px] text-z-muted">{t.detalle}</span>
                    </span>
                  </div>
                </li>
              ))}
            </ul>
            <div role="img" aria-label="Vídeo de alumnas el día de la graduación" className="mt-6 flex h-[200px] flex-col items-center justify-center gap-3 rounded-lg bg-z-photo md:mt-8 md:h-[350px]">
              <span className="inline-flex size-12 items-center justify-center rounded-full bg-z-burgundy text-white md:size-14">
                <PlayIcon className="size-5 fill-current" aria-hidden />
              </span>
              <span className="text-xs font-semibold text-[#4d4948]">Vídeo de alumnas el día de la graduación</span>
            </div>
            <p className="mt-6 text-center">
              <a href={SITIO.googleResenasUrl} target="_blank" rel="noopener noreferrer" className="inline-flex items-center gap-1.5 text-xs font-bold text-z-burgundy hover:underline">
                Ver todas las reseñas en Google <ArrowRightIcon className="size-3.5" aria-hidden />
              </a>
            </p>
          </Contenedor>
        </section>

        {/* Waiting list */}
        <section id="lista-espera" aria-labelledby="lista-espera-titulo" className="scroll-mt-4 bg-z-cream py-10 md:py-16">
          <Contenedor className="grid gap-6 md:grid-cols-2 md:gap-12">
            <div className="text-center md:text-left">
              <h2 id="lista-espera-titulo" className="text-[22px] font-bold leading-tight md:text-[30px]">
                Entra a la Lista de Espera
              </h2>
              <p className="mt-2 text-[13px] font-bold leading-snug text-z-burgundy md:mt-3 md:text-base">
                Sé la primera en enterarte cuando abramos nuevas fechas
              </p>
              <p className="mt-3 hidden text-sm leading-relaxed text-z-text md:block">
                Déjanos tus datos y la formación que te interesa. Te avisamos por WhatsApp o email en cuanto haya fechas nuevas,
                antes de anunciarlas.
              </p>
            </div>
            <div className="rounded-lg bg-white p-5 md:p-7">
              <ListaEsperaDiferida
                cursos={cursos.map(({ id, slug, nombre }) => ({ id, slug, nombre }))}
                cursoInicial={cursoPedido?.id}
                edicionPreferidaId={edicionPedida?.id}
                edicionPreferidaTexto={edicionPedida ? fechaEdicion(edicionPedida.inicio) : undefined}
              />
            </div>
          </Contenedor>
        </section>

        {/* Doubts */}
        <section aria-labelledby="dudas" className="bg-z-burgundy px-[18px] py-10 text-center text-white md:py-14">
          <h2 id="dudas" className="text-[22px] font-bold leading-tight md:text-[30px]">
            <span className="md:hidden">¿Dudas Sobre Qué Formación Elegir?</span>
            <span className="hidden md:inline">¿Tienes Dudas Sobre Qué Formación Elegir?</span>
          </h2>
          <p className="mx-auto mt-3 max-w-md text-[13px] leading-relaxed md:max-w-none md:text-sm">
            Escríbenos y te ayudamos a elegir según tu nivel y lo que quieres conseguir.
          </p>
          <BotonLink href={whatsappUrl("Hola, tengo dudas sobre qué formación elegir.")} variante="blanco" className="mt-5 w-full max-w-sm md:w-auto">
            Escríbenos por WhatsApp
          </BotonLink>
        </section>
      </main>
      <Pie />
    </>
  );
}

function Chip({ children }: { children: React.ReactNode }) {
  return <li className="rounded-md bg-z-cream px-3 py-1.5 text-[11px] font-semibold text-z-ink">{children}</li>;
}
