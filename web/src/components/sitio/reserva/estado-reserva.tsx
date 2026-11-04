"use client";

import * as React from "react";
import { CheckCircle2Icon, ClockIcon, CopyIcon, Loader2Icon, XCircleIcon } from "lucide-react";
import { formatearImporte } from "@/lib/formato";
import { type ReservaPublicaDto, llamarApi } from "@/lib/sitio/api";
import { fechaEdicion } from "@/lib/sitio/ediciones";
import { cn } from "@/lib/utils";
import { BotonLink, clasesBoton } from "../boton";
import { IconoWhatsapp } from "../iconos";
import { Pasos, Tarjeta } from "./pasos";

const SONDEO_MS = 2000;
const SONDEO_MAX_MS = 30_000;

/** Hours left until the manual booking expires (what "Te la guardamos durante X horas" says). */
export function horasRestantes(expiraEn: string, ahora: number): number {
  return Math.max(0, Math.round((new Date(expiraEn).getTime() - ahora) / 3_600_000));
}

/** Text copied by "Copiar datos". */
export function textoDatosPago(r: ReservaPublicaDto): string {
  const d = r.datosPago;
  if (!d) return "";
  return [
    `Importe: ${formatearImporte(r.importe)}`,
    `Titular: ${d.titular}`,
    `IBAN: ${d.iban}`,
    `Bizum: ${d.bizumTelefono}`,
    `Concepto: ${d.concepto}`,
  ].join("\n");
}

/**
 * /reserva/[codigo]: transfer/Bizum instructions (step 2 of the design), "confirmando tu pago" after Stripe
 * Checkout (polls until the webhook lands), paid, expired or cancelled.
 */
export function EstadoReserva({ inicial, desdeStripe }: { inicial: ReservaPublicaDto; desdeStripe: boolean }) {
  const [reserva, setReserva] = React.useState(inicial);
  const [sondeoAgotado, setSondeoAgotado] = React.useState(false);
  const esperandoStripe = reserva.metodo === "Stripe" && reserva.estado === "Pendiente";

  React.useEffect(() => {
    if (!esperandoStripe) return;
    const inicio = Date.now();
    const id = window.setInterval(async () => {
      if (Date.now() - inicio > SONDEO_MAX_MS) {
        window.clearInterval(id);
        setSondeoAgotado(true);
        return;
      }
      try {
        const nueva = await llamarApi<ReservaPublicaDto>(`/api/reservas/${encodeURIComponent(reserva.codigo)}`);
        setReserva(nueva);
        if (nueva.estado !== "Pendiente") window.clearInterval(id);
      } catch {
        // Try again on the next tick.
      }
    }, SONDEO_MS);
    return () => window.clearInterval(id);
  }, [esperandoStripe, reserva.codigo]);

  if (reserva.estado === "Pagada") return <Pagada reserva={reserva} />;
  if (reserva.estado === "Expirada" || reserva.estado === "Cancelada") return <Inactiva />;
  if (reserva.metodo === "Stripe") return <ConfirmandoPago agotado={sondeoAgotado || !desdeStripe} />;
  return <InstruccionesPago reserva={reserva} />;
}

/** Short states (confirming, paid, expired) stay a centered column on desktop, like a confirmation card. */
function Centrado({ className, children }: { className?: string; children: React.ReactNode }) {
  return <div className={cn("mx-auto w-full max-w-[560px]", className)}>{children}</div>;
}

function Cabecera({ icono, tono, titulo, children }: { icono: React.ReactNode; tono: "ambar" | "verde" | "rojo"; titulo: string; children: React.ReactNode }) {
  const fondo = { ambar: "bg-z-amber-soft text-z-amber", verde: "bg-z-green-soft text-z-green", rojo: "bg-z-red-soft text-z-burgundy" }[tono];
  return (
    <Tarjeta className="px-6 py-6 text-center md:px-10 md:py-9">
      <span className={`mx-auto inline-flex size-[52px] items-center justify-center rounded-full ${fondo}`}>{icono}</span>
      <h1 className="mt-4 text-[21px] font-bold leading-tight md:text-[26px]">{titulo}</h1>
      <div className="mx-auto mt-2 max-w-[520px] text-[13px] leading-relaxed text-z-text md:text-sm">{children}</div>
    </Tarjeta>
  );
}

function Fila({ etiqueta, valor }: { etiqueta: string; valor: string }) {
  return (
    <div className="flex items-baseline justify-between gap-4 border-b border-z-line py-2.5 last:border-0">
      <dt className="text-sm text-z-text">{etiqueta}</dt>
      <dd className="text-right text-sm font-bold break-all">{valor}</dd>
    </div>
  );
}

function InstruccionesPago({ reserva }: { reserva: ReservaPublicaDto }) {
  const [copiado, setCopiado] = React.useState(false);
  const [ahora] = React.useState(() => Date.now());
  const d = reserva.datosPago;
  const horas = reserva.expiraEn ? horasRestantes(reserva.expiraEn, ahora) : 0;
  const resto = reserva.precioCurso - reserva.importe;

  const copiar = async () => {
    try {
      await navigator.clipboard.writeText(textoDatosPago(reserva));
      setCopiado(true);
      window.setTimeout(() => setCopiado(false), 2500);
    } catch {
      // Clipboard blocked: the details stay on screen.
    }
  };

  return (
    <div className="space-y-4">
      <Pasos actual={2} />
      <Cabecera icono={<ClockIcon className="size-6" aria-hidden />} tono="ambar" titulo="Tu plaza está pre-reservada">
        Te la guardamos durante <strong className="text-z-ink">{horas} horas</strong>. Haz el pago y envíanos el comprobante para
        confirmarla.
        {reserva.expiraEn ? <span className="mt-1 block text-xs">Hasta el {fechaEdicion(reserva.expiraEn)}</span> : null}
      </Cabecera>

      <div className="grid gap-4 md:grid-cols-2 md:items-stretch">
        {d ? (
          <Tarjeta className="md:p-7">
            <h2 className="text-xs font-bold uppercase tracking-[0.08em] text-z-burgundy">Paso 1 · Haz el pago</h2>
            <dl className="mt-2">
              <Fila etiqueta="Importe" valor={formatearImporte(reserva.importe)} />
              <Fila etiqueta="Titular" valor={d.titular} />
              <Fila etiqueta="IBAN" valor={d.iban} />
              <Fila etiqueta="Bizum" valor={d.bizumTelefono} />
              <Fila etiqueta="Concepto" valor={d.concepto} />
            </dl>
            <button type="button" onClick={copiar} className={`${clasesBoton("contorno", "md", true)} mt-3`}>
              {copiado ? <CheckCircle2Icon className="size-4 text-z-green" aria-hidden /> : <CopyIcon className="size-4" aria-hidden />}
              {copiado ? "Datos copiados" : "Copiar datos"}
            </button>
            <p className="sr-only" role="status">
              {copiado ? "Datos copiados" : ""}
            </p>
          </Tarjeta>
        ) : null}

        {reserva.whatsappUrl ? (
          <Tarjeta className="flex flex-col md:p-7">
            <h2 className="text-xs font-bold uppercase tracking-[0.08em] text-z-burgundy">Paso 2 · Envía el comprobante</h2>
            <p className="mt-2 text-[13px] leading-relaxed text-z-text">
              Pulsa el botón y se abrirá WhatsApp con un mensaje ya escrito. Solo tienes que adjuntar la captura del pago.
            </p>
            <ul className="mb-6 mt-4 hidden space-y-2.5 text-[13px] text-z-text md:block">
              {[
                <>
                  El mensaje ya lleva tu código <strong className="text-z-ink">{reserva.codigo}</strong>
                </>,
                "Adjunta la captura o el justificante del pago",
                "Te confirmamos la plaza por WhatsApp y email",
              ].map((texto, i) => (
                <li key={i} className="flex gap-2">
                  <CheckCircle2Icon className="mt-px size-4 shrink-0 text-z-green" aria-hidden />
                  <span>{texto}</span>
                </li>
              ))}
            </ul>
            <BotonLink href={reserva.whatsappUrl} variante="whatsapp" ancho className="mt-4 h-14 text-[13px] md:mt-auto">
              <IconoWhatsapp className="size-5 shrink-0" />
              Enviar comprobante por WhatsApp
            </BotonLink>
          </Tarjeta>
        ) : null}
      </div>

      <p className="px-2 text-center text-xs leading-relaxed text-z-text">
        Recibirás la confirmación de tu plaza por WhatsApp y email. También te hemos enviado estos datos por email.
        {resto > 0 ? ` El resto del curso, ${formatearImporte(resto)}, se paga en persona.` : ""}
      </p>
    </div>
  );
}

function ConfirmandoPago({ agotado }: { agotado: boolean }) {
  return agotado ? (
    <Centrado>
      <Cabecera icono={<ClockIcon className="size-6" aria-hidden />} tono="ambar" titulo="Tu pago se está procesando">
        Recibirás un email de confirmación en cuanto se complete. Si tarda, escríbenos con tu código de reserva.
      </Cabecera>
    </Centrado>
  ) : (
    <Centrado>
      <div role="status" aria-live="polite">
        <Cabecera icono={<Loader2Icon className="size-6 animate-spin" aria-hidden />} tono="ambar" titulo="Confirmando tu pago…">
          Un momento, estamos recibiendo la confirmación del banco.
        </Cabecera>
      </div>
    </Centrado>
  );
}

function Pagada({ reserva }: { reserva: ReservaPublicaDto }) {
  const resto = reserva.precioCurso - reserva.importe;
  return (
    <Centrado className="space-y-4">
      <Cabecera icono={<CheckCircle2Icon className="size-6" aria-hidden />} tono="verde" titulo="¡Tu plaza está confirmada!">
        Te hemos enviado un email con todos los detalles.
      </Cabecera>
      <Tarjeta className="md:px-7">
        <dl>
          <Fila etiqueta="Formación" valor={reserva.curso} />
          <Fila etiqueta="Fecha" valor={fechaEdicion(reserva.inicio)} />
          <Fila etiqueta="Código" valor={reserva.codigo} />
          <Fila etiqueta={resto > 0 ? "Reserva pagada" : "Pagado"} valor={formatearImporte(reserva.importe)} />
          {resto > 0 ? <Fila etiqueta="Resto en persona" valor={formatearImporte(resto)} /> : null}
        </dl>
      </Tarjeta>
      <BotonLink href="/" variante="oscuro" ancho>
        Volver al inicio
      </BotonLink>
    </Centrado>
  );
}

function Inactiva() {
  return (
    <Centrado className="space-y-4">
      <Cabecera icono={<XCircleIcon className="size-6" aria-hidden />} tono="rojo" titulo="Esta reserva ya no está activa">
        El plazo para pagar terminó o la reserva se canceló. Si quieres, vuelve a reservar tu plaza.
      </Cabecera>
      <BotonLink href="/formaciones#calendario" ancho>
        Volver a reservar
      </BotonLink>
    </Centrado>
  );
}
