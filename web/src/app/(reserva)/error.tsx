"use client";

import Link from "next/link";
import { Boton, clasesBoton } from "@/components/sitio/boton";

export default function ErrorPagina({ reset }: { error: Error & { digest?: string }; reset: () => void }) {
  return (
    <main className="flex flex-1 flex-col items-center justify-center bg-z-cream px-[18px] py-24 text-center">
      <h1 className="text-[26px] font-bold md:text-[34px]">Algo ha fallado</h1>
      <p className="mt-3 max-w-md text-sm text-z-text">No hemos podido cargar esta página. Inténtalo de nuevo en un momento.</p>
      <div className="mt-6 flex flex-col gap-3 sm:flex-row">
        <Boton onClick={reset}>Reintentar</Boton>
        <Link href="/contacto" className={clasesBoton("contorno")}>
          Escríbenos
        </Link>
      </div>
    </main>
  );
}
