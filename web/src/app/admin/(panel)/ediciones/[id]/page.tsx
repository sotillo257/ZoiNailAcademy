"use client";

import { DownloadIcon, LoaderCircleIcon, MailIcon, PencilIcon, PhoneIcon } from "lucide-react";
import Link from "next/link";
import { useParams } from "next/navigation";
import * as React from "react";
import { toast } from "sonner";
import { useAuth } from "@/components/admin/auth-provider";
import { EdicionBadges } from "@/components/admin/ediciones/edicion-badge";
import { ErrorCarga, TarjetasSkeleton, Vacio } from "@/components/admin/estados";
import { BarraOcupacion } from "@/components/admin/ocupacion";
import { AccionReservaDialog } from "@/components/admin/reservas/accion-dialog";
import { EstadoBadge, NOMBRE_METODO, ReservaCard, type TipoAccion } from "@/components/admin/reservas/reserva-card";
import { useResumen } from "@/components/admin/resumen-provider";
import { useAhora, useApi } from "@/components/admin/use-api";
import { Button } from "@/components/ui/button";
import { Card, Skeleton } from "@/components/ui/card";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { descargarArchivo } from "@/lib/admin/descargar";
import { mensajeError, SessionExpiredError } from "@/lib/admin/errors";
import type { ReservaAdminDto, ReservasEdicionDto } from "@/lib/admin/types";
import { formatearFechaHora, formatearFechaLarga } from "@/lib/fechas";
import { formatearImporte, telUrl } from "@/lib/formato";

export default function EdicionDetallePage() {
  const { id } = useParams<{ id: string }>();
  const { session } = useAuth();
  const ahora = useAhora();
  const { recargar: recargarResumen } = useResumen();
  const { data, error, cargando, recargar } = useApi<ReservasEdicionDto>(`/api/admin/ediciones/${id}/reservas`);
  const [tab, setTab] = React.useState("inscritos");
  // "Otras" (expired/cancelled) is loaded only when its tab is opened.
  const otras = useApi<ReservasEdicionDto>(tab === "otras" ? `/api/admin/ediciones/${id}/reservas?incluir=expiradas,canceladas` : null);
  const [accion, setAccion] = React.useState<{ tipo: TipoAccion; reserva: ReservaAdminDto } | null>(null);
  const [descargando, setDescargando] = React.useState(false);

  const inscritos = data?.reservas.filter((r) => r.estado === "Pagada") ?? [];
  const pendientes = data?.reservas.filter((r) => r.estado === "Pendiente") ?? [];

  const descargar = async () => {
    setDescargando(true);
    try {
      await descargarArchivo(session, `/api/admin/ediciones/${id}/reservas.csv`, "asistentes.csv");
    } catch (e) {
      if (!(e instanceof SessionExpiredError)) toast.error(mensajeError(e));
    } finally {
      setDescargando(false);
    }
  };

  if (error) return <ErrorCarga error={error} onReintentar={() => void recargar()} />;
  if (!data) return cargando ? <TarjetasSkeleton /> : null;

  const { edicion } = data;
  return (
    <>
      <Card className="mb-6 grid gap-4">
        <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
          <div className="min-w-0">
            <h1 className="text-xl font-semibold md:text-2xl">{edicion.curso}</h1>
            <p className="text-muted-foreground first-letter:uppercase">
              {formatearFechaLarga(edicion.inicio)} · {formatearFechaHora(edicion.inicio).split(", ")[1]}
            </p>
            <div className="mt-2">
              <EdicionBadges edicion={edicion} ahora={ahora} />
            </div>
          </div>
          <Button asChild variant="outline">
            <Link href={`/admin/ediciones/${edicion.id}/editar`}>
              <PencilIcon /> Editar
            </Link>
          </Button>
        </div>
        <BarraOcupacion plazas={edicion.plazas} pagadas={edicion.pagadas} pendientes={edicion.pendientes} />
        {edicion.notas ? <p className="rounded-lg bg-muted p-3 text-sm">{edicion.notas}</p> : null}
      </Card>

      <Tabs value={tab} onValueChange={setTab}>
        <div className="mb-4 flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
          <TabsList>
            <TabsTrigger value="inscritos">Inscritos ({inscritos.length})</TabsTrigger>
            <TabsTrigger value="pendientes">Pendientes ({pendientes.length})</TabsTrigger>
            <TabsTrigger value="otras">Otras</TabsTrigger>
          </TabsList>
          <Button variant="outline" onClick={() => void descargar()} disabled={descargando}>
            {descargando ? <LoaderCircleIcon className="animate-spin" /> : <DownloadIcon />}
            Descargar asistentes (CSV)
          </Button>
        </div>

        <TabsContent value="inscritos">
          {inscritos.length === 0 ? <Vacio>Todavía no hay inscritos.</Vacio> : <ListaInscritos reservas={inscritos} />}
        </TabsContent>

        <TabsContent value="pendientes">
          {pendientes.length === 0 ? (
            <Vacio>No hay pagos pendientes 🎉</Vacio>
          ) : (
            <div className="grid gap-3 md:grid-cols-2">
              {pendientes.map((r) => (
                <ReservaCard key={r.id} reserva={r} ahora={ahora} onAccion={(tipo, reserva) => setAccion({ tipo, reserva })} />
              ))}
            </div>
          )}
        </TabsContent>

        <TabsContent value="otras">
          {otras.error ? <ErrorCarga error={otras.error} onReintentar={() => void otras.recargar()} /> : null}
          {!otras.data && !otras.error ? <Skeleton className="h-24" /> : null}
          {otras.data && otras.data.reservas.length === 0 ? <Vacio>No hay reservas caducadas ni canceladas.</Vacio> : null}
          {otras.data && otras.data.reservas.length > 0 ? (
            <div className="grid gap-2">
              {otras.data.reservas.map((r) => (
                <Card key={r.id} className="flex flex-wrap items-center justify-between gap-2 py-3">
                  <div className="min-w-0">
                    <p className="font-medium">{r.nombre}</p>
                    <p className="font-mono text-xs text-muted-foreground">
                      {r.codigo} · {NOMBRE_METODO[r.metodo]} · {formatearImporte(r.importe)}
                    </p>
                    {r.notaInterna ? <p className="text-xs text-muted-foreground">{r.notaInterna}</p> : null}
                  </div>
                  <EstadoBadge reserva={r} />
                </Card>
              ))}
            </div>
          ) : null}
        </TabsContent>
      </Tabs>

      <AccionReservaDialog
        accion={accion}
        onClose={() => setAccion(null)}
        onHecho={() => {
          setAccion(null);
          void recargar();
          void recargarResumen();
          if (otras.data) void otras.recargar();
        }}
      />
    </>
  );
}

function ListaInscritos({ reservas }: { reservas: ReservaAdminDto[] }) {
  return (
    <>
      <div className="grid gap-3 md:hidden">
        {reservas.map((r) => (
          <Card key={r.id} className="grid gap-2" data-testid="inscrito">
            <div className="flex items-start justify-between gap-2">
              <p className="font-medium">{r.nombre}</p>
              <span className="font-mono text-xs text-muted-foreground">{r.codigo}</span>
            </div>
            <a href={telUrl(r.telefono)} className="flex items-center gap-2 text-sm text-primary">
              <PhoneIcon className="size-4" aria-hidden /> {r.telefono}
            </a>
            <a href={`mailto:${r.email}`} className="flex items-center gap-2 break-all text-sm text-primary">
              <MailIcon className="size-4 shrink-0" aria-hidden /> {r.email}
            </a>
            <p className="text-xs text-muted-foreground">
              {NOMBRE_METODO[r.metodo]} · pagada {r.pagadaEn ? formatearFechaHora(r.pagadaEn) : "—"}
            </p>
          </Card>
        ))}
      </div>
      <Card className="hidden overflow-x-auto p-0 md:block">
        <table className="w-full text-sm">
          <thead className="bg-muted/60 text-left text-xs uppercase tracking-wide text-muted-foreground">
            <tr>
              <th className="px-4 py-3 font-medium">Nombre</th>
              <th className="px-4 py-3 font-medium">Teléfono</th>
              <th className="px-4 py-3 font-medium">Email</th>
              <th className="px-4 py-3 font-medium">Método</th>
              <th className="px-4 py-3 font-medium">Pagada</th>
              <th className="px-4 py-3 font-medium">Código</th>
            </tr>
          </thead>
          <tbody className="divide-y">
            {reservas.map((r) => (
              <tr key={r.id} data-testid="inscrito">
                <td className="px-4 py-3 font-medium">{r.nombre}</td>
                <td className="px-4 py-3">
                  <a href={telUrl(r.telefono)} className="text-primary hover:underline">
                    {r.telefono}
                  </a>
                </td>
                <td className="px-4 py-3">
                  <a href={`mailto:${r.email}`} className="text-primary hover:underline">
                    {r.email}
                  </a>
                </td>
                <td className="px-4 py-3">{NOMBRE_METODO[r.metodo]}</td>
                <td className="px-4 py-3">{r.pagadaEn ? formatearFechaHora(r.pagadaEn) : "—"}</td>
                <td className="px-4 py-3 font-mono">{r.codigo}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </Card>
    </>
  );
}
