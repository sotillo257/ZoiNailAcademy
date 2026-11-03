import { cn } from "@/lib/utils";

/** Paid / pending / free seats as one bar, with the numbers spelled out (the colours are not the only cue). */
export function BarraOcupacion({
  plazas,
  pagadas,
  pendientes,
  className,
}: {
  plazas: number;
  pagadas: number;
  pendientes: number;
  className?: string;
}) {
  const libres = Math.max(plazas - pagadas - pendientes, 0);
  const pct = (n: number) => (plazas > 0 ? `${(Math.min(n, plazas) / plazas) * 100}%` : "0%");
  return (
    <div className={cn("grid gap-1", className)}>
      <div
        className="flex h-2.5 w-full overflow-hidden rounded-full bg-ocupacion-libres"
        role="img"
        aria-label={`${pagadas} pagadas, ${pendientes} pendientes, ${libres} libres de ${plazas}`}
      >
        <div className="bg-ocupacion-pagadas" style={{ width: pct(pagadas) }} />
        <div className="bg-ocupacion-pendientes" style={{ width: pct(pendientes) }} />
      </div>
      <p className="text-xs text-muted-foreground">
        <span className="font-medium text-foreground">{pagadas}</span> pagadas ·{" "}
        <span className="font-medium text-foreground">{pendientes}</span> pendientes ·{" "}
        <span className="font-medium text-foreground">{libres}</span> libres de {plazas}
      </p>
    </div>
  );
}
