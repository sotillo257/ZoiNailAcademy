"use client";

import { LoaderCircleIcon } from "lucide-react";
import * as React from "react";
import { toast } from "sonner";
import { useAuth } from "@/components/admin/auth-provider";
import { Cabecera } from "@/components/admin/estados";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Campo, Input } from "@/components/ui/input";
import { isApiError, mensajeError, SessionExpiredError } from "@/lib/admin/errors";

const MINIMO = 10;

export default function CuentaPage() {
  const { user, session } = useAuth();
  const [actual, setActual] = React.useState("");
  const [nueva, setNueva] = React.useState("");
  const [repetida, setRepetida] = React.useState("");
  const [errores, setErrores] = React.useState<{ actual?: string; nueva?: string; repetida?: string }>({});
  const [enviando, setEnviando] = React.useState(false);

  const enviar = async (e: React.FormEvent) => {
    e.preventDefault();
    const nuevos: typeof errores = {};
    if (!actual) nuevos.actual = "Escribe tu contraseña actual.";
    if (nueva.length < MINIMO) nuevos.nueva = `La nueva contraseña debe tener al menos ${MINIMO} caracteres.`;
    if (nueva !== repetida) nuevos.repetida = "Las contraseñas no coinciden.";
    setErrores(nuevos);
    if (Object.keys(nuevos).length > 0) return;

    setEnviando(true);
    try {
      await session.json<void>("/api/auth/cambiar-password", {
        method: "POST",
        body: JSON.stringify({ passwordActual: actual, passwordNueva: nueva }),
      });
      toast.success("Contraseña cambiada. Se han cerrado las demás sesiones.");
      setActual("");
      setNueva("");
      setRepetida("");
    } catch (error) {
      if (error instanceof SessionExpiredError) return;
      if (isApiError(error, 400)) {
        setErrores({
          actual: error.fieldError("passwordActual"),
          nueva: error.fieldError("passwordNueva"),
        });
        if (!error.fieldError("passwordActual") && !error.fieldError("passwordNueva")) toast.error(mensajeError(error));
      } else {
        toast.error(mensajeError(error));
      }
    } finally {
      setEnviando(false);
    }
  };

  return (
    <>
      <Cabecera titulo="Cuenta" descripcion={user ? `${user.nombre} · ${user.email}` : undefined} />
      <Card className="max-w-md">
        <h2 className="mb-4 font-semibold">Cambiar contraseña</h2>
        <form onSubmit={enviar} noValidate className="grid gap-4">
          <input type="email" autoComplete="username" value={user?.email ?? ""} readOnly hidden />
          <Campo id="actual" label="Contraseña actual" error={errores.actual}>
            {(p) => <Input {...p} type="password" autoComplete="current-password" value={actual} onChange={(e) => setActual(e.target.value)} />}
          </Campo>
          <Campo id="nueva" label="Nueva contraseña" error={errores.nueva} ayuda={`Al menos ${MINIMO} caracteres.`}>
            {(p) => <Input {...p} type="password" autoComplete="new-password" value={nueva} onChange={(e) => setNueva(e.target.value)} />}
          </Campo>
          <Campo id="repetida" label="Repite la nueva contraseña" error={errores.repetida}>
            {(p) => <Input {...p} type="password" autoComplete="new-password" value={repetida} onChange={(e) => setRepetida(e.target.value)} />}
          </Campo>
          <Button type="submit" disabled={enviando}>
            {enviando ? <LoaderCircleIcon className="animate-spin" /> : null}
            Cambiar contraseña
          </Button>
        </form>
      </Card>
    </>
  );
}
