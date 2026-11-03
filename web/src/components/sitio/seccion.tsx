import { cn } from "@/lib/utils";

/** Centered title + subtitle of the design's sections. */
export function TituloSeccion({ titulo, subtitulo, className, id }: { titulo: string; subtitulo?: string; className?: string; id?: string }) {
  return (
    <div className={cn("text-center", className)}>
      <h2 id={id} className="text-2xl font-bold leading-tight text-z-ink md:text-[34px]">
        {titulo}
      </h2>
      {subtitulo ? <p className="mt-2 text-[13px] text-z-text md:text-sm">{subtitulo}</p> : null}
    </div>
  );
}

/** Content width of the design (1100 px) with the phone gutter. */
export function Contenedor({ className, children }: { className?: string; children: React.ReactNode }) {
  return <div className={cn("mx-auto w-full max-w-[1100px] px-[18px] md:px-6 xl:px-0", className)}>{children}</div>;
}
