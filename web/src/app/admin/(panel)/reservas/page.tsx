"use client";

import { useRouter, useSearchParams } from "next/navigation";
import * as React from "react";
import { BuscadorReservas } from "@/components/admin/buscador";
import { Cabecera, ErrorCarga, TarjetasSkeleton, Vacio } from "@/components/admin/estados";
import { AccionReservaDialog } from "@/components/admin/reservas/accion-dialog";
import { ReservaCard, type TipoAccion } from "@/components/admin/reservas/reserva-card";
import { useResumen } from "@/components/admin/resumen-provider";
import { useAhora, useApi } from "@/components/admin/use-api";
import { Button } from "@/components/ui/button";
import type { ReservaAccionDto, ReservaAdminDto } from "@/lib/admin/types";

export default function ReservasPage() {
  return (
    <React.Suspense fallback={<TarjetasSkeleton />}>
      <Reservas />
    </React.Suspense>
  );
}

function Reservas() {
  const router = useRouter();
  const params = useSearchParams();
  const q = params.get("q") ?? "";
  const ahora = useAhora();
  const { recargar: recargarResumen } = useResumen();
  const ruta = `/api/admin/reservas/pendientes${q ? `?q=${encodeURIComponent(q)}` : ""}`;
  const { data, error, cargando, recargar, setData } = useApi<ReservaAdminDto[]>(ruta);
  const [accion, setAccion] = React.useState<{ tipo: TipoAccion; reserva: ReservaAdminDto } | null>(null);

  const revision = data?.filter((r) => r.requiereRevision) ?? [];
  const pendientes = data?.filter((r) => !r.requiereRevision && r.estado === "Pendiente") ?? [];
  const caducadas = data?.filter((r) => !r.requiereRevision && r.estado === "Expirada") ?? [];

  const abrir = (tipo: TipoAccion, reserva: ReservaAdminDto) => setAccion({ tipo, reserva });

  const hecho = (resultado: ReservaAccionDto | null) => {
    const id = accion?.reserva.id;
    setAccion(null);
    // A paid or cancelled booking leaves the list at once; the reload brings the rest up to date.
    if (resultado && id && (resultado.reserva.estado === "Pagada" || resultado.reserva.estado === "Cancelada")) {
      setData((actual) => actual?.filter((r) => r.id !== id));
    }
    void recargar();
    void recargarResumen();
  };

  const buscar = (nuevo: string) =>
    router.replace(nuevo ? `/admin/reservas?q=${encodeURIComponent(nuevo)}` : "/admin/reservas");

  return (
    <>
      <BuscadorReservas className="mb-5" valorInicial={q} onBuscar={buscar} />
      <Cabecera
        titulo={q ? `Resultados de “${q}”` : "Pendientes de pago"}
        acciones={
          q ? (
            <Button variant="outline" size="sm" onClick={() => buscar("")}>
              Quitar búsqueda
            </Button>
          ) : null
        }
      />

      {error ? <ErrorCarga error={error} onReintentar={() => void recargar()} /> : null}
      {!data && cargando ? <TarjetasSkeleton /> : null}

      {data ? (
        <div className="grid gap-8">
          {revision.length > 0 ? (
            <Seccion titulo="Requieren revisión" alerta>
              {revision.map((r) => (
                <ReservaCard key={r.id} reserva={r} ahora={ahora} onAccion={abrir} />
              ))}
            </Seccion>
          ) : null}

          <Seccion titulo={revision.length > 0 || caducadas.length > 0 ? "Pendientes de pago" : undefined}>
            {pendientes.length === 0 ? (
              <Vacio>{q ? "No hay reservas pendientes que coincidan." : "No hay pagos pendientes 🎉"}</Vacio>
            ) : (
              pendientes.map((r) => <ReservaCard key={r.id} reserva={r} ahora={ahora} onAccion={abrir} />)
            )}
          </Seccion>

          {caducadas.length > 0 ? (
            <Seccion titulo="Caducadas (últimos 7 días)">
              {caducadas.map((r) => (
                <ReservaCard key={r.id} reserva={r} ahora={ahora} onAccion={abrir} />
              ))}
            </Seccion>
          ) : null}
        </div>
      ) : null}

      <AccionReservaDialog accion={accion} onClose={() => setAccion(null)} onHecho={hecho} />
    </>
  );
}

function Seccion({ titulo, alerta, children }: { titulo?: string; alerta?: boolean; children: React.ReactNode }) {
  return (
    <section className="grid gap-3">
      {titulo ? <h2 className={alerta ? "text-lg font-semibold text-destructive" : "text-lg font-semibold"}>{titulo}</h2> : null}
      <div className="grid gap-3 md:grid-cols-2">{children}</div>
    </section>
  );
}
