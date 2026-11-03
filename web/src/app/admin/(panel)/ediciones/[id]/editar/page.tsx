"use client";

import { LoaderCircleIcon } from "lucide-react";
import { useParams, useRouter } from "next/navigation";
import * as React from "react";
import { toast } from "sonner";
import { useAuth } from "@/components/admin/auth-provider";
import { EdicionForm } from "@/components/admin/ediciones/edicion-form";
import { Cabecera, ErrorCarga, TarjetasSkeleton } from "@/components/admin/estados";
import { useApi } from "@/components/admin/use-api";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { isApiError, mensajeError, SessionExpiredError } from "@/lib/admin/errors";
import type { CursoOpcionDto, EdicionEditadaDto, ReservasEdicionDto } from "@/lib/admin/types";
import { formatearFechaHora } from "@/lib/fechas";

const TODAS = "pagadas,pendientes,expiradas,canceladas";

export default function EditarEdicionPage() {
  const { id } = useParams<{ id: string }>();
  const cursos = useApi<CursoOpcionDto[]>("/api/admin/cursos");
  // Every booking, to know whether the edition can be deleted.
  const { data, error, recargar } = useApi<ReservasEdicionDto>(`/api/admin/ediciones/${id}/reservas?incluir=${TODAS}`);
  const cargaError = error ?? cursos.error;

  return (
    <>
      <Cabecera
        titulo="Editar fecha"
        descripcion={data ? `${data.edicion.curso} · ${formatearFechaHora(data.edicion.inicio)}` : "Fecha y hora en hora de Madrid."}
      />
      {cargaError ? (
        <ErrorCarga
          error={cargaError}
          onReintentar={() => {
            void recargar();
            void cursos.recargar();
          }}
        />
      ) : null}
      {data && cursos.data ? (
        <>
          <EdicionForm key={data.edicion.version} cursos={cursos.data} edicion={data.edicion} onConflicto={() => void recargar()} />
          <ZonaPeligro datos={data} onCambio={() => void recargar()} />
        </>
      ) : !cargaError ? (
        <TarjetasSkeleton filas={1} />
      ) : null}
    </>
  );
}

function ZonaPeligro({ datos, onCambio }: { datos: ReservasEdicionDto; onCambio: () => void }) {
  const { session } = useAuth();
  const router = useRouter();
  const [dialogo, setDialogo] = React.useState<"cancelar" | "borrar" | null>(null);
  const [enviando, setEnviando] = React.useState(false);
  const { edicion, reservas } = datos;
  const empezada = new Date(edicion.inicio) <= new Date();
  const puedeCancelar = edicion.estado !== "Cancelada" && !empezada;
  const puedeBorrar = reservas.length === 0;

  if (!puedeCancelar && !puedeBorrar) return null;

  const ejecutar = async () => {
    setEnviando(true);
    try {
      if (dialogo === "cancelar") {
        const resultado = await session.json<EdicionEditadaDto>(`/api/admin/ediciones/${edicion.id}`, {
          method: "PUT",
          body: JSON.stringify({
            inicio: edicion.inicio,
            plazas: edicion.plazas,
            estado: "Cancelada",
            notas: edicion.notas,
            version: edicion.version,
          }),
        });
        toast.success("Fecha cancelada");
        for (const aviso of resultado.avisos) toast.info(aviso);
        setDialogo(null);
        onCambio();
      } else {
        await session.json<void>(`/api/admin/ediciones/${edicion.id}`, { method: "DELETE" });
        toast.success("Fecha borrada");
        router.push("/admin/ediciones");
      }
    } catch (error) {
      if (error instanceof SessionExpiredError) return;
      if (isApiError(error, 409, "concurrencia")) toast.warning("Alguien ha modificado esta fecha. Recarga para ver los cambios.");
      else toast.error(mensajeError(error));
      setDialogo(null);
      onCambio();
    } finally {
      setEnviando(false);
    }
  };

  return (
    <Card className="mt-10 grid max-w-xl gap-4 border-destructive/40">
      <h2 className="font-semibold text-destructive">Zona de peligro</h2>
      {puedeCancelar ? (
        <div className="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
          <p className="text-sm text-muted-foreground">Cancela la fecha: deja de verse en la web y no se puede reactivar.</p>
          <Button variant="destructive" onClick={() => setDialogo("cancelar")}>
            Cancelar esta fecha
          </Button>
        </div>
      ) : null}
      {puedeBorrar ? (
        <div className="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
          <p className="text-sm text-muted-foreground">No tiene reservas: puedes borrarla del todo.</p>
          <Button variant="outline" className="border-destructive/50 text-destructive" onClick={() => setDialogo("borrar")}>
            Borrar fecha
          </Button>
        </div>
      ) : null}

      <Dialog open={dialogo !== null} onOpenChange={(open) => !open && setDialogo(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{dialogo === "cancelar" ? "¿Cancelar esta fecha?" : "¿Borrar esta fecha?"}</DialogTitle>
            <DialogDescription asChild>
              <div className="grid gap-2">
                <span>
                  {edicion.curso} · {formatearFechaHora(edicion.inicio)}
                </span>
                {dialogo === "cancelar" ? (
                  <>
                    <span>
                      Tiene <strong className="text-foreground">{edicion.pagadas} pagadas</strong> y{" "}
                      <strong className="text-foreground">{edicion.pendientes} pendientes</strong>.
                    </span>
                    {edicion.pendientes > 0 ? <span>Las reservas pendientes se cancelarán.</span> : null}
                    {edicion.pagadas > 0 ? <span>Las pagadas tendrás que avisarlas y devolverlas a mano.</span> : null}
                    <span>Esto no se puede deshacer.</span>
                  </>
                ) : (
                  <span>Se borrará del todo. Esto no se puede deshacer.</span>
                )}
              </div>
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDialogo(null)} disabled={enviando}>
              No, volver
            </Button>
            <Button variant="destructive" onClick={() => void ejecutar()} disabled={enviando}>
              {enviando ? <LoaderCircleIcon className="animate-spin" /> : null}
              {dialogo === "cancelar" ? "Sí, cancelar la fecha" : "Sí, borrar"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </Card>
  );
}
