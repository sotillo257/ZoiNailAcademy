"use client";

import { LoaderCircleIcon } from "lucide-react";
import { useRouter } from "next/navigation";
import * as React from "react";
import Markdown from "react-markdown";
import { toast } from "sonner";
import { useAuth } from "@/components/admin/auth-provider";
import { Button } from "@/components/ui/button";
import { Campo, Input, Label, Textarea } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { isApiError, mensajeError, SessionExpiredError } from "@/lib/admin/errors";
import type { CursoAdminDto, CursoCampos, CursoEditadoDto } from "@/lib/admin/types";

type Errores = Partial<Record<keyof CursoCampos, string>>;

const VACIO: CursoCampos = {
  nombre: "",
  slug: "",
  descripcionCorta: "",
  descripcion: "",
  precio: 0,
  imagen: "",
  ubicacion: null,
  duracionTexto: null,
  activo: true,
  orden: 0,
};

const SLUG = /^[a-z0-9]+(-[a-z0-9]+)*$/;

export function slugDe(texto: string): string {
  return texto
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}

/** Every field of a course (F02), with a preview of the Markdown description. */
export function CursoForm({ curso }: { curso?: CursoAdminDto }) {
  const { session } = useAuth();
  const router = useRouter();
  const [valores, setValores] = React.useState<CursoCampos>(curso ?? VACIO);
  const [precio, setPrecio] = React.useState(curso ? String(curso.precio) : "");
  const [slugTocado, setSlugTocado] = React.useState(Boolean(curso));
  const [errores, setErrores] = React.useState<Errores>({});
  const [enviando, setEnviando] = React.useState(false);

  const set = <K extends keyof CursoCampos>(campo: K, valor: CursoCampos[K]) =>
    setValores((v) => ({ ...v, [campo]: valor }));

  const enviar = async (e: React.FormEvent) => {
    e.preventDefault();
    if (enviando) return;
    const numPrecio = Number(precio.replace(",", "."));
    const nuevos: Errores = {};
    if (!valores.nombre.trim()) nuevos.nombre = "El nombre es obligatorio.";
    if (!SLUG.test(valores.slug)) nuevos.slug = "Solo minúsculas, números y guiones (p. ej. 'acrilico-avanzado').";
    if (!valores.descripcionCorta.trim()) nuevos.descripcionCorta = "La descripción corta es obligatoria.";
    if (!(numPrecio > 0)) nuevos.precio = "El precio debe ser mayor que cero.";
    if (!valores.imagen.trim()) nuevos.imagen = "La imagen es obligatoria.";
    if (!Number.isInteger(valores.orden) || valores.orden < 0) nuevos.orden = "El orden no puede ser negativo.";
    setErrores(nuevos);
    if (Object.keys(nuevos).length > 0) return;

    const body: CursoCampos = {
      ...valores,
      precio: numPrecio,
      nombre: valores.nombre.trim(),
      descripcionCorta: valores.descripcionCorta.trim(),
      imagen: valores.imagen.trim(),
      ubicacion: valores.ubicacion?.trim() || null,
      duracionTexto: valores.duracionTexto?.trim() || null,
    };

    setEnviando(true);
    try {
      if (curso) {
        const resultado = await session.json<CursoEditadoDto>(`/api/admin/cursos/${curso.id}`, {
          method: "PUT",
          body: JSON.stringify(body),
        });
        toast.success("Curso guardado");
        for (const aviso of resultado.avisos) toast.info(aviso, { duration: 10_000 });
      } else {
        await session.json<CursoAdminDto>("/api/admin/cursos", { method: "POST", body: JSON.stringify(body) });
        toast.success("Curso creado");
      }
      router.push("/admin/cursos");
    } catch (error) {
      setEnviando(false);
      if (error instanceof SessionExpiredError) return;
      if (isApiError(error, 400) && Object.keys(error.errors).length > 0) {
        const porCampo: Errores = {};
        for (const campo of Object.keys(VACIO) as (keyof CursoCampos)[]) {
          const mensaje = error.fieldError(campo);
          if (mensaje) porCampo[campo] = mensaje;
        }
        setErrores(porCampo);
        if (Object.keys(porCampo).length === 0) toast.error(mensajeError(error));
        return;
      }
      if (isApiError(error, 409) && /slug/i.test(error.detail ?? "")) {
        setErrores({ slug: mensajeError(error) });
        return;
      }
      toast.error(mensajeError(error));
    }
  };

  return (
    <form onSubmit={enviar} noValidate className="grid max-w-3xl gap-4">
      <div className="grid gap-4 md:grid-cols-2">
        <Campo id="nombre" label="Nombre" error={errores.nombre}>
          {(p) => (
            <Input
              {...p}
              value={valores.nombre}
              maxLength={120}
              onChange={(e) => {
                set("nombre", e.target.value);
                if (!slugTocado) set("slug", slugDe(e.target.value));
              }}
            />
          )}
        </Campo>
        <Campo id="slug" label="Slug (URL)" error={errores.slug} ayuda={`/cursos/${valores.slug || "…"}`}>
          {(p) => (
            <Input
              {...p}
              value={valores.slug}
              maxLength={120}
              autoCapitalize="off"
              onChange={(e) => {
                setSlugTocado(true);
                set("slug", e.target.value.toLowerCase());
              }}
            />
          )}
        </Campo>
      </div>

      <Campo id="descripcionCorta" label="Descripción corta" error={errores.descripcionCorta}>
        {(p) => (
          <Textarea {...p} maxLength={300} value={valores.descripcionCorta} onChange={(e) => set("descripcionCorta", e.target.value)} />
        )}
      </Campo>

      <div className="grid gap-1.5">
        <Label htmlFor="descripcion">Descripción (Markdown)</Label>
        <Tabs defaultValue="editar">
          <TabsList className="mb-2">
            <TabsTrigger value="editar">Editar</TabsTrigger>
            <TabsTrigger value="vista">Vista previa</TabsTrigger>
          </TabsList>
          <TabsContent value="editar">
            <Textarea
              id="descripcion"
              className="min-h-56 font-mono text-sm"
              value={valores.descripcion}
              aria-invalid={Boolean(errores.descripcion)}
              onChange={(e) => set("descripcion", e.target.value)}
            />
          </TabsContent>
          <TabsContent value="vista">
            <div className="prosa min-h-56 rounded-lg border bg-card p-4 text-sm">
              {valores.descripcion.trim() ? (
                <Markdown>{valores.descripcion}</Markdown>
              ) : (
                <p className="text-muted-foreground">Sin descripción.</p>
              )}
            </div>
          </TabsContent>
        </Tabs>
        {errores.descripcion ? <p className="text-sm text-destructive">{errores.descripcion}</p> : null}
      </div>

      <div className="grid gap-4 md:grid-cols-3">
        <Campo id="precio" label="Precio (€)" error={errores.precio}>
          {(p) => <Input {...p} inputMode="decimal" value={precio} onChange={(e) => setPrecio(e.target.value)} placeholder="149,00" />}
        </Campo>
        <Campo id="orden" label="Orden" error={errores.orden} ayuda="Menor sale antes.">
          {(p) => (
            <Input {...p} type="number" inputMode="numeric" min={0} value={valores.orden} onChange={(e) => set("orden", Number(e.target.value))} />
          )}
        </Campo>
        <div className="flex items-center gap-2 md:pt-6">
          <Switch id="activo" checked={valores.activo} onCheckedChange={(v) => set("activo", v)} />
          <Label htmlFor="activo">Activo (visible en la web)</Label>
        </div>
      </div>

      <Campo id="imagen" label="Imagen" error={errores.imagen} ayuda="Ruta de la imagen en la web, p. ej. /cursos/acrilico.jpg">
        {(p) => <Input {...p} maxLength={300} value={valores.imagen} onChange={(e) => set("imagen", e.target.value)} />}
      </Campo>

      <div className="grid gap-4 md:grid-cols-2">
        <Campo id="ubicacion" label="Ubicación (opcional)" error={errores.ubicacion}>
          {(p) => <Input {...p} maxLength={300} value={valores.ubicacion ?? ""} onChange={(e) => set("ubicacion", e.target.value)} />}
        </Campo>
        <Campo id="duracionTexto" label="Duración (opcional)" error={errores.duracionTexto} ayuda="p. ej. 2 días · 16 h">
          {(p) => <Input {...p} maxLength={100} value={valores.duracionTexto ?? ""} onChange={(e) => set("duracionTexto", e.target.value)} />}
        </Campo>
      </div>

      <div className="flex flex-col-reverse gap-2 md:flex-row">
        <Button variant="outline" onClick={() => router.back()} disabled={enviando}>
          Volver
        </Button>
        <Button type="submit" size="lg" className="md:h-10 md:text-sm" disabled={enviando}>
          {enviando ? <LoaderCircleIcon className="animate-spin" /> : null}
          {curso ? "Guardar curso" : "Crear curso"}
        </Button>
      </div>
    </form>
  );
}
