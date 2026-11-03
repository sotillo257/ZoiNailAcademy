import Link from "next/link";
import { NAV, SITIO } from "@/lib/sitio/datos";
import { cn } from "@/lib/utils";
import { LogoZ } from "./logo";

/**
 * Footer of the design. `fondo` follows the section above it (cream after a red band, white after a cream one);
 * `datos` adds the spa's address line; `enlaces` the navigation (desktop only on the contact page).
 */
export function Pie({
  fondo = "crema",
  datos = true,
  enlaces = true,
  inicio = false,
}: {
  fondo?: "crema" | "blanco";
  datos?: boolean;
  enlaces?: boolean;
  /** The home page footer lists the other pages only. */
  inicio?: boolean;
}) {
  const items = NAV.filter((n) => !(inicio && n.href === "/"));
  return (
    <footer className={cn("px-4 py-10 text-center md:py-12", fondo === "crema" ? "bg-z-cream" : "border-t border-z-line bg-white")}>
      <Link href="/" aria-label={`${SITIO.nombre}, inicio`} className="inline-block">
        <LogoZ />
      </Link>
      {enlaces ? (
        <ul className="mt-5 hidden flex-wrap justify-center gap-x-6 gap-y-2 md:flex">
          {items.map((n) => (
            <li key={n.label}>
              <Link href={n.href} className="text-xs font-semibold text-z-ink hover:text-z-burgundy">
                {n.label.charAt(0) + n.label.slice(1).toLowerCase()}
              </Link>
            </li>
          ))}
        </ul>
      ) : null}
      {datos ? (
        <p className="mx-auto mt-4 max-w-xs text-[11px] leading-relaxed text-z-text md:max-w-none">
          {SITIO.direccion}, {SITIO.ciudad} · {SITIO.whatsappTexto} · {SITIO.horarioSemana}
        </p>
      ) : null}
      {enlaces ? (
        <p className="mt-3 md:hidden">
          <Link href="/contacto" className="text-xs font-semibold text-z-ink">
            Contacto
          </Link>
        </p>
      ) : null}
      <p className="mt-4 text-[11px] text-z-muted">
        <Link href="/privacidad" className="hover:text-z-ink">
          Política de privacidad
        </Link>{" "}
        · <Link href="/aviso-legal" className="hover:text-z-ink">Aviso legal</Link> ·{" "}
        <Link href="/condiciones" className="hover:text-z-ink">Condiciones</Link> · © 2026 {SITIO.nombre}
      </p>
    </footer>
  );
}
