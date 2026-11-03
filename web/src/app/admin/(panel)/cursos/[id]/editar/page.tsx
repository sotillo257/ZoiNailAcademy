"use client";

import { LoaderCircleIcon } from "lucide-react";
import { useParams, useRouter } from "next/navigation";
import * as React from "react";
import { toast } from "sonner";
import { useAuth } from "@/components/admin/auth-provider";
import { CursoForm } from "@/components/admin/cursos/curso-form";
import { Cabecera, ErrorCarga, TarjetasSkeleton } from "@/components/admin/estados";
import { useApi } from "@/components/admin/use-api";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { mensajeError, SessionExpiredError } from "@/lib/admin/errors";
import type { CursoAdminDto } from "@/lib/admin/types";

// No front-side role check here: an Admin gets the API's 403, shown as "Sin permiso".
export default function EditarCursoPage() {
  const { id } = useParams<{ id: string }>();
  const { data, error, recargar } = useApi<CursoAdminDto>(`/api/admin/cursos/${id}`);
  if (error) return <ErrorCarga error={error} onReintentar={() => void recargar()} />;
  if (!data) return <TarjetasSkeleton filas={2} />;
  return (
    <>
      <Cabecera titulo={`Editar ${data.nombre}`} />
      <CursoForm key={JSON.stringify(data)} curso={data} />
      {data.activo ? <Desactivar curso={data} /> : null}
    </>
  );
}

function Desactivar({ curso }: { curso: CursoAdminDto }) {
  const { session } = useAuth();
  const router = useRouter();
  const [abierto, setAbierto] = React.useState(false);
  const [enviando, setEnviando] = React.useState(false);

  const desactivar = async () => {
    setEnviando(true);
    try {
      await session.json<void>(`/api/admin/cursos/${curso.id}`, { method: "DELETE" });
      toast.success("Curso desactivado");
      router.push("/admin/cursos");
    } catch (e) {
      if (!(e instanceof SessionExpiredError)) toast.error(mensajeError(e));
      setEnviando(false);
      setAbierto(false);
    }
  };

  return (
    <Card className="mt-10 flex max-w-3xl flex-col gap-3 border-destructive/40 md:flex-row md:items-center md:justify-between">
      <div>
        <h2 className="font-semibold text-destructive">Desactivar curso</h2>
        <p className="text-sm text-muted-foreground">Deja de verse en la web. No se borra: puedes volver a activarlo.</p>
      </div>
      <Button variant="destructive" onClick={() => setAbierto(true)}>
        Desactivar
      </Button>
      <Dialog open={abierto} onOpenChange={setAbierto}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>¿Desactivar {curso.nombre}?</DialogTitle>
            <DialogDescription>Dejará de verse en la web. Si tiene fechas futuras, la API no lo permitirá.</DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setAbierto(false)} disabled={enviando}>
              No, volver
            </Button>
            <Button variant="destructive" onClick={() => void desactivar()} disabled={enviando}>
              {enviando ? <LoaderCircleIcon className="animate-spin" /> : null}
              Sí, desactivar
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </Card>
  );
}
