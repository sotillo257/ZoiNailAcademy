"use client";

import { LoaderCircleIcon } from "lucide-react";
import { useRouter } from "next/navigation";
import * as React from "react";
import { toast } from "sonner";
import { useAuth } from "@/components/admin/auth-provider";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Campo, Input, NativeSelect, Textarea } from "@/components/ui/input";
import { isApiError, mensajeError, SessionExpiredError } from "@/lib/admin/errors";
import { LIMITES, type CursoOpcionDto, type EdicionAdminDto, type EdicionEditadaDto } from "@/lib/admin/types";
import { HoraInexistenteError, isoAMadrid, madridAIso } from "@/lib/fechas";

type Campos = "cursoId" | "fecha" | "hora" | "plazas" | "estado" | "notas";
type Errores = Partial<Record<Campos, string>>;

/** Maps the API's validation fields (PascalCase) to the form's. */
const CAMPO_API: Record<string, Campos> = {
  cursoid: "cursoId",
  inicio: "fecha",
  plazas: "plazas",
  estado: "estado",
  notas: "notas",
};

export function EdicionForm({
  cursos,
  edicion,
  onConflicto,
}: {
  cursos: CursoOpcionDto[];
  /** Absent when creating. */
  edicion?: EdicionAdminDto;
  /** `concurrencia`: someone changed it meanwhile. */
  onConflicto?: () => void;
}) {
  const { session } = useAuth();
  const router = useRouter();
  const inicial = edicion ? isoAMadrid(edicion.inicio) : { fecha: "", hora: "10:00" };
  const [cursoId, setCursoId] = React.useState(edicion?.cursoId ?? (cursos.length === 1 ? cursos[0]?.id ?? "" : ""));
  const [fecha, setFecha] = React.useState(inicial.fecha);
  const [hora, setHora] = React.useState(inicial.hora);
  const [plazas, setPlazas] = React.useState(String(edicion?.plazas ?? 8));
  const [estado, setEstado] = React.useState(edicion?.estado ?? "Programada");
  const [notas, setNotas] = React.useState(edicion?.notas ?? "");
  const [errores, setErrores] = React.useState<Errores>({});
  const [enviando, setEnviando] = React.useState(false);
  const [conflicto, setConflicto] = React.useState(false);
  const [confirmarFecha, setConfirmarFecha] = React.useState<{ reservas: number; mensaje: string } | null>(null);

  const soloNotas = Boolean(edicion && (edicion.estado === "Cancelada" || new Date(edicion.inicio) <= new Date()));
  const opcionesCurso = cursos.filter((c) => c.activo || c.id === edicion?.cursoId);

  const enviar = async (confirmarCambioFecha = false) => {
    if (enviando) return;
    const nuevosErrores: Errores = {};
    if (!edicion && !cursoId) nuevosErrores.cursoId = "Elige un curso.";
    if (!fecha) nuevosErrores.fecha = "Indica la fecha.";
    if (!hora) nuevosErrores.hora = "Indica la hora.";
    const numPlazas = Number(plazas);
    if (!Number.isInteger(numPlazas) || numPlazas < LIMITES.plazasMinimas || numPlazas > LIMITES.plazasMaximas) {
      nuevosErrores.plazas = `Las plazas deben estar entre ${LIMITES.plazasMinimas} y ${LIMITES.plazasMaximas}.`;
    }

    let inicio = "";
    if (fecha && hora) {
      // Unchanged date and time: keep the stored instant (the repeated October hour has two of them).
      if (edicion && fecha === inicial.fecha && hora === inicial.hora) {
        inicio = edicion.inicio;
      } else {
        try {
          inicio = madridAIso({ fecha, hora });
        } catch (e) {
          nuevosErrores.hora = e instanceof HoraInexistenteError ? e.message : "Fecha u hora no válidas.";
        }
      }
    }
    setErrores(nuevosErrores);
    if (Object.keys(nuevosErrores).length > 0) return;

    setEnviando(true);
    setConflicto(false);
    try {
      if (edicion) {
        const resultado = await session.json<EdicionEditadaDto>(`/api/admin/ediciones/${edicion.id}`, {
          method: "PUT",
          body: JSON.stringify({ inicio, plazas: numPlazas, estado, notas: notas.trim() || null, version: edicion.version, confirmarCambioFecha }),
        });
        toast.success("Fecha guardada");
        for (const aviso of resultado.avisos) toast.info(aviso);
        router.push(`/admin/ediciones/${edicion.id}`);
      } else {
        const creada = await session.json<EdicionAdminDto>("/api/admin/ediciones", {
          method: "POST",
          body: JSON.stringify({ cursoId, inicio, plazas: numPlazas, estado, notas: notas.trim() || null }),
        });
        toast.success(estado === "Programada" ? "Fecha creada y publicada" : "Fecha creada (oculta)");
        router.push(`/admin/ediciones/${creada.id}`);
      }
    } catch (error) {
      setEnviando(false);
      if (error instanceof SessionExpiredError) return;
      if (isApiError(error, 409, "plazas_por_debajo_de_ocupadas")) {
        const ocupadas = error.numberExtension("ocupadas");
        setErrores({
          plazas: ocupadas !== undefined ? `Hay ${ocupadas} plazas ocupadas: el mínimo es ${ocupadas}.` : mensajeError(error),
        });
        return;
      }
      if (isApiError(error, 409, "cambio_fecha_requiere_confirmacion")) {
        setConfirmarFecha({ reservas: error.numberExtension("reservas") ?? 0, mensaje: mensajeError(error) });
        return;
      }
      if (isApiError(error, 409, "concurrencia")) {
        setConflicto(true);
        return;
      }
      if (isApiError(error, 400) && Object.keys(error.errors).length > 0) {
        const porCampo: Errores = {};
        for (const [campo, mensajes] of Object.entries(error.errors)) {
          const destino = CAMPO_API[campo.toLowerCase()];
          if (destino && mensajes[0]) porCampo[destino] = mensajes[0];
        }
        setErrores(porCampo);
        if (Object.keys(porCampo).length === 0) toast.error(mensajeError(error));
        return;
      }
      toast.error(mensajeError(error));
    }
  };

  return (
    <form
      noValidate
      onSubmit={(e) => {
        e.preventDefault();
        void enviar();
      }}
      className="grid max-w-xl gap-4"
    >
      {conflicto ? (
        <div role="alert" className="flex flex-col gap-2 rounded-lg bg-warning-soft p-3 text-sm text-warning md:flex-row md:items-center md:justify-between">
          <span>Alguien ha modificado esta fecha. Recarga para ver los cambios.</span>
          <Button variant="outline" size="sm" onClick={onConflicto}>
            Recargar
          </Button>
        </div>
      ) : null}

      <Campo id="cursoId" label="Curso" error={errores.cursoId}>
        {(props) => (
          <NativeSelect {...props} value={cursoId} onChange={(e) => setCursoId(e.target.value)} disabled={Boolean(edicion)}>
            <option value="">Elige un curso…</option>
            {opcionesCurso.map((c) => (
              <option key={c.id} value={c.id}>
                {c.nombre}
              </option>
            ))}
          </NativeSelect>
        )}
      </Campo>

      <div className="grid grid-cols-2 gap-3">
        <Campo id="fecha" label="Fecha" error={errores.fecha}>
          {(props) => <Input {...props} type="date" value={fecha} onChange={(e) => setFecha(e.target.value)} disabled={soloNotas} />}
        </Campo>
        <Campo id="hora" label="Hora (Madrid)" error={errores.hora}>
          {(props) => <Input {...props} type="time" step={300} value={hora} onChange={(e) => setHora(e.target.value)} disabled={soloNotas} />}
        </Campo>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <Campo id="plazas" label="Plazas" error={errores.plazas}>
          {(props) => (
            <Input
              {...props}
              type="number"
              inputMode="numeric"
              min={LIMITES.plazasMinimas}
              max={LIMITES.plazasMaximas}
              value={plazas}
              onChange={(e) => setPlazas(e.target.value)}
              disabled={soloNotas}
            />
          )}
        </Campo>
        <Campo id="estado" label="Estado" error={errores.estado}>
          {(props) => (
            <NativeSelect
              {...props}
              value={estado}
              onChange={(e) => setEstado(e.target.value as typeof estado)}
              disabled={soloNotas}
            >
              <option value="Programada">Programada (publicada)</option>
              <option value="Oculta">Oculta</option>
              {estado === "Cancelada" ? <option value="Cancelada">Cancelada</option> : null}
            </NativeSelect>
          )}
        </Campo>
      </div>

      <Campo id="notas" label="Notas internas" error={errores.notas} ayuda="Solo se ven en el backoffice.">
        {(props) => <Textarea {...props} maxLength={LIMITES.notasEdicion} value={notas} onChange={(e) => setNotas(e.target.value)} />}
      </Campo>

      {soloNotas ? (
        <p className="text-sm text-muted-foreground">
          {edicion?.estado === "Cancelada" ? "La fecha está cancelada" : "La fecha ya ha empezado"}: solo se pueden editar las notas.
        </p>
      ) : null}

      <div className="flex flex-col-reverse gap-2 md:flex-row">
        <Button variant="outline" onClick={() => router.back()} disabled={enviando}>
          Volver
        </Button>
        <Button type="submit" size="lg" className="md:h-10 md:text-sm" disabled={enviando}>
          {enviando ? <LoaderCircleIcon className="animate-spin" /> : null}
          {edicion ? "Guardar cambios" : "Crear fecha"}
        </Button>
      </div>

      <Dialog open={confirmarFecha !== null} onOpenChange={(open) => !open && setConfirmarFecha(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>¿Cambiar la fecha igualmente?</DialogTitle>
            <DialogDescription>
              Esta edición tiene {confirmarFecha?.reservas} inscritos, tendrás que avisarles.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setConfirmarFecha(null)}>
              No, volver
            </Button>
            <Button
              onClick={() => {
                setConfirmarFecha(null);
                void enviar(true);
              }}
            >
              Sí, cambiar la fecha
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </form>
  );
}
