"use client";

import * as React from "react";
import { useAuth } from "@/components/admin/auth-provider";
import { Cabecera, ErrorCarga } from "@/components/admin/estados";
import { SoloSuperAdmin } from "@/components/admin/solo-superadmin";
import { Card, Skeleton } from "@/components/ui/card";
import { Label, NativeSelect } from "@/components/ui/input";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { ApiError, SessionExpiredError } from "@/lib/admin/errors";
import { TIPOS_EMAIL } from "@/lib/admin/types";
import { cn } from "@/lib/utils";

export default function EmailsPage() {
  return (
    <SoloSuperAdmin>
      <Emails />
    </SoloSuperAdmin>
  );
}

function Emails() {
  const { session } = useAuth();
  const [tipo, setTipo] = React.useState<string>(TIPOS_EMAIL[0].tipo);
  const [formato, setFormato] = React.useState<"html" | "texto">("html");
  const [contenido, setContenido] = React.useState<string | null>(null);
  const [error, setError] = React.useState<unknown>(null);
  const [intento, setIntento] = React.useState(0);

  React.useEffect(() => {
    let vigente = true;
    setContenido(null);
    setError(null);
    // The preview needs the Bearer token, so it is fetched and shown with srcdoc (an iframe src would not carry it).
    session
      .fetch(`/api/admin/emails/preview/${encodeURIComponent(tipo)}${formato === "texto" ? "?formato=texto" : ""}`, {
        headers: { Accept: formato === "texto" ? "text/plain" : "text/html" },
      })
      .then(async (res) => {
        if (!res.ok) throw new ApiError(res.status, await res.json().catch(() => null));
        const texto = await res.text();
        if (vigente) setContenido(texto);
      })
      .catch((e: unknown) => {
        if (vigente && !(e instanceof SessionExpiredError)) setError(e);
      });
    return () => {
      vigente = false;
    };
  }, [session, tipo, formato, intento]);

  return (
    <>
      <Cabecera titulo="Emails" descripcion="Vista previa de cada email con datos de ejemplo, para revisarlos con la clienta." />
      <div className="grid gap-4 md:grid-cols-[16rem_1fr]">
        <nav aria-label="Tipos de email" className="hidden md:grid md:content-start md:gap-1">
          {TIPOS_EMAIL.map((t) => (
            <button
              key={t.tipo}
              type="button"
              onClick={() => setTipo(t.tipo)}
              aria-current={tipo === t.tipo ? "true" : undefined}
              className={cn(
                "rounded-lg px-3 py-2 text-left text-sm",
                tipo === t.tipo ? "bg-secondary font-medium text-secondary-foreground" : "text-muted-foreground hover:bg-muted",
              )}
            >
              {t.nombre}
            </button>
          ))}
        </nav>
        <div className="grid min-w-0 gap-3">
          <div className="md:hidden">
            <Label htmlFor="tipo-email" className="sr-only">
              Tipo de email
            </Label>
            <NativeSelect id="tipo-email" value={tipo} onChange={(e) => setTipo(e.target.value)}>
              {TIPOS_EMAIL.map((t) => (
                <option key={t.tipo} value={t.tipo}>
                  {t.nombre}
                </option>
              ))}
            </NativeSelect>
          </div>
          <Tabs value={formato} onValueChange={(v) => setFormato(v as "html" | "texto")}>
            <TabsList>
              <TabsTrigger value="html">HTML</TabsTrigger>
              <TabsTrigger value="texto">Texto</TabsTrigger>
            </TabsList>
          </Tabs>
          {error ? <ErrorCarga error={error} onReintentar={() => setIntento((n) => n + 1)} /> : null}
          {!error && contenido === null ? <Skeleton className="h-[70dvh]" /> : null}
          {contenido !== null && formato === "html" ? (
            // sandbox without allow-scripts: the email cannot run code nor reach this page.
            <iframe title="Vista previa del email" sandbox="" srcDoc={contenido} className="h-[70dvh] w-full rounded-xl border bg-white" />
          ) : null}
          {contenido !== null && formato === "texto" ? (
            <Card className="overflow-x-auto">
              <pre className="whitespace-pre-wrap break-words font-mono text-sm">{contenido}</pre>
            </Card>
          ) : null}
        </div>
      </div>
    </>
  );
}
