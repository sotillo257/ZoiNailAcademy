"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import * as React from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { InfoIcon, Loader2Icon } from "lucide-react";
import { type CursoDetalleDto, type EdicionPublicaDto, type ReservaPublicaDto, type ReservaTarjetaCreadaDto, llamarApi } from "@/lib/sitio/api";
import { edicionInicial, etiquetaEdicion } from "@/lib/sitio/ediciones";
import { errorDeReserva } from "@/lib/sitio/errores";
import { type ReservaFormulario, reservaEsquema } from "@/lib/sitio/esquemas";
import { cn } from "@/lib/utils";
import { Boton, BotonLink } from "../boton";
import { AvisoError, Campo, Casilla, Honeypot, claseControl } from "../campos";
import { Tarjeta } from "./pasos";

const METODOS = [
  { valor: "Stripe", titulo: "Tarjeta, Apple Pay o Google Pay", detalle: "Tu plaza queda confirmada al momento" },
  { valor: "Manual", titulo: "Transferencia o Bizum", detalle: "Envías el comprobante por WhatsApp y te confirmamos" },
] as const;

/** Step 1 of the booking: date, details and payment method → card (Stripe Checkout) or transfer/Bizum (step 2). */
export function ReservaForm({
  curso,
  edicionPedida,
  abandonada,
  listaEsperaHref,
}: {
  curso: CursoDetalleDto;
  edicionPedida: string | null;
  abandonada: string | null;
  listaEsperaHref: string;
}) {
  const router = useRouter();
  const [ediciones, setEdiciones] = React.useState<EdicionPublicaDto[]>(curso.ediciones);
  const [error, setError] = React.useState<{ mensaje: string; listaEspera: boolean } | null>(null);
  const [avisoAbandono, setAvisoAbandono] = React.useState(false);
  const [redirigiendo, setRedirigiendo] = React.useState(false);
  const abandonoEnviado = React.useRef(false);

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    setError: setErrorCampo,
    formState: { errors, isSubmitting },
  } = useForm<ReservaFormulario>({
    resolver: zodResolver(reservaEsquema),
    defaultValues: {
      edicionId: edicionInicial(curso.ediciones, edicionPedida),
      nombre: "",
      telefono: "",
      email: "",
      metodo: "Stripe",
      website: "",
    },
  });
  const metodo = watch("metodo");

  // Back from Stripe Checkout without paying: free the seat once and say so.
  React.useEffect(() => {
    if (!abandonada || abandonoEnviado.current) return;
    abandonoEnviado.current = true;
    setAvisoAbandono(true);
    void llamarApi(`/api/reservas/${encodeURIComponent(abandonada)}/abandonar`, { method: "POST" })
      .catch(() => undefined)
      .then(() => recargarFechas());
    const url = new URL(window.location.href);
    url.searchParams.delete("abandonada");
    router.replace(url.pathname + url.search, { scroll: false });
    // Runs once per abandoned code; recargarFechas only reads state through setters.
  }, [abandonada, router]); // eslint-disable-line react-hooks/exhaustive-deps

  async function recargarFechas() {
    try {
      const nuevas = await llamarApi<EdicionPublicaDto[]>(`/api/cursos/${encodeURIComponent(curso.slug)}/ediciones`);
      setEdiciones(nuevas);
      const actual = watch("edicionId");
      if (!nuevas.some((e) => e.id === actual && e.estado !== "Completo")) setValue("edicionId", edicionInicial(nuevas));
    } catch {
      // Keep the dates we have; the next submit will tell.
    }
  }

  const enviar = handleSubmit(async (datos) => {
    setError(null);
    const cuerpo = JSON.stringify({
      edicionId: datos.edicionId,
      nombre: datos.nombre,
      email: datos.email,
      telefono: datos.telefono,
      aceptaCondiciones: datos.aceptaCondiciones,
      website: datos.website || null,
    });
    try {
      if (datos.metodo === "Stripe") {
        const creada = await llamarApi<ReservaTarjetaCreadaDto>("/api/reservas/tarjeta", { method: "POST", body: cuerpo });
        setRedirigiendo(true);
        window.location.assign(creada.checkoutUrl);
      } else {
        const creada = await llamarApi<ReservaPublicaDto>("/api/reservas/manual", { method: "POST", body: cuerpo });
        setRedirigiendo(true);
        router.push(`/reserva/${encodeURIComponent(creada.codigo)}`);
      }
    } catch (e) {
      const fallo = errorDeReserva(e);
      for (const [campo, mensaje] of Object.entries(fallo.campos)) {
        if (campo in reservaEsquema.shape) setErrorCampo(campo as keyof ReservaFormulario, { message: mensaje });
      }
      setError({ mensaje: fallo.mensaje, listaEspera: fallo.ofrecerListaEspera });
      if (fallo.recargarFechas) await recargarFechas();
    }
  });

  const reservables = ediciones.filter((e) => e.estado !== "Completo");
  const ocupado = isSubmitting || redirigiendo;

  if (reservables.length === 0) {
    return (
      <Tarjeta className="text-center md:px-10 md:py-10">
        <p className="text-base font-bold">No hay fechas abiertas ahora mismo</p>
        <p className="mt-2 text-sm text-z-text">Apúntate a la lista de espera y te avisamos en cuanto haya nuevas fechas de {curso.nombre}.</p>
        <BotonLink href={listaEsperaHref} ancho className="mt-5">
          Avisarme de nuevas fechas
        </BotonLink>
      </Tarjeta>
    );
  }

  return (
    <Tarjeta className="md:p-7 lg:p-8">
      <form onSubmit={enviar} noValidate className="relative space-y-4 md:space-y-5">
        {avisoAbandono ? (
          <div role="status" className="flex gap-2 rounded-md bg-z-amber-soft px-4 py-3 text-sm text-z-amber">
            <InfoIcon className="mt-0.5 size-4 shrink-0" aria-hidden />
            No se completó el pago; tu plaza se ha liberado. Puedes intentarlo de nuevo.
          </div>
        ) : null}

        <Campo id="reserva-edicion" label="Edición" error={errors.edicionId?.message}>
          {(p) => (
            <select {...p} {...register("edicionId")} className={`${claseControl} h-12 cursor-pointer`}>
              {ediciones.map((e) => (
                <option key={e.id} value={e.id} disabled={e.estado === "Completo"}>
                  {etiquetaEdicion(e)}
                </option>
              ))}
            </select>
          )}
        </Campo>
        {ediciones.some((e) => e.estado === "Completo") ? (
          <p className="-mt-2 text-xs text-z-text">
            ¿Querías una fecha completa?{" "}
            <Link href={listaEsperaHref} className="font-bold text-z-burgundy underline">
              Avisarme si se libera una plaza
            </Link>
          </p>
        ) : null}
        <Campo id="reserva-nombre" label="Nombre y apellidos" error={errors.nombre?.message}>
          {(p) => <input {...p} {...register("nombre")} autoComplete="name" className={`${claseControl} h-12`} />}
        </Campo>
        <div className="grid gap-4 md:grid-cols-2 md:gap-5">
          <Campo id="reserva-telefono" label="WhatsApp" error={errors.telefono?.message}>
            {(p) => <input {...p} {...register("telefono")} type="tel" autoComplete="tel" inputMode="tel" placeholder="612 345 678" className={`${claseControl} h-12`} />}
          </Campo>
          <Campo id="reserva-email" label="Email" error={errors.email?.message}>
            {(p) => <input {...p} {...register("email")} type="email" autoComplete="email" inputMode="email" className={`${claseControl} h-12`} />}
          </Campo>
        </div>

        <fieldset>
          <legend className="mb-2.5 text-sm font-bold">¿Cómo quieres pagar la reserva?</legend>
          <div className="grid gap-2.5 md:grid-cols-2 md:gap-3">
            {METODOS.map((m) => {
              const elegido = metodo === m.valor;
              return (
                <label
                  key={m.valor}
                  className={cn(
                    "flex cursor-pointer items-start gap-3.5 rounded-[10px] border px-4 py-3.5 transition-colors",
                    elegido ? "border-z-burgundy bg-[#fbf3f3] ring-1 ring-z-burgundy" : "border-[#dcd6d5] bg-white",
                  )}
                >
                  <input type="radio" value={m.valor} {...register("metodo")} className="mt-1 size-[18px] shrink-0 cursor-pointer accent-z-burgundy" />
                  <span>
                    <span className="block text-sm font-bold">{m.titulo}</span>
                    <span className="mt-0.5 block text-xs text-z-text">{m.detalle}</span>
                  </span>
                </label>
              );
            })}
          </div>
        </fieldset>

        <Casilla id="reserva-condiciones" error={errors.aceptaCondiciones?.message} {...register("aceptaCondiciones")}>
          Acepto las{" "}
          <Link href="/condiciones" target="_blank" className="underline">
            condiciones de reserva
          </Link>{" "}
          y la{" "}
          <Link href="/privacidad" target="_blank" className="underline">
            política de privacidad
          </Link>
        </Casilla>
        <Honeypot {...register("website")} />

        {error ? (
          <AvisoError>
            {error.mensaje}
            {error.listaEspera ? (
              <>
                {" "}
                <Link href={listaEsperaHref} className="font-bold underline">
                  Apúntate a la lista de espera
                </Link>
              </>
            ) : null}
          </AvisoError>
        ) : null}

        <Boton type="submit" ancho disabled={ocupado} aria-busy={ocupado} className="h-12 text-sm md:h-[52px]">
          {ocupado ? <Loader2Icon className="size-4 animate-spin" aria-hidden /> : null}
          {redirigiendo && metodo === "Stripe" ? "Abriendo el pago…" : "Continuar"}
        </Boton>
      </form>
    </Tarjeta>
  );
}
