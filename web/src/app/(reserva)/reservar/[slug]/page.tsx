import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ANCHO_RESERVA, BarraReserva } from "@/components/sitio/reserva/barra";
import { Pasos } from "@/components/sitio/reserva/pasos";
import { ReservaForm } from "@/components/sitio/reserva/reserva-form";
import { type CursoDetalleDto, leerApi } from "@/lib/sitio/api";
import { SITIO, whatsappUrl } from "@/lib/sitio/datos";
import { formatearImporte } from "@/lib/formato";

type Props = { params: Promise<{ slug: string }>; searchParams: Promise<Record<string, string | string[] | undefined>> };

// Seats are never cached.
export const dynamic = "force-dynamic";

async function curso(slug: string) {
  return leerApi<CursoDetalleDto>(`/api/cursos/${encodeURIComponent(slug)}`, { fresco: true });
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const c = await curso((await params).slug);
  return {
    title: c ? `Reserva tu plaza · ${c.nombre}` : "Reserva tu plaza",
    robots: { index: false, follow: false },
  };
}

function uno(valor: string | string[] | undefined): string | null {
  return typeof valor === "string" && valor ? valor : null;
}

export default async function ReservarPage({ params, searchParams }: Props) {
  const { slug } = await params;
  const query = await searchParams;
  const c = await curso(slug);
  if (!c) notFound();

  const senal = c.precioReserva < c.precio;
  const listaEsperaHref = `/formaciones?curso=${encodeURIComponent(c.slug)}${uno(query.edicion) ? `&edicion=${uno(query.edicion)}` : ""}#lista-espera`;

  const resto = c.precio - c.precioReserva;

  return (
    <>
      <BarraReserva volver="/formaciones" />
      {/* Mobile: one column (steps, course, form). Desktop: form on the left, course summary sticky on the right. */}
      <main
        className={`${ANCHO_RESERVA} grid gap-4 pb-10 pt-5 md:pt-8 lg:grid-cols-[minmax(0,1fr)_380px] lg:items-start lg:gap-x-8 lg:gap-y-5 lg:pb-16 lg:pt-10`}
      >
        <Pasos actual={1} />
        <aside className="grid gap-4 lg:sticky lg:top-6 lg:col-start-2 lg:row-span-3 lg:row-start-1">
          <section className="rounded-[10px] bg-z-burgundy px-5 py-5 text-white lg:px-7 lg:py-7">
            <p className="text-[11px] font-bold uppercase tracking-[0.08em] opacity-90">Formación presencial · {SITIO.ciudad}</p>
            <h1 className="mt-1.5 text-[21px] font-bold leading-tight lg:text-[26px]">{c.nombre}</h1>
            <p className="mt-1.5 text-sm lg:hidden">
              Precio total: {formatearImporte(c.precio)}
              {senal ? <> · Reserva de plaza: {formatearImporte(c.precioReserva)}</> : null}
            </p>
            {senal ? <p className="mt-1 text-xs opacity-90 lg:hidden">El resto, {formatearImporte(resto)}, se paga en persona.</p> : null}
            <p className="mt-3 hidden text-sm leading-relaxed opacity-95 lg:block">{c.descripcionCorta}</p>
            <dl className="mt-5 hidden space-y-2.5 border-t border-white/25 pt-5 text-sm lg:block">
              {c.duracionTexto ? (
                <div className="flex justify-between gap-4">
                  <dt className="opacity-90">Duración</dt>
                  <dd className="text-right font-bold">{c.duracionTexto}</dd>
                </div>
              ) : null}
              <div className="flex justify-between gap-4">
                <dt className="opacity-90">Precio total</dt>
                <dd className="font-bold">{formatearImporte(c.precio)}</dd>
              </div>
              {senal ? (
                <>
                  <div className="flex justify-between gap-4">
                    <dt className="opacity-90">Pagas hoy (reserva)</dt>
                    <dd className="font-bold">{formatearImporte(c.precioReserva)}</dd>
                  </div>
                  <div className="flex justify-between gap-4">
                    <dt className="opacity-90">Resto, en persona</dt>
                    <dd className="font-bold">{formatearImporte(resto)}</dd>
                  </div>
                </>
              ) : null}
            </dl>
          </section>
          <section className="hidden rounded-[10px] border border-z-line bg-white p-6 lg:block" aria-labelledby="como-funciona">
            <h2 id="como-funciona" className="text-xs font-bold uppercase tracking-[0.08em] text-z-burgundy">
              Cómo funciona
            </h2>
            <ol className="mt-4 space-y-4 text-[13px] leading-relaxed text-z-text">
              {[
                ["Elige fecha y deja tus datos", "Solo te pedimos lo necesario para guardarte la plaza."],
                [
                  senal ? `Paga la reserva de ${formatearImporte(c.precioReserva)}` : `Paga ${formatearImporte(c.precio)}`,
                  "Con tarjeta queda confirmada al momento; por transferencia o Bizum, cuando nos llegue el comprobante.",
                ],
                ["Recibe la confirmación", senal ? "Por WhatsApp y email. El resto se paga en persona." : "Por WhatsApp y email."],
              ].map(([titulo, texto], i) => (
                <li key={titulo} className="flex gap-3">
                  <span className="inline-flex size-6 shrink-0 items-center justify-center rounded-full bg-z-cream text-xs font-bold text-z-ink">
                    {i + 1}
                  </span>
                  <span>
                    <span className="block font-bold text-z-ink">{titulo}</span>
                    {texto}
                  </span>
                </li>
              ))}
            </ol>
          </section>
        </aside>
        <div className="lg:col-start-1">
          <ReservaForm curso={c} edicionPedida={uno(query.edicion)} abandonada={uno(query.abandonada)} listaEsperaHref={listaEsperaHref} />
        </div>
        <p className="pt-1 text-center text-[13px] text-z-text lg:col-start-1 lg:pt-0 lg:text-left">
          ¿Prefieres hablar antes?{" "}
          <a
            href={whatsappUrl(`Hola, tengo una duda sobre ${c.nombre}.`)}
            target="_blank"
            rel="noopener noreferrer"
            className="font-bold text-z-ink underline underline-offset-2"
          >
            Escríbenos por WhatsApp
          </a>
        </p>
      </main>
    </>
  );
}
