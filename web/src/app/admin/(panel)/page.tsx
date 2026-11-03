"use client";

import { AlertTriangleIcon, ChevronRightIcon, ClockIcon, UsersIcon } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { BuscadorReservas } from "@/components/admin/buscador";
import { Cabecera, ErrorCarga, Vacio } from "@/components/admin/estados";
import { BarraOcupacion } from "@/components/admin/ocupacion";
import { useResumen } from "@/components/admin/resumen-provider";
import { Card, Skeleton } from "@/components/ui/card";
import { formatearFechaHora } from "@/lib/fechas";
import { cn } from "@/lib/utils";

export default function PortadaPage() {
  const router = useRouter();
  const { resumen, error, cargando, recargar } = useResumen();

  return (
    <>
      <BuscadorReservas
        className="mb-5"
        onBuscar={(q) => router.push(q ? `/admin/reservas?q=${encodeURIComponent(q)}` : "/admin/reservas")}
      />
      <Cabecera titulo="Inicio" />

      {error && !resumen ? <ErrorCarga error={error} onReintentar={() => void recargar()} /> : null}

      {!resumen && cargando ? (
        <div className="grid gap-3 md:grid-cols-3">
          <Skeleton className="h-28" />
          <Skeleton className="h-28" />
          <Skeleton className="h-28" />
        </div>
      ) : null}

      {resumen ? (
        <>
          <div className="grid gap-3 md:grid-cols-3">
            <Tarjeta href="/admin/reservas" titulo="Pendientes de pago" valor={resumen.pendientesDePago} icono={ClockIcon}>
              {resumen.pendientesQueCaducanHoy > 0
                ? `${resumen.pendientesQueCaducanHoy} caducan hoy`
                : "Ninguna caduca hoy"}
            </Tarjeta>
            <Tarjeta
              href="/admin/reservas"
              titulo="Requieren revisión"
              valor={resumen.requierenRevision}
              icono={AlertTriangleIcon}
              alerta={resumen.requierenRevision > 0}
            >
              {resumen.requierenRevision > 0 ? "Revísalas cuanto antes" : "Todo en orden"}
            </Tarjeta>
            <Tarjeta
              href="/admin/lista-espera"
              titulo="Interesados sin contactar"
              valor={resumen.listaEsperaSinContactar}
              icono={UsersIcon}
            >
              En la lista de espera
            </Tarjeta>
          </div>

          <h2 className="mb-3 mt-8 text-lg font-semibold">Próximas fechas</h2>
          {resumen.proximasEdiciones.length === 0 ? (
            <Vacio>
              No hay fechas programadas.{" "}
              <Link href="/admin/ediciones/nueva" className="font-medium text-primary underline">
                Crear una
              </Link>
            </Vacio>
          ) : (
            <div className="grid gap-3 md:grid-cols-2">
              {resumen.proximasEdiciones.map((e) => (
                <Link key={e.id} href={`/admin/ediciones/${e.id}`} className="group">
                  <Card className="grid gap-3 transition-colors group-hover:border-primary/40">
                    <div className="flex items-start justify-between gap-2">
                      <div className="min-w-0">
                        <p className="truncate font-medium">{e.curso}</p>
                        <p className="text-sm text-muted-foreground">{formatearFechaHora(e.inicio)}</p>
                      </div>
                      <ChevronRightIcon className="size-5 shrink-0 text-muted-foreground" aria-hidden />
                    </div>
                    <BarraOcupacion plazas={e.plazas} pagadas={e.pagadas} pendientes={e.pendientes} />
                  </Card>
                </Link>
              ))}
            </div>
          )}
        </>
      ) : null}
    </>
  );
}

function Tarjeta({
  href,
  titulo,
  valor,
  icono: Icono,
  alerta,
  children,
}: {
  href: string;
  titulo: string;
  valor: number;
  icono: React.ComponentType<{ className?: string }>;
  alerta?: boolean;
  children: React.ReactNode;
}) {
  return (
    <Link href={href} className="group">
      <Card
        className={cn(
          "flex items-center gap-4 transition-colors group-hover:border-primary/40",
          alerta && "border-destructive/50 bg-destructive-soft",
        )}
      >
        <div
          className={cn(
            "flex size-11 shrink-0 items-center justify-center rounded-full",
            alerta ? "bg-destructive text-white" : "bg-secondary text-secondary-foreground",
          )}
        >
          <Icono className="size-5" />
        </div>
        <div className="min-w-0">
          <p className={cn("text-sm", alerta ? "font-medium text-destructive" : "text-muted-foreground")}>{titulo}</p>
          <p className={cn("text-2xl font-semibold", alerta && "text-destructive")}>{valor}</p>
          <p className="truncate text-xs text-muted-foreground">{children}</p>
        </div>
      </Card>
    </Link>
  );
}
