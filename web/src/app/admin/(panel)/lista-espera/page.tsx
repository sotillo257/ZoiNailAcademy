"use client";

import { BellOffIcon, DownloadIcon, LoaderCircleIcon, MailIcon, MessageCircleIcon, PhoneIcon, SearchIcon } from "lucide-react";
import * as React from "react";
import { toast } from "sonner";
import { useAuth } from "@/components/admin/auth-provider";
import { Cabecera, ErrorCarga, TarjetasSkeleton, Vacio } from "@/components/admin/estados";
import { useResumen } from "@/components/admin/resumen-provider";
import { useAhora, useApi, useDebounced } from "@/components/admin/use-api";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input, Label, NativeSelect, Textarea } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import { descargarArchivo } from "@/lib/admin/descargar";
import { mensajeError, SessionExpiredError } from "@/lib/admin/errors";
import { LIMITES, type CursoOpcionDto, type ListaEsperaAdminDto, type PagedResult } from "@/lib/admin/types";
import { antiguedad, formatearFecha } from "@/lib/fechas";
import { telUrl, whatsappUrl } from "@/lib/formato";

const PAGE_SIZE = 50;

export default function ListaEsperaPage() {
  const { session } = useAuth();
  const ahora = useAhora();
  const { recargar: recargarResumen } = useResumen();
  const cursos = useApi<CursoOpcionDto[]>("/api/admin/cursos");
  const [cursoId, setCursoId] = React.useState("");
  const [soloSinContactar, setSoloSinContactar] = React.useState(true);
  const [busqueda, setBusqueda] = React.useState("");
  const q = useDebounced(busqueda.trim(), 350);
  const [paginas, setPaginas] = React.useState(1);
  const [extra, setExtra] = React.useState<ListaEsperaAdminDto[]>([]);
  const [cargandoMas, setCargandoMas] = React.useState(false);
  const [descargando, setDescargando] = React.useState(false);

  const filtros = React.useMemo(() => {
    const p = new URLSearchParams({ pageSize: String(PAGE_SIZE) });
    if (cursoId) p.set("cursoId", cursoId);
    if (soloSinContactar) p.set("contactado", "false");
    if (q) p.set("q", q);
    return p;
  }, [cursoId, soloSinContactar, q]);

  const { data, error, cargando, recargar, setData } = useApi<PagedResult<ListaEsperaAdminDto>>(
    `/api/admin/lista-espera?${filtros.toString()}`,
  );

  // New filters start from the first page.
  React.useEffect(() => {
    setPaginas(1);
    setExtra([]);
  }, [filtros]);

  const items = React.useMemo(() => [...(data?.items ?? []), ...extra], [data, extra]);
  const hayMas = data ? items.length < data.total : false;

  const cargarMas = async () => {
    setCargandoMas(true);
    try {
      const p = new URLSearchParams(filtros);
      p.set("page", String(paginas + 1));
      const siguiente = await session.json<PagedResult<ListaEsperaAdminDto>>(`/api/admin/lista-espera?${p.toString()}`);
      setExtra((actual) => [...actual, ...siguiente.items]);
      setPaginas((n) => n + 1);
    } catch (e) {
      if (!(e instanceof SessionExpiredError)) toast.error(mensajeError(e));
    } finally {
      setCargandoMas(false);
    }
  };

  const reemplazar = (fila: ListaEsperaAdminDto) => {
    setData((actual) => (actual ? { ...actual, items: actual.items.map((i) => (i.id === fila.id ? fila : i)) } : actual));
    setExtra((actual) => actual.map((i) => (i.id === fila.id ? fila : i)));
  };

  const descargar = async () => {
    setDescargando(true);
    try {
      await descargarArchivo(
        session,
        `/api/admin/lista-espera.csv${cursoId ? `?cursoId=${cursoId}` : ""}`,
        "lista-espera.csv",
      );
    } catch (e) {
      if (!(e instanceof SessionExpiredError)) toast.error(mensajeError(e));
    } finally {
      setDescargando(false);
    }
  };

  return (
    <>
      <Cabecera
        titulo="Lista de espera"
        descripcion={data ? `${data.total} ${data.total === 1 ? "persona" : "personas"}` : undefined}
        acciones={
          <Button variant="outline" onClick={() => void descargar()} disabled={descargando}>
            {descargando ? <LoaderCircleIcon className="animate-spin" /> : <DownloadIcon />}
            {cursoId ? "CSV del curso" : "CSV (todos los cursos)"}
          </Button>
        }
      />

      <div className="mb-4 grid gap-3 md:grid-cols-[16rem_1fr_auto] md:items-center">
        <div>
          <Label htmlFor="filtro-curso" className="sr-only">
            Curso
          </Label>
          <NativeSelect id="filtro-curso" value={cursoId} onChange={(e) => setCursoId(e.target.value)}>
            <option value="">Todos los cursos</option>
            {cursos.data?.map((c) => (
              <option key={c.id} value={c.id}>
                {c.nombre}
              </option>
            ))}
          </NativeSelect>
        </div>
        <div className="relative">
          <Label htmlFor="buscar-lista" className="sr-only">
            Buscar por nombre, email o teléfono
          </Label>
          <SearchIcon className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            id="buscar-lista"
            type="search"
            className="pl-9"
            placeholder="Buscar por nombre, email o teléfono"
            value={busqueda}
            onChange={(e) => setBusqueda(e.target.value)}
          />
        </div>
        <div className="flex items-center gap-2">
          <Switch id="solo-sin-contactar" checked={soloSinContactar} onCheckedChange={setSoloSinContactar} />
          <Label htmlFor="solo-sin-contactar">Solo sin contactar</Label>
        </div>
      </div>

      {error ? <ErrorCarga error={error} onReintentar={() => void recargar()} /> : null}
      {!data && cargando ? <TarjetasSkeleton /> : null}
      {data && items.length === 0 ? (
        <Vacio>{soloSinContactar ? "No queda nadie por contactar 🎉" : "No hay nadie en la lista de espera."}</Vacio>
      ) : null}

      {items.length > 0 ? (
        <div className="grid gap-3 md:grid-cols-2">
          {items.map((item) => (
            <TarjetaInteresado
              key={item.id}
              item={item}
              ahora={ahora}
              onCambio={(fila) => {
                reemplazar(fila);
                void recargarResumen();
              }}
            />
          ))}
        </div>
      ) : null}

      {hayMas ? (
        <div className="mt-4 flex justify-center">
          <Button variant="outline" onClick={() => void cargarMas()} disabled={cargandoMas}>
            {cargandoMas ? <LoaderCircleIcon className="animate-spin" /> : null}
            Cargar más
          </Button>
        </div>
      ) : null}
    </>
  );
}

function TarjetaInteresado({
  item,
  ahora,
  onCambio,
}: {
  item: ListaEsperaAdminDto;
  ahora: Date;
  onCambio: (fila: ListaEsperaAdminDto) => void;
}) {
  const { session } = useAuth();
  const [notas, setNotas] = React.useState(item.notas ?? "");
  const [guardandoNotas, setGuardandoNotas] = React.useState(false);
  const notasCambiadas = notas.trim() !== (item.notas ?? "");
  const idCasilla = `contactado-${item.id}`;

  React.useEffect(() => setNotas(item.notas ?? ""), [item.notas]);

  // Optimistic: the box changes at once and goes back if the API says no.
  const cambiarContactado = async (contactado: boolean) => {
    const anterior = item;
    onCambio({ ...item, contactado });
    try {
      onCambio(
        await session.json<ListaEsperaAdminDto>(`/api/admin/lista-espera/${item.id}/contactado`, {
          method: "PUT",
          body: JSON.stringify({ contactado }),
        }),
      );
    } catch (e) {
      onCambio(anterior);
      if (!(e instanceof SessionExpiredError)) toast.error(`No se ha podido guardar: ${mensajeError(e)}`);
    }
  };

  const guardarNotas = async () => {
    setGuardandoNotas(true);
    try {
      onCambio(
        await session.json<ListaEsperaAdminDto>(`/api/admin/lista-espera/${item.id}/contactado`, {
          method: "PUT",
          body: JSON.stringify({ contactado: item.contactado, notas: notas.trim() || null }),
        }),
      );
      toast.success("Notas guardadas");
    } catch (e) {
      if (!(e instanceof SessionExpiredError)) toast.error(mensajeError(e));
    } finally {
      setGuardandoNotas(false);
    }
  };

  return (
    <Card className="grid gap-3" data-testid="interesado">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="truncate font-medium">{item.nombre}</p>
          <p className="text-sm text-muted-foreground">{item.curso}</p>
        </div>
        <label htmlFor={idCasilla} className="flex shrink-0 cursor-pointer items-center gap-2 rounded-lg border px-3 py-2 text-sm">
          <input
            id={idCasilla}
            type="checkbox"
            className="size-5 accent-[var(--primary)]"
            checked={item.contactado}
            onChange={(e) => void cambiarContactado(e.target.checked)}
          />
          Contactado
        </label>
      </div>

      <div className="flex flex-wrap gap-1.5">
        <Badge variant="muted" title={formatearFecha(item.creadoEn)}>
          {antiguedad(item.creadoEn, ahora)}
        </Badge>
        {item.baja ? (
          <Badge variant="warning">
            <BellOffIcon /> Se dio de baja de avisos
          </Badge>
        ) : null}
      </div>

      {item.edicionPreferida || item.fechaPreferida ? (
        <p className="text-sm">
          <span className="text-muted-foreground">Fecha preferida: </span>
          {[item.edicionPreferida, item.fechaPreferida].filter(Boolean).join(" · ")}
        </p>
      ) : null}

      <div className="flex flex-wrap gap-2">
        <Button asChild variant="outline" size="sm">
          <a href={telUrl(item.telefono)}>
            <PhoneIcon /> {item.telefono}
          </a>
        </Button>
        <Button asChild variant="outline" size="sm">
          <a
            href={whatsappUrl(item.telefono, `Hola ${item.nombre.split(" ")[0] ?? ""}, te escribo de Zoi Nail Academy por el curso ${item.curso}.`)}
            target="_blank"
            rel="noopener noreferrer"
          >
            <MessageCircleIcon /> WhatsApp
          </a>
        </Button>
        <Button asChild variant="ghost" size="sm" className="max-w-full">
          <a href={`mailto:${item.email}`}>
            <MailIcon /> <span className="truncate">{item.email}</span>
          </a>
        </Button>
      </div>

      <div className="grid gap-2">
        <Label htmlFor={`notas-${item.id}`} className="text-xs text-muted-foreground">
          Notas
        </Label>
        <Textarea
          id={`notas-${item.id}`}
          className="min-h-16"
          maxLength={LIMITES.notasListaEspera}
          value={notas}
          onChange={(e) => setNotas(e.target.value)}
        />
        {notasCambiadas ? (
          <Button size="sm" className="justify-self-end" onClick={() => void guardarNotas()} disabled={guardandoNotas}>
            {guardandoNotas ? <LoaderCircleIcon className="animate-spin" /> : null}
            Guardar notas
          </Button>
        ) : null}
      </div>
    </Card>
  );
}
