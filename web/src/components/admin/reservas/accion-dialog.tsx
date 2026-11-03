"use client";

import { LoaderCircleIcon } from "lucide-react";
import * as React from "react";
import { toast } from "sonner";
import { useAuth } from "@/components/admin/auth-provider";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Campo, Input, Textarea } from "@/components/ui/input";
import { isApiError, mensajeError, SessionExpiredError } from "@/lib/admin/errors";
import { LIMITES, type ReservaAccionDto, type ReservaAdminDto } from "@/lib/admin/types";
import { formatearFechaHora } from "@/lib/fechas";
import { formatearImporte } from "@/lib/formato";
import { cn } from "@/lib/utils";
import type { TipoAccion } from "./reserva-card";

const HORAS_RAPIDAS = [12, 24, 48] as const;

const TITULOS: Record<TipoAccion, string> = {
  confirmar: "¿Confirmar el pago?",
  ampliar: "Ampliar el plazo de pago",
  cancelar: "¿Cancelar la reserva?",
  revisada: "Marcar como revisada",
};

/**
 * Confirmation dialog of a booking action: repeats code and amount, sends the `version` the list read and handles
 * `concurrencia` (someone else changed it) by reloading.
 */
export function AccionReservaDialog({
  accion,
  onClose,
  onHecho,
}: {
  accion: { tipo: TipoAccion; reserva: ReservaAdminDto } | null;
  onClose: () => void;
  /** After a success or a conflict: the caller reloads its list. */
  onHecho: (resultado: ReservaAccionDto | null) => void;
}) {
  return (
    <Dialog open={accion !== null} onOpenChange={(open) => !open && onClose()}>
      <DialogContent>
        {accion ? <Formulario key={`${accion.tipo}-${accion.reserva.id}`} {...accion} onClose={onClose} onHecho={onHecho} /> : null}
      </DialogContent>
    </Dialog>
  );
}

function Formulario({
  tipo,
  reserva,
  onClose,
  onHecho,
}: {
  tipo: TipoAccion;
  reserva: ReservaAdminDto;
  onClose: () => void;
  onHecho: (resultado: ReservaAccionDto | null) => void;
}) {
  const { session } = useAuth();
  const [enviando, setEnviando] = React.useState(false);
  const [texto, setTexto] = React.useState("");
  const [horas, setHoras] = React.useState<number | "personalizado">(24);
  const [horasPersonalizadas, setHorasPersonalizadas] = React.useState("72");
  const [errorCampo, setErrorCampo] = React.useState<string | undefined>();

  const horasElegidas = horas === "personalizado" ? Number(horasPersonalizadas) : horas;

  const enviar = async (e: React.FormEvent) => {
    e.preventDefault();
    if (enviando) return;
    setErrorCampo(undefined);

    let body: Record<string, unknown>;
    switch (tipo) {
      case "confirmar":
        body = { version: reserva.version };
        break;
      case "ampliar":
        if (!Number.isInteger(horasElegidas) || horasElegidas < 1 || horasElegidas > LIMITES.horasAmpliacionMaximas) {
          setErrorCampo(`Elige entre 1 y ${LIMITES.horasAmpliacionMaximas} horas.`);
          return;
        }
        body = { version: reserva.version, horas: horasElegidas };
        break;
      case "cancelar":
        body = { version: reserva.version, motivo: texto.trim() || null };
        break;
      case "revisada":
        if (!texto.trim()) {
          setErrorCampo("Escribe qué has revisado o hecho.");
          return;
        }
        body = { version: reserva.version, nota: texto.trim() };
        break;
    }

    const ruta = { confirmar: "confirmar", ampliar: "ampliar", cancelar: "cancelar", revisada: "marcar-revisada" }[tipo];
    setEnviando(true);
    try {
      const resultado = await session.json<ReservaAccionDto>(`/api/admin/reservas/${reserva.id}/${ruta}`, {
        method: "POST",
        body: JSON.stringify(body),
      });
      toast.success(
        {
          confirmar: "Pago confirmado, se ha enviado el email",
          ampliar: `Plazo ampliado ${horasElegidas} h`,
          cancelar: "Reserva cancelada",
          revisada: "Reserva marcada como revisada",
        }[tipo],
      );
      for (const aviso of resultado.avisos) toast.info(aviso);
      onHecho(resultado);
    } catch (error) {
      if (error instanceof SessionExpiredError) return;
      if (isApiError(error, 409, "concurrencia")) {
        toast.warning("Esta reserva acaba de cambiar");
        onHecho(null);
        return;
      }
      if (isApiError(error, 400)) {
        setErrorCampo(error.fieldError("nota") ?? error.fieldError("motivo") ?? error.fieldError("horas") ?? mensajeError(error));
        setEnviando(false);
        return;
      }
      toast.error(mensajeError(error));
      // A 409 (state changed, no seats…) means the list is out of date.
      if (isApiError(error, 409)) onHecho(null);
      else setEnviando(false);
    }
  };

  return (
    <form onSubmit={enviar} className="grid gap-4">
      <DialogHeader>
        <DialogTitle>{TITULOS[tipo]}</DialogTitle>
        <DialogDescription asChild>
          <div className="grid gap-0.5">
            <span className="font-mono text-base font-bold text-foreground">{reserva.codigo}</span>
            <span>
              {reserva.nombre} · <strong className="text-foreground">{formatearImporte(reserva.importe)}</strong>
            </span>
            <span>
              {reserva.curso} · {formatearFechaHora(reserva.inicio)}
            </span>
          </div>
        </DialogDescription>
      </DialogHeader>

      {tipo === "confirmar" ? (
        <p className="text-sm">
          Confirma solo si has recibido <strong>{formatearImporte(reserva.importe)}</strong> con el concepto{" "}
          <strong className="font-mono">{reserva.codigo}</strong>. Se enviará el email de confirmación.
        </p>
      ) : null}

      {tipo === "ampliar" ? (
        <fieldset className="grid gap-2">
          <legend className="mb-1 text-sm font-medium">Ampliar desde ahora</legend>
          <div className="grid grid-cols-4 gap-2">
            {HORAS_RAPIDAS.map((h) => (
              <OpcionHoras key={h} activa={horas === h} onClick={() => setHoras(h)}>
                {h} h
              </OpcionHoras>
            ))}
            <OpcionHoras activa={horas === "personalizado"} onClick={() => setHoras("personalizado")}>
              Otra
            </OpcionHoras>
          </div>
          {horas === "personalizado" ? (
            <Campo id="horas" label="Horas" error={errorCampo}>
              {(props) => (
                <Input
                  {...props}
                  type="number"
                  inputMode="numeric"
                  min={1}
                  max={LIMITES.horasAmpliacionMaximas}
                  value={horasPersonalizadas}
                  onChange={(e) => setHorasPersonalizadas(e.target.value)}
                />
              )}
            </Campo>
          ) : errorCampo ? (
            <p role="alert" className="text-sm text-destructive">
              {errorCampo}
            </p>
          ) : null}
        </fieldset>
      ) : null}

      {tipo === "cancelar" || tipo === "revisada" ? (
        <Campo
          id="texto-accion"
          label={tipo === "cancelar" ? "Motivo (opcional)" : "Nota (obligatoria)"}
          error={errorCampo}
        >
          {(props) => (
            <Textarea
              {...props}
              maxLength={LIMITES.notaReserva}
              value={texto}
              onChange={(e) => setTexto(e.target.value)}
              placeholder={tipo === "revisada" ? "Qué has comprobado o hecho (p. ej. devolución hecha)" : undefined}
            />
          )}
        </Campo>
      ) : null}

      {tipo === "confirmar" && errorCampo ? (
        <p role="alert" className="text-sm text-destructive">
          {errorCampo}
        </p>
      ) : null}

      <DialogFooter>
        <Button variant="outline" onClick={onClose} disabled={enviando}>
          Volver
        </Button>
        <Button
          type="submit"
          size="lg"
          className="md:h-10 md:text-sm"
          variant={tipo === "confirmar" ? "success" : tipo === "cancelar" ? "destructive" : "default"}
          disabled={enviando}
        >
          {enviando ? <LoaderCircleIcon className="animate-spin" /> : null}
          {
            {
              confirmar: `Sí, confirmar ${formatearImporte(reserva.importe)}`,
              ampliar: "Ampliar plazo",
              cancelar: "Sí, cancelar",
              revisada: "Marcar como revisada",
            }[tipo]
          }
        </Button>
      </DialogFooter>
    </form>
  );
}

function OpcionHoras({ activa, onClick, children }: { activa: boolean; onClick: () => void; children: React.ReactNode }) {
  return (
    <button
      type="button"
      aria-pressed={activa}
      onClick={onClick}
      className={cn(
        "h-11 rounded-lg border text-sm font-medium",
        activa ? "border-primary bg-secondary text-secondary-foreground" : "border-input hover:bg-muted",
      )}
    >
      {children}
    </button>
  );
}
