import Link from "next/link";
import * as React from "react";
import { cn } from "@/lib/utils";

const VARIANTES = {
  primario: "bg-z-burgundy text-white hover:bg-z-burgundy-dark",
  oscuro: "bg-z-ink text-white hover:bg-black",
  blanco: "bg-white text-z-burgundy hover:bg-z-cream",
  contorno: "border border-z-ink bg-white text-z-ink hover:bg-z-cream",
  whatsapp: "bg-z-green text-white hover:bg-z-green-dark",
} as const;

const TAMANOS = {
  md: "h-12 px-8 text-xs",
  sm: "h-10 px-6 text-[11px]",
  lg: "h-14 px-8 text-sm",
} as const;

export type VarianteBoton = keyof typeof VARIANTES;

export function clasesBoton(variante: VarianteBoton = "primario", tamano: keyof typeof TAMANOS = "md", ancho = false) {
  return cn(
    "inline-flex items-center justify-center gap-2 rounded-full font-bold uppercase tracking-[0.06em] transition-colors",
    "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-z-burgundy",
    "disabled:pointer-events-none disabled:opacity-60",
    VARIANTES[variante],
    TAMANOS[tamano],
    ancho && "w-full",
  );
}

type Comun = { variante?: VarianteBoton; tamano?: keyof typeof TAMANOS; ancho?: boolean; className?: string };

/** Pill link of the design. External links open in a new tab. */
export function BotonLink({
  href,
  variante,
  tamano,
  ancho,
  className,
  children,
  ...props
}: Comun & Omit<React.ComponentProps<"a">, "href"> & { href: string }) {
  const clases = cn(clasesBoton(variante, tamano, ancho), className);
  if (/^(https?:|mailto:|tel:)/.test(href)) {
    return (
      <a href={href} className={clases} target="_blank" rel="noopener noreferrer" {...props}>
        {children}
      </a>
    );
  }
  return (
    <Link href={href} className={clases} {...props}>
      {children}
    </Link>
  );
}

export function Boton({ variante, tamano, ancho, className, type = "button", ...props }: Comun & React.ComponentProps<"button">) {
  return <button type={type} className={cn(clasesBoton(variante, tamano, ancho), className)} {...props} />;
}
