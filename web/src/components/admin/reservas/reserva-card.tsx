"use client";

import { AlertTriangleIcon, CheckIcon, ClockIcon, MessageCircleIcon, PhoneIcon } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { caducidad, formatearFechaHora } from "@/lib/fechas";
import { formatearImporte, telUrl, whatsappUrl } from "@/lib/formato";
import type { ReservaAdminDto } from "@/lib/admin/types";
import { cn } from "@/lib/utils";

export type TipoAccion = "confirmar" | "ampliar" | "cancelar" | "revisada";

export const NOMBRE_METODO = { Manual: "Transferencia / Bizum", Stripe: "Tarjeta" } as const;

/** The actions a booking of the pending list offers, by state (the API checks them again). */
export function accionesDisponibles(r: ReservaAdminDto): { tipo: TipoAccion; habilitada: boolean; motivo?: string }[] {
  if (r.requiereRevision) return [{ tipo: "revisada", habilitada: true }];
  if (r.estado === "Pendiente") {
    return r.metodo === "Manual"
      ? [
          { tipo: "confirmar", habilitada: true },
          { tipo: "ampliar", habilitada: true },
          { tipo: "cancelar", habilitada: true },
        ]
      : [{ tipo: "cancelar", habilitada: true }];
  }
  if (r.estado === "Expirada" && r.metodo === "Manual") {
    return r.plazaDisponible
      ? [{ tipo: "confirmar", habilitada: true }]
      : [{ tipo: "confirmar", habilitada: false, motivo: "La plaza ya está ocupada: habla con la persona antes de confirmar." }];
  }
  return [];
}

export function ReservaCard({
  reserva: r,
  ahora,
  onAccion,
}: {
  reserva: ReservaAdminDto;
  ahora: Date;
  onAccion: (tipo: TipoAccion, reserva: ReservaAdminDto) => void;
}) {
  const acciones = accionesDisponibles(r);
  const plazo = r.estado === "Pendiente" ? caducidad(r.expiraEn, ahora) : null;
  const mensajeWhatsapp = `Hola ${r.nombre.split(" ")[0] ?? ""}, te escribo por tu reserva ${r.codigo} de ${r.curso}.`;

  return (
    <Card
      data-testid="reserva-card"
      data-codigo={r.codigo}
      className={cn("grid gap-3", r.requiereRevision && "border-destructive/50 ring-1 ring-destructive/30")}
    >
      {r.requiereRevision ? (
        <div className="-mx-4 -mt-4 flex items-start gap-2 rounded-t-xl bg-destructive-soft px-4 py-2 text-sm font-medium text-destructive">
          <AlertTriangleIcon className="mt-0.5 size-4 shrink-0" aria-hidden />
          <span>Requiere revisión{r.notaInterna ? `: ${r.notaInterna}` : ""}</span>
        </div>
      ) : null}

      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="font-mono text-xl font-bold tracking-wide">{r.codigo}</p>
          <p className="truncate font-medium">{r.nombre}</p>
        </div>
        <div className="text-right">
          <p className="text-lg font-semibold">{formatearImporte(r.importe)}</p>
          {r.precioCurso > r.importe ? (
            <p className="text-xs text-muted-foreground">Resto en persona: {formatearImporte(r.precioCurso - r.importe)}</p>
          ) : null}
          <EstadoBadge reserva={r} />
        </div>
      </div>

      <div className="grid gap-1 text-sm text-muted-foreground">
        <p className="text-foreground">
          {r.curso} · {formatearFechaHora(r.inicio)}
        </p>
        <p>{NOMBRE_METODO[r.metodo]}</p>
        {plazo ? (
          <p className={cn("flex items-center gap-1.5", plazo.urgente && "font-medium text-destructive")}>
            <ClockIcon className="size-4" aria-hidden /> {plazo.texto}
          </p>
        ) : null}
        {r.estado === "Pendiente" && r.metodo === "Stripe" ? <p>Pago con tarjeta en curso: se confirma solo.</p> : null}
      </div>

      <div className="flex flex-wrap gap-2">
        <Button asChild variant="outline" size="sm">
          <a href={telUrl(r.telefono)}>
            <PhoneIcon /> {r.telefono}
          </a>
        </Button>
        <Button asChild variant="outline" size="sm">
          <a href={whatsappUrl(r.telefono, mensajeWhatsapp)} target="_blank" rel="noopener noreferrer">
            <MessageCircleIcon /> WhatsApp
          </a>
        </Button>
      </div>

      {acciones.length > 0 ? (
        <div className="grid gap-2 border-t pt-3">
          <div className="flex flex-col gap-2 md:flex-row md:flex-wrap">
            {acciones.map((a) => (
              <BotonAccion key={a.tipo} reserva={r} tipo={a.tipo} habilitada={a.habilitada} onAccion={onAccion} />
            ))}
          </div>
          {acciones.find((a) => a.motivo) ? (
            <p className="text-xs text-muted-foreground">{acciones.find((a) => a.motivo)?.motivo}</p>
          ) : null}
        </div>
      ) : null}
    </Card>
  );
}

function BotonAccion({
  reserva,
  tipo,
  habilitada,
  onAccion,
}: {
  reserva: ReservaAdminDto;
  tipo: TipoAccion;
  habilitada: boolean;
  onAccion: (tipo: TipoAccion, reserva: ReservaAdminDto) => void;
}) {
  const click = () => onAccion(tipo, reserva);
  switch (tipo) {
    case "confirmar":
      return (
        <Button size="lg" variant="success" disabled={!habilitada} onClick={click} className="md:h-10 md:text-sm">
          <CheckIcon /> {reserva.estado === "Expirada" ? "Confirmar igualmente" : "Confirmar pago"}
        </Button>
      );
    case "ampliar":
      return (
        <Button variant="outline" onClick={click}>
          Ampliar plazo
        </Button>
      );
    case "cancelar":
      return (
        <Button variant="ghost" className="text-destructive hover:bg-destructive-soft" onClick={click}>
          Cancelar reserva
        </Button>
      );
    case "revisada":
      return (
        <Button variant="destructive" onClick={click}>
          Marcar como revisada
        </Button>
      );
  }
}

export function EstadoBadge({ reserva: r }: { reserva: Pick<ReservaAdminDto, "estado"> }) {
  switch (r.estado) {
    case "Pagada":
      return <Badge variant="success">Pagada</Badge>;
    case "Pendiente":
      return <Badge variant="warning">Pendiente</Badge>;
    case "Expirada":
      return <Badge variant="muted">Caducada</Badge>;
    case "Cancelada":
      return <Badge variant="destructive">Cancelada</Badge>;
  }
}
