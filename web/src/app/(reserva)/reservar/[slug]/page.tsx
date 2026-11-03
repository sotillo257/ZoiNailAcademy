import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { BarraReserva } from "@/components/sitio/reserva/barra";
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

  return (
    <>
      <BarraReserva volver="/formaciones" />
      <main className="mx-auto max-w-[480px] space-y-4 px-[18px] pb-10 pt-5">
        <Pasos actual={1} />
        <section className="rounded-[10px] bg-z-burgundy px-5 py-5 text-white">
          <p className="text-[11px] font-bold uppercase tracking-[0.08em] opacity-90">Formación presencial · {SITIO.ciudad}</p>
          <h1 className="mt-1.5 text-[21px] font-bold leading-tight">{c.nombre}</h1>
          <p className="mt-1.5 text-sm">
            Precio total: {formatearImporte(c.precio)}
            {senal ? <> · Reserva de plaza: {formatearImporte(c.precioReserva)}</> : null}
          </p>
          {senal ? (
            <p className="mt-1 text-xs opacity-90">El resto, {formatearImporte(c.precio - c.precioReserva)}, se paga en persona.</p>
          ) : null}
        </section>
        <ReservaForm curso={c} edicionPedida={uno(query.edicion)} abandonada={uno(query.abandonada)} listaEsperaHref={listaEsperaHref} />
        <p className="pt-1 text-center text-[13px] text-z-text">
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
