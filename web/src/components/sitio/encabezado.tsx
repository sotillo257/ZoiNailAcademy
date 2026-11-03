"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import * as React from "react";
import { MenuIcon, XIcon } from "lucide-react";
import { NAV, SITIO, enlace } from "@/lib/sitio/datos";
import { cn } from "@/lib/utils";
import { BotonLink } from "./boton";
import { LogoZ } from "./logo";

export type Aviso = { texto: string; textoMovil?: string; enlace: { texto: string; href: string } };

function EnlaceNav({
  href,
  propio,
  children,
  className,
  onClick,
}: {
  href: string;
  /** Page of this web (not an external link, nor a fallback to /contacto): it can be the current page. */
  propio: boolean;
  children: React.ReactNode;
  className?: string;
  onClick?: () => void;
}) {
  const pathname = usePathname();
  const activo = propio && (href === "/" ? pathname === "/" : pathname.startsWith(href));
  const props = { className: cn(className, activo ? "text-z-burgundy" : "text-z-ink hover:text-z-burgundy"), "aria-current": activo ? ("page" as const) : undefined, onClick };
  return href.startsWith("/") ? (
    <Link href={href} {...props}>
      {children}
    </Link>
  ) : (
    <a href={href} target="_blank" rel="noopener noreferrer" {...props}>
      {children}
    </a>
  );
}

/** Announcement bar + header of the design. On phones the navigation folds into a menu. */
export function Encabezado({ aviso }: { aviso: Aviso }) {
  const [abierto, setAbierto] = React.useState(false);
  const pathname = usePathname();
  React.useEffect(() => setAbierto(false), [pathname]);

  return (
    <header className="relative z-30">
      <div className="bg-z-burgundy px-4 py-2.5 text-center text-[11px] font-semibold leading-tight text-white md:text-xs">
        {aviso.textoMovil ? (
          <>
            <span className="md:hidden">{aviso.textoMovil}</span>
            <span className="hidden md:inline">{aviso.texto}</span>
          </>
        ) : (
          aviso.texto
        )}{" "}
        ·{" "}
        <Link href={aviso.enlace.href} className="font-bold underline underline-offset-2">
          {aviso.enlace.texto}
        </Link>
      </div>
      <div className="border-b border-z-line bg-white">
        <div className="mx-auto flex h-16 max-w-[1100px] items-center justify-between gap-6 px-[18px] md:h-[84px] md:px-6 xl:px-0">
          <Link href="/" className="flex items-center gap-3">
            <LogoZ className="size-9 text-base md:size-11 md:text-lg" />
            <span className="leading-tight">
              <span className="block text-[15px] font-bold text-z-ink md:text-base">{SITIO.nombre}</span>
              <span className="block text-[10px] text-z-muted md:text-[11px]">{SITIO.subtitulo}</span>
            </span>
          </Link>

          <nav aria-label="Principal" className="hidden lg:block">
            <ul className="flex items-center gap-7">
              {NAV.map((item) => (
                <li key={item.label}>
                  <EnlaceNav href={item.href} propio={item.propio} className="text-[11px] font-bold uppercase tracking-[0.06em] transition-colors">
                    {item.label}
                  </EnlaceNav>
                </li>
              ))}
            </ul>
          </nav>

          <BotonLink href={enlace(SITIO.reservarCitaUrl)} variante="oscuro" tamano="sm" className="hidden lg:inline-flex">
            Reservar cita
          </BotonLink>

          <button
            type="button"
            className="inline-flex size-10 items-center justify-center rounded-full bg-z-cream text-z-ink lg:hidden"
            aria-expanded={abierto}
            aria-controls="menu-movil"
            aria-label={abierto ? "Cerrar menú" : "Abrir menú"}
            onClick={() => setAbierto((v) => !v)}
          >
            {abierto ? <XIcon className="size-5" aria-hidden /> : <MenuIcon className="size-5" aria-hidden />}
          </button>
        </div>
      </div>

      {abierto ? (
        <nav id="menu-movil" aria-label="Principal" className="absolute inset-x-0 top-full border-b border-z-line bg-white shadow-lg lg:hidden">
          <ul className="px-[18px] py-2">
            {NAV.map((item) => (
              <li key={item.label} className="border-b border-z-line last:border-0">
                <EnlaceNav href={item.href} propio={item.propio} className="block py-4 text-xs font-bold uppercase tracking-[0.06em]" onClick={() => setAbierto(false)}>
                  {item.label}
                </EnlaceNav>
              </li>
            ))}
          </ul>
          <div className="px-[18px] pb-5">
            <BotonLink href={enlace(SITIO.reservarCitaUrl)} variante="oscuro" ancho>
              Reservar cita
            </BotonLink>
          </div>
        </nav>
      ) : null}
    </header>
  );
}
