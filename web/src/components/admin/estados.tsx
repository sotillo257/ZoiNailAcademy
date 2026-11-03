"use client";

import { RefreshCwIcon, ShieldOffIcon, WifiOffIcon } from "lucide-react";
import * as React from "react";
import { Button } from "@/components/ui/button";
import { Card, Skeleton } from "@/components/ui/card";
import { isApiError, mensajeError } from "@/lib/admin/errors";

/** Error of a load: "Sin permiso" on 403, otherwise a banner with "Reintentar". */
export function ErrorCarga({ error, onReintentar }: { error: unknown; onReintentar: () => void }) {
  if (isApiError(error, 403)) return <SinPermiso />;
  return (
    <div
      role="alert"
      className="flex flex-col gap-3 rounded-xl border border-destructive/30 bg-destructive-soft p-4 text-destructive md:flex-row md:items-center md:justify-between"
    >
      <p className="flex items-start gap-2 text-sm">
        <WifiOffIcon className="mt-0.5 size-4 shrink-0" aria-hidden />
        {mensajeError(error)}
      </p>
      <Button variant="outline" size="sm" onClick={onReintentar}>
        <RefreshCwIcon /> Reintentar
      </Button>
    </div>
  );
}

export function SinPermiso() {
  return (
    <Card className="flex flex-col items-center gap-2 py-10 text-center">
      <ShieldOffIcon className="size-8 text-muted-foreground" aria-hidden />
      <h2 className="text-lg font-semibold">Sin permiso</h2>
      <p className="max-w-sm text-sm text-muted-foreground">Esta sección solo está disponible para el SuperAdmin.</p>
    </Card>
  );
}

export function Vacio({ children }: { children: React.ReactNode }) {
  return <Card className="py-10 text-center text-sm text-muted-foreground">{children}</Card>;
}

export function TarjetasSkeleton({ filas = 3 }: { filas?: number }) {
  return (
    <div className="grid gap-3" aria-busy="true" aria-label="Cargando">
      {Array.from({ length: filas }, (_, i) => (
        <Card key={i} className="grid gap-3">
          <Skeleton className="h-5 w-1/3" />
          <Skeleton className="h-4 w-2/3" />
          <Skeleton className="h-4 w-1/2" />
        </Card>
      ))}
    </div>
  );
}

export function Cabecera({
  titulo,
  descripcion,
  acciones,
}: {
  titulo: React.ReactNode;
  descripcion?: React.ReactNode;
  acciones?: React.ReactNode;
}) {
  return (
    <div className="mb-4 flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
      <div className="min-w-0">
        <h1 className="text-xl font-semibold md:text-2xl">{titulo}</h1>
        {descripcion ? <p className="mt-1 text-sm text-muted-foreground">{descripcion}</p> : null}
      </div>
      {acciones ? <div className="flex flex-wrap gap-2">{acciones}</div> : null}
    </div>
  );
}
