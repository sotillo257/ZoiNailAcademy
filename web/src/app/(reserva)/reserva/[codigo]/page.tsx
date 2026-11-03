import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { BarraReserva } from "@/components/sitio/reserva/barra";
import { EstadoReserva } from "@/components/sitio/reserva/estado-reserva";
import { type ReservaPublicaDto, leerApi } from "@/lib/sitio/api";

type Props = { params: Promise<{ codigo: string }>; searchParams: Promise<{ origen?: string }> };

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  title: "Tu reserva",
  robots: { index: false, follow: false },
};

export default async function ReservaPage({ params, searchParams }: Props) {
  const { codigo } = await params;
  const { origen } = await searchParams;
  const reserva = await leerApi<ReservaPublicaDto>(`/api/reservas/${encodeURIComponent(codigo)}`, { fresco: true });
  if (!reserva) notFound();

  return (
    <>
      <BarraReserva volver="/formaciones" titulo={reserva.estado === "Pagada" ? "Tu reserva" : "Reserva tu plaza"} />
      <main className="mx-auto max-w-[480px] px-[18px] pb-10 pt-5">
        <EstadoReserva inicial={reserva} desdeStripe={origen === "stripe"} />
      </main>
    </>
  );
}
