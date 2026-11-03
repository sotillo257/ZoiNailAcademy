"use client";

import { EdicionForm } from "@/components/admin/ediciones/edicion-form";
import { Cabecera, ErrorCarga, TarjetasSkeleton } from "@/components/admin/estados";
import { useApi } from "@/components/admin/use-api";
import type { CursoOpcionDto } from "@/lib/admin/types";

export default function NuevaEdicionPage() {
  const { data, error, recargar } = useApi<CursoOpcionDto[]>("/api/admin/cursos");
  return (
    <>
      <Cabecera titulo="Nueva fecha" descripcion="Fecha y hora en hora de Madrid." />
      {error ? <ErrorCarga error={error} onReintentar={() => void recargar()} /> : null}
      {data ? <EdicionForm cursos={data} /> : !error ? <TarjetasSkeleton filas={1} /> : null}
    </>
  );
}
