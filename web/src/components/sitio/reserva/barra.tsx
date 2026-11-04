import Link from "next/link";
import { ChevronLeftIcon, LockIcon } from "lucide-react";
import { SITIO } from "@/lib/sitio/datos";
import { LogoZ } from "../logo";

/**
 * Width of the booking screens: a phone column on mobile, wider on tablet and the two-column layout on desktop.
 * Shared by the top bar and the pages so their edges line up.
 */
export const ANCHO_RESERVA = "mx-auto w-full max-w-[480px] px-[18px] md:max-w-[768px] md:px-6 lg:max-w-[1100px]";

/** Top bar of the booking screens: back button + title; on desktop also the brand and a "secure payment" note. */
export function BarraReserva({ volver, titulo = "Reserva tu plaza" }: { volver: string; titulo?: string }) {
  return (
    <header className="border-b border-z-line bg-white">
      <div className={`${ANCHO_RESERVA} flex h-[68px] items-center gap-3 md:h-[76px]`}>
        <Link
          href={volver}
          aria-label="Volver"
          className="inline-flex size-11 items-center justify-center rounded-full bg-z-cream text-z-ink hover:bg-z-photo"
        >
          <ChevronLeftIcon className="size-5" aria-hidden />
        </Link>
        <p className="text-[17px] font-bold md:text-xl">{titulo}</p>
        <div className="ml-auto hidden items-center gap-6 md:flex">
          <p className="flex items-center gap-1.5 text-xs font-bold text-z-text">
            <LockIcon className="size-3.5" aria-hidden />
            Pago seguro
          </p>
          <Link href="/" className="flex items-center gap-2.5" aria-label={`${SITIO.nombre} · Inicio`}>
            <LogoZ className="size-10 text-base" />
            <span className="hidden leading-tight lg:block">
              <span className="block text-sm font-bold">{SITIO.nombre}</span>
              <span className="block text-[11px] text-z-text">{SITIO.subtitulo}</span>
            </span>
          </Link>
        </div>
      </div>
    </header>
  );
}
