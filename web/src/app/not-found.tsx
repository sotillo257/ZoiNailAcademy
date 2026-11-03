import Link from "next/link";
import { clasesBoton } from "@/components/sitio/boton";
import { LogoZ } from "@/components/sitio/logo";

export default function NotFound() {
  return (
    <main className="flex min-h-dvh flex-col items-center justify-center bg-z-cream px-[18px] text-center font-montserrat text-z-ink">
      <LogoZ />
      <h1 className="mt-6 text-[28px] font-bold md:text-[40px]">No encontramos esta página</h1>
      <p className="mt-3 max-w-md text-sm text-z-text">
        Puede que el enlace esté mal escrito o que la página ya no exista. Si buscabas una reserva, revisa el enlace de tu email.
      </p>
      <div className="mt-7 flex flex-col gap-3 sm:flex-row">
        <Link href="/" className={clasesBoton("primario")}>
          Ir al inicio
        </Link>
        <Link href="/formaciones" className={clasesBoton("oscuro")}>
          Ver formaciones
        </Link>
      </div>
    </main>
  );
}
