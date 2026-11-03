"use client";

import * as React from "react";
import { ChevronLeftIcon, ChevronRightIcon } from "lucide-react";
import { cn } from "@/lib/utils";
import { Foto } from "./foto";

/** Photo carousel of a course: arrows over the photo and dots below (design of "Nuestras Formaciones"). */
export function Carrusel({ fotos, nombre, className }: { fotos: (string | null)[]; nombre: string; className?: string }) {
  const [actual, setActual] = React.useState(0);
  const total = Math.max(1, fotos.length);
  const ir = (i: number) => setActual((i + total) % total);

  return (
    <div className={className} role="group" aria-roledescription="carrusel" aria-label={`Fotos de ${nombre}`}>
      <div className="relative">
        <Foto
          src={fotos[actual] ?? null}
          alt={`Foto de ${nombre}${total > 1 ? ` (${actual + 1} de ${total})` : ""}`}
          className="aspect-[6/5] w-full px-14 md:aspect-auto md:h-[416px]"
          sizes="(min-width: 768px) 528px, 100vw"
        />
        {total > 1 ? (
          <>
            <button
              type="button"
              onClick={() => ir(actual - 1)}
              aria-label="Foto anterior"
              className="absolute left-3 top-1/2 inline-flex size-9 -translate-y-1/2 items-center justify-center rounded-full bg-white text-z-ink shadow-sm hover:bg-z-cream"
            >
              <ChevronLeftIcon className="size-4" aria-hidden />
            </button>
            <button
              type="button"
              onClick={() => ir(actual + 1)}
              aria-label="Foto siguiente"
              className="absolute right-3 top-1/2 inline-flex size-9 -translate-y-1/2 items-center justify-center rounded-full bg-white text-z-ink shadow-sm hover:bg-z-cream"
            >
              <ChevronRightIcon className="size-4" aria-hidden />
            </button>
          </>
        ) : null}
      </div>
      {total > 1 ? (
        <div className="mt-1.5 flex justify-center">
          {Array.from({ length: total }, (_, i) => (
            <button
              key={i}
              type="button"
              onClick={() => ir(i)}
              aria-label={`Ver foto ${i + 1}`}
              aria-current={i === actual}
              className="inline-flex h-6 min-w-6 items-center justify-center"
            >
              <span aria-hidden className={cn("h-1.5 rounded-full transition-all", i === actual ? "w-5 bg-z-burgundy" : "w-1.5 bg-[#d6cfce]")} />
            </button>
          ))}
        </div>
      ) : null}
    </div>
  );
}
