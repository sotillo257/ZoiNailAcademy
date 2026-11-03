"use client";

import { ChevronRightIcon, PlusIcon } from "lucide-react";
import Link from "next/link";
import * as React from "react";
import { EdicionBadges } from "@/components/admin/ediciones/edicion-badge";
import { Cabecera, ErrorCarga, TarjetasSkeleton, Vacio } from "@/components/admin/estados";
import { BarraOcupacion } from "@/components/admin/ocupacion";
import { useAhora, useApi } from "@/components/admin/use-api";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Label, NativeSelect } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import type { CursoOpcionDto, EdicionAdminDto } from "@/lib/admin/types";
import { formatearFechaHora } from "@/lib/fechas";

export default function EdicionesPage() {
  const ahora = useAhora();
  const [cursoId, setCursoId] = React.useState("");
  const [pasadas, setPasadas] = React.useState(false);
  const cursos = useApi<CursoOpcionDto[]>("/api/admin/cursos");
  const query = new URLSearchParams();
  if (cursoId) query.set("cursoId", cursoId);
  if (pasadas) query.set("incluirPasadas", "true");
  const qs = query.toString();
  const { data, error, cargando, recargar } = useApi<EdicionAdminDto[]>(`/api/admin/ediciones${qs ? `?${qs}` : ""}`);

  return (
    <>
      <Cabecera
        titulo="Fechas"
        acciones={
          <Button asChild>
            <Link href="/admin/ediciones/nueva">
              <PlusIcon /> Nueva fecha
            </Link>
          </Button>
        }
      />

      <div className="mb-4 flex flex-col gap-3 md:flex-row md:items-center">
        <div className="md:w-72">
          <Label htmlFor="filtro-curso" className="sr-only">
            Curso
          </Label>
          <NativeSelect id="filtro-curso" value={cursoId} onChange={(e) => setCursoId(e.target.value)}>
            <option value="">Todos los cursos</option>
            {cursos.data?.map((c) => (
              <option key={c.id} value={c.id}>
                {c.nombre}
                {c.activo ? "" : " (inactivo)"}
              </option>
            ))}
          </NativeSelect>
        </div>
        <div className="flex items-center gap-2">
          <Switch id="mostrar-pasadas" checked={pasadas} onCheckedChange={setPasadas} />
          <Label htmlFor="mostrar-pasadas">Mostrar pasadas</Label>
        </div>
      </div>

      {error ? <ErrorCarga error={error} onReintentar={() => void recargar()} /> : null}
      {!data && cargando ? <TarjetasSkeleton /> : null}
      {data && data.length === 0 ? (
        <Vacio>
          No hay fechas{pasadas ? "" : " próximas"}.{" "}
          <Link href="/admin/ediciones/nueva" className="font-medium text-primary underline">
            Crear una
          </Link>
        </Vacio>
      ) : null}

      {data && data.length > 0 ? (
        <>
          {/* Phone: cards */}
          <div className="grid gap-3 md:hidden">
            {data.map((e) => (
              <Link key={e.id} href={`/admin/ediciones/${e.id}`} data-testid="edicion-item">
                <Card className="grid gap-3">
                  <div className="flex items-start justify-between gap-2">
                    <div className="min-w-0">
                      <p className="truncate font-medium">{e.curso}</p>
                      <p className="text-sm text-muted-foreground">{formatearFechaHora(e.inicio)}</p>
                    </div>
                    <ChevronRightIcon className="size-5 shrink-0 text-muted-foreground" aria-hidden />
                  </div>
                  <EdicionBadges edicion={e} ahora={ahora} />
                  <BarraOcupacion plazas={e.plazas} pagadas={e.pagadas} pendientes={e.pendientes} />
                </Card>
              </Link>
            ))}
          </div>

          {/* Desktop: table */}
          <Card className="hidden overflow-hidden p-0 md:block">
            <table className="w-full text-sm">
              <thead className="bg-muted/60 text-left text-xs uppercase tracking-wide text-muted-foreground">
                <tr>
                  <th className="px-4 py-3 font-medium">Curso</th>
                  <th className="px-4 py-3 font-medium">Fecha y hora</th>
                  <th className="w-64 px-4 py-3 font-medium">Ocupación</th>
                  <th className="px-4 py-3 font-medium">Estado</th>
                  <th className="px-4 py-3 font-medium">
                    <span className="sr-only">Acciones</span>
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {data.map((e) => (
                  <tr key={e.id} data-testid="edicion-item" className="hover:bg-muted/40">
                    <td className="px-4 py-3 font-medium">{e.curso}</td>
                    <td className="px-4 py-3">{formatearFechaHora(e.inicio)}</td>
                    <td className="px-4 py-3">
                      <BarraOcupacion plazas={e.plazas} pagadas={e.pagadas} pendientes={e.pendientes} />
                    </td>
                    <td className="px-4 py-3">
                      <EdicionBadges edicion={e} ahora={ahora} />
                    </td>
                    <td className="px-4 py-3 text-right">
                      <div className="flex justify-end gap-1">
                        <Button asChild variant="ghost" size="sm">
                          <Link href={`/admin/ediciones/${e.id}/editar`}>Editar</Link>
                        </Button>
                        <Button asChild variant="outline" size="sm">
                          <Link href={`/admin/ediciones/${e.id}`}>Ver</Link>
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </Card>
        </>
      ) : null}
    </>
  );
}
