"use client";

import { useSearchParams } from "next/navigation";
import * as React from "react";
import { CheckCircle2Icon, Loader2Icon } from "lucide-react";
import { type CursoDetalleDto, llamarApi } from "@/lib/sitio/api";
import { Boton, BotonLink } from "./boton";
import { AvisoError } from "./campos";

/**
 * Unsubscribe from new-dates notices (F11). Never on a plain GET: the person confirms with the button, which
 * calls POST /api/lista-espera/baja (always 204).
 */
export function Baja() {
  const params = useSearchParams();
  const token = params.get("token") ?? "";
  const slug = params.get("curso") ?? "";
  const [curso, setCurso] = React.useState<string | null>(null);
  const [estado, setEstado] = React.useState<"inicio" | "enviando" | "hecho" | "error">("inicio");

  React.useEffect(() => {
    if (!slug) return;
    llamarApi<CursoDetalleDto>(`/api/cursos/${encodeURIComponent(slug)}`)
      .then((c) => setCurso(c.nombre))
      .catch(() => setCurso(null));
  }, [slug]);

  const darDeBaja = async () => {
    setEstado("enviando");
    try {
      await llamarApi("/api/lista-espera/baja", { method: "POST", body: JSON.stringify({ token }) });
      setEstado("hecho");
    } catch {
      setEstado("error");
    }
  };

  if (!token) {
    return (
      <div className="text-center">
        <h1 className="text-2xl font-bold">Enlace no válido</h1>
        <p className="mt-3 text-sm text-z-text">Usa el enlace del último email que te enviamos o escríbenos y te damos de baja.</p>
        <BotonLink href="/contacto" className="mt-6">
          Escríbenos
        </BotonLink>
      </div>
    );
  }

  if (estado === "hecho") {
    return (
      <div role="status" className="text-center">
        <CheckCircle2Icon className="mx-auto size-10 text-z-green" aria-hidden />
        <h1 className="mt-4 text-2xl font-bold">Listo</h1>
        <p className="mt-3 text-sm text-z-text">Ya no te avisaremos de nuevas fechas{curso ? ` de ${curso}` : ""}.</p>
        <BotonLink href="/formaciones" variante="oscuro" className="mt-6">
          Ver formaciones
        </BotonLink>
      </div>
    );
  }

  return (
    <div className="text-center">
      <h1 className="text-2xl font-bold">¿Dejar de recibir avisos?</h1>
      <p className="mt-3 text-sm text-z-text">
        No te escribiremos más cuando haya nuevas fechas{curso ? ` de ${curso}` : ""}. Puedes volver a apuntarte cuando quieras.
      </p>
      {estado === "error" ? <AvisoError className="mt-5 text-left">No hemos podido darte de baja. Inténtalo de nuevo en un momento.</AvisoError> : null}
      <Boton onClick={darDeBaja} disabled={estado === "enviando"} className="mt-6">
        {estado === "enviando" ? <Loader2Icon className="size-4 animate-spin" aria-hidden /> : null}
        Darme de baja de los avisos
      </Boton>
    </div>
  );
}
