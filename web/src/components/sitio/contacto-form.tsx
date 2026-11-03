"use client";

import Link from "next/link";
import * as React from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { CheckCircle2Icon, Loader2Icon } from "lucide-react";
import { llamarApi } from "@/lib/sitio/api";
import { SITIO } from "@/lib/sitio/datos";
import { errorDeFormulario } from "@/lib/sitio/errores";
import { ASUNTOS_CONTACTO, type ContactoFormulario, contactoEsquema, mensajeConAsunto } from "@/lib/sitio/esquemas";
import { Boton } from "./boton";
import { AvisoError, Campo, Casilla, Honeypot, claseControl } from "./campos";

/** "Envíanos un Mensaje" → POST /api/contacto (F06). */
export function ContactoForm() {
  const [enviado, setEnviado] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const {
    register,
    handleSubmit,
    setError: setErrorCampo,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ContactoFormulario>({
    resolver: zodResolver(contactoEsquema),
    defaultValues: { nombre: "", telefono: "", email: "", asunto: ASUNTOS_CONTACTO[0], mensaje: "", website: "" },
  });

  const enviar = handleSubmit(async (datos) => {
    setError(null);
    try {
      await llamarApi("/api/contacto", {
        method: "POST",
        body: JSON.stringify({
          nombre: datos.nombre,
          email: datos.email,
          telefono: datos.telefono || null,
          mensaje: mensajeConAsunto(datos.asunto, datos.mensaje),
          aceptaPrivacidad: datos.aceptaPrivacidad,
          website: datos.website || null,
        }),
      });
      reset();
      setEnviado(true);
    } catch (e) {
      const fallo = errorDeFormulario(e);
      for (const [campo, mensaje] of Object.entries(fallo.campos)) {
        if (campo in contactoEsquema.shape) setErrorCampo(campo as keyof ContactoFormulario, { message: mensaje });
      }
      setError(fallo.mensaje);
    }
  });

  if (enviado) {
    return (
      <div role="status" className="flex flex-col items-center py-10 text-center">
        <CheckCircle2Icon className="size-10 text-z-green" aria-hidden />
        <p className="mt-4 text-lg font-bold">¡Mensaje enviado!</p>
        <p className="mt-2 text-sm text-z-text">Te contestamos por WhatsApp o email lo antes posible.</p>
        <button type="button" className="mt-5 text-xs font-bold uppercase tracking-[0.06em] text-z-burgundy underline" onClick={() => setEnviado(false)}>
          Enviar otro mensaje
        </button>
      </div>
    );
  }

  return (
    <form onSubmit={enviar} noValidate className="relative mt-5 space-y-4">
      <div className="grid gap-4 md:grid-cols-2 md:gap-3">
        <Campo id="contacto-nombre" label="Nombre" error={errors.nombre?.message}>
          {(p) => <input {...p} {...register("nombre")} autoComplete="name" className={`${claseControl} h-12`} />}
        </Campo>
        <Campo id="contacto-telefono" label="WhatsApp" error={errors.telefono?.message}>
          {(p) => <input {...p} {...register("telefono")} type="tel" autoComplete="tel" placeholder="612 345 678" className={`${claseControl} h-12`} />}
        </Campo>
      </div>
      <Campo id="contacto-email" label="Email" error={errors.email?.message}>
        {(p) => <input {...p} {...register("email")} type="email" autoComplete="email" className={`${claseControl} h-12`} />}
      </Campo>
      <Campo id="contacto-asunto" label="¿Sobre qué nos escribes?">
        {(p) => (
          <select {...p} {...register("asunto")} className={`${claseControl} h-12 cursor-pointer`}>
            {ASUNTOS_CONTACTO.map((a) => (
              <option key={a}>{a}</option>
            ))}
          </select>
        )}
      </Campo>
      <Campo id="contacto-mensaje" label="Mensaje" error={errors.mensaje?.message}>
        {(p) => <textarea {...p} {...register("mensaje")} rows={5} className={`${claseControl} min-h-[120px] py-3`} />}
      </Campo>
      <Casilla id="contacto-privacidad" error={errors.aceptaPrivacidad?.message} {...register("aceptaPrivacidad")}>
        Acepto la{" "}
        <Link href="/privacidad" className="underline" target="_blank">
          política de privacidad
        </Link>
      </Casilla>
      <Honeypot {...register("website")} />
      {error ? <AvisoError>{error}</AvisoError> : null}
      <Boton type="submit" ancho disabled={isSubmitting} aria-busy={isSubmitting}>
        {isSubmitting ? <Loader2Icon className="size-4 animate-spin" aria-hidden /> : null}
        Enviar mensaje
      </Boton>
    </form>
  );
}
