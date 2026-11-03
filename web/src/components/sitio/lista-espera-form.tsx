"use client";

import Link from "next/link";
import * as React from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { CheckCircle2Icon, Loader2Icon } from "lucide-react";
import { llamarApi } from "@/lib/sitio/api";
import { errorDeFormulario } from "@/lib/sitio/errores";
import { CONSENTIMIENTO_LISTA_ESPERA_V1, type ListaEsperaFormulario, listaEsperaEsquema } from "@/lib/sitio/esquemas";
import { Boton } from "./boton";
import { AvisoError, Campo, Casilla, Honeypot, claseControl } from "./campos";

export interface OpcionCurso {
  id: string;
  slug: string;
  nombre: string;
}

/** "Entra a la Lista de Espera" → POST /api/lista-espera (F05). */
export function ListaEsperaForm({
  cursos,
  cursoInicial,
  edicionPreferidaId,
  edicionPreferidaTexto,
}: {
  cursos: OpcionCurso[];
  cursoInicial?: string;
  edicionPreferidaId?: string;
  edicionPreferidaTexto?: string;
}) {
  const [enviado, setEnviado] = React.useState<string | null>(null);
  const [error, setError] = React.useState<string | null>(null);
  const {
    register,
    handleSubmit,
    setError: setErrorCampo,
    formState: { errors, isSubmitting },
  } = useForm<ListaEsperaFormulario>({
    resolver: zodResolver(listaEsperaEsquema),
    defaultValues: {
      cursoId: cursoInicial ?? cursos[0]?.id ?? "",
      nombre: "",
      telefono: "",
      email: "",
      fechaPreferida: "",
      website: "",
    },
  });

  const enviar = handleSubmit(async (datos) => {
    setError(null);
    try {
      await llamarApi("/api/lista-espera", {
        method: "POST",
        body: JSON.stringify({
          cursoId: datos.cursoId,
          edicionPreferidaId: datos.cursoId === cursoInicial ? (edicionPreferidaId ?? null) : null,
          fechaPreferida: datos.fechaPreferida || null,
          nombre: datos.nombre,
          email: datos.email,
          telefono: datos.telefono,
          aceptaPrivacidad: datos.aceptaPrivacidad,
          textoConsentimiento: CONSENTIMIENTO_LISTA_ESPERA_V1,
          website: datos.website || null,
        }),
      });
      setEnviado(cursos.find((c) => c.id === datos.cursoId)?.nombre ?? "");
    } catch (e) {
      const fallo = errorDeFormulario(e);
      for (const [campo, mensaje] of Object.entries(fallo.campos)) {
        if (campo in listaEsperaEsquema.shape) setErrorCampo(campo as keyof ListaEsperaFormulario, { message: mensaje });
      }
      setError(fallo.mensaje);
    }
  });

  if (enviado !== null) {
    return (
      <div role="status" className="flex flex-col items-center py-8 text-center">
        <CheckCircle2Icon className="size-10 text-z-green" aria-hidden />
        <p className="mt-4 text-lg font-bold">¡Ya estás en la lista!</p>
        <p className="mt-2 text-sm text-z-text">Te avisaremos cuando haya nuevas fechas{enviado ? ` de ${enviado}` : ""}.</p>
      </div>
    );
  }

  return (
    <form onSubmit={enviar} noValidate className="relative space-y-4">
      <Campo id="espera-nombre" label="Nombre y apellidos" error={errors.nombre?.message}>
        {(p) => <input {...p} {...register("nombre")} autoComplete="name" className={`${claseControl} h-12`} />}
      </Campo>
      <Campo id="espera-telefono" label="WhatsApp" error={errors.telefono?.message}>
        {(p) => <input {...p} {...register("telefono")} type="tel" autoComplete="tel" placeholder="612 345 678" className={`${claseControl} h-12`} />}
      </Campo>
      <Campo id="espera-email" label="Email" error={errors.email?.message}>
        {(p) => <input {...p} {...register("email")} type="email" autoComplete="email" className={`${claseControl} h-12`} />}
      </Campo>
      <Campo id="espera-curso" label="¿Qué formación te interesa?" error={errors.cursoId?.message}>
        {(p) => (
          <select {...p} {...register("cursoId")} className={`${claseControl} h-12 cursor-pointer`}>
            {cursos.map((c) => (
              <option key={c.id} value={c.id}>
                {c.nombre}
              </option>
            ))}
          </select>
        )}
      </Campo>
      {edicionPreferidaTexto ? <p className="-mt-2 text-xs text-z-text">Fecha que te interesaba: {edicionPreferidaTexto}</p> : null}
      <Campo id="espera-fecha" label="¿Qué fechas te vendrían bien? (opcional)" error={errors.fechaPreferida?.message}>
        {(p) => <input {...p} {...register("fechaPreferida")} placeholder="Ej.: fines de semana, a partir de marzo" className={`${claseControl} h-12`} />}
      </Campo>
      <Casilla id="espera-consentimiento" error={errors.aceptaPrivacidad?.message} {...register("aceptaPrivacidad")}>
        {/* Same words as CONSENTIMIENTO_LISTA_ESPERA_V1, with the policy linked. */}
        Acepto{" "}
        <Link href="/privacidad" target="_blank" className="underline">
          la política de privacidad
        </Link>{" "}
        y que me contactéis por teléfono, WhatsApp o email para avisarme cuando haya nuevas fechas de este curso.
      </Casilla>
      <Honeypot {...register("website")} />
      {error ? <AvisoError>{error}</AvisoError> : null}
      <Boton type="submit" ancho disabled={isSubmitting} aria-busy={isSubmitting}>
        {isSubmitting ? <Loader2Icon className="size-4 animate-spin" aria-hidden /> : null}
        Quiero entrar en la lista
      </Boton>
    </form>
  );
}
