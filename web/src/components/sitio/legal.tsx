import { Encabezado } from "./encabezado";
import { Pie } from "./pie";
import { Contenedor } from "./seccion";

/** Page of a legal text. The texts are placeholders until the client provides the real ones. */
export function PaginaLegal({ titulo, actualizado, children }: { titulo: string; actualizado: string; children: React.ReactNode }) {
  return (
    <>
      <Encabezado aviso={{ texto: "Calendario 2027 ya disponible", enlace: { texto: "Ver fechas", href: "/formaciones#calendario" } }} />
      <main className="flex-1">
        <section className="bg-z-cream py-10 text-center md:py-14">
          <h1 className="px-[18px] text-[28px] font-bold md:text-[40px]">{titulo}</h1>
          <p className="mt-2 text-xs text-z-muted">Última actualización: {actualizado}</p>
        </section>
        <Contenedor className="legal max-w-[760px] py-10 md:py-14">{children}</Contenedor>
      </main>
      <Pie fondo="blanco" />
    </>
  );
}
