import Link from "next/link";
import { ChevronLeftIcon } from "lucide-react";

/** Top bar of the booking screens: back button + title. */
export function BarraReserva({ volver, titulo = "Reserva tu plaza" }: { volver: string; titulo?: string }) {
  return (
    <header className="bg-white">
      <div className="mx-auto flex h-[68px] max-w-[480px] items-center gap-3 px-[18px]">
        <Link
          href={volver}
          aria-label="Volver"
          className="inline-flex size-11 items-center justify-center rounded-full bg-z-cream text-z-ink hover:bg-z-photo"
        >
          <ChevronLeftIcon className="size-5" aria-hidden />
        </Link>
        <p className="text-[17px] font-bold">{titulo}</p>
      </div>
    </header>
  );
}
