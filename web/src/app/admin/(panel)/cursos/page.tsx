"use client";

import { PencilIcon, PlusIcon } from "lucide-react";
import Link from "next/link";
import { Cabecera, ErrorCarga, TarjetasSkeleton, Vacio } from "@/components/admin/estados";
import { SoloSuperAdmin } from "@/components/admin/solo-superadmin";
import { useApi } from "@/components/admin/use-api";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import type { CursoOpcionDto } from "@/lib/admin/types";

export default function CursosPage() {
  return (
    <SoloSuperAdmin>
      <Cursos />
    </SoloSuperAdmin>
  );
}

function Cursos() {
  const { data, error, cargando, recargar } = useApi<CursoOpcionDto[]>("/api/admin/cursos");
  return (
    <>
      <Cabecera
        titulo="Cursos"
        acciones={
          <Button asChild>
            <Link href="/admin/cursos/nuevo">
              <PlusIcon /> Nuevo curso
            </Link>
          </Button>
        }
      />
      {error ? <ErrorCarga error={error} onReintentar={() => void recargar()} /> : null}
      {!data && cargando ? <TarjetasSkeleton /> : null}
      {data && data.length === 0 ? <Vacio>No hay cursos.</Vacio> : null}
      {data && data.length > 0 ? (
        <Card className="divide-y p-0">
          {data.map((c) => (
            <div key={c.id} className="flex items-center justify-between gap-3 px-4 py-3">
              <div className="min-w-0">
                <p className="truncate font-medium">{c.nombre}</p>
                <p className="truncate font-mono text-xs text-muted-foreground">/cursos/{c.slug}</p>
              </div>
              <div className="flex shrink-0 items-center gap-2">
                {c.activo ? <Badge variant="success">Activo</Badge> : <Badge variant="muted">Inactivo</Badge>}
                <Button asChild variant="outline" size="sm">
                  <Link href={`/admin/cursos/${c.id}/editar`} aria-label={`Editar ${c.nombre}`}>
                    <PencilIcon /> <span className="hidden md:inline">Editar</span>
                  </Link>
                </Button>
              </div>
            </div>
          ))}
        </Card>
      ) : null}
    </>
  );
}
