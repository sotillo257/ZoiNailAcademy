import type { Metadata } from "next";
import { Suspense } from "react";
import { Baja } from "@/components/sitio/baja";
import { Encabezado } from "@/components/sitio/encabezado";
import { Pie } from "@/components/sitio/pie";

export const metadata: Metadata = { title: "Baja de avisos", robots: { index: false, follow: false } };

export default function BajaPage() {
  return (
    <>
      <Encabezado aviso={{ texto: "Calendario 2027 ya disponible", enlace: { texto: "Ver fechas", href: "/formaciones#calendario" } }} />
      <main className="flex flex-1 items-center bg-z-cream px-[18px] py-16">
        <div className="mx-auto w-full max-w-md rounded-lg bg-white p-8">
          <Suspense>
            <Baja />
          </Suspense>
        </div>
      </main>
      <Pie fondo="blanco" />
    </>
  );
}
