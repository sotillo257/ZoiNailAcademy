"use client";

import {
  CalendarDaysIcon,
  GraduationCapIcon,
  HomeIcon,
  ListChecksIcon,
  LogOutIcon,
  MailIcon,
  MenuIcon,
  ReceiptTextIcon,
  RefreshCwIcon,
  UserIcon,
  type LucideIcon,
} from "lucide-react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import * as React from "react";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/card";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { mensajeError } from "@/lib/admin/errors";
import { cn } from "@/lib/utils";
import { useAuth } from "./auth-provider";
import { ResumenProvider, useResumen } from "./resumen-provider";

interface ItemNav {
  href: string;
  label: string;
  icon: LucideIcon;
  /** Shown in the bottom bar on a phone (the rest goes in the "Más" menu). */
  principal: boolean;
  soloSuperAdmin?: boolean;
  contador?: boolean;
}

const NAV: ItemNav[] = [
  { href: "/admin", label: "Inicio", icon: HomeIcon, principal: true },
  { href: "/admin/reservas", label: "Reservas", icon: ReceiptTextIcon, principal: true, contador: true },
  { href: "/admin/ediciones", label: "Fechas", icon: CalendarDaysIcon, principal: true },
  { href: "/admin/lista-espera", label: "Lista de espera", icon: ListChecksIcon, principal: true },
  { href: "/admin/cursos", label: "Cursos", icon: GraduationCapIcon, principal: false, soloSuperAdmin: true },
  { href: "/admin/emails", label: "Emails", icon: MailIcon, principal: false, soloSuperAdmin: true },
  { href: "/admin/cuenta", label: "Cuenta", icon: UserIcon, principal: false },
];

function activo(pathname: string, href: string): boolean {
  return href === "/admin" ? pathname === "/admin" : pathname === href || pathname.startsWith(`${href}/`);
}

/** Guard + navigation of every authenticated backoffice page. */
export function PanelShell({ children }: { children: React.ReactNode }) {
  const { estado, motivo, comprobar, errorComprobacion } = useAuth();
  const router = useRouter();

  React.useEffect(() => {
    // Restores the session with the cookie on load; calling it again while it runs shares the same refresh.
    if (estado === "comprobando") void comprobar();
  }, [estado, comprobar]);

  React.useEffect(() => {
    if (estado !== "anonimo") return;
    const next = `${window.location.pathname}${window.location.search}`;
    // After a voluntary logout there is nothing to come back to.
    const query = motivo === "logout" ? "" : `?next=${encodeURIComponent(next)}`;
    router.replace(`/admin/login${query}`);
  }, [estado, motivo, router]);

  if (estado === "sin-conexion") {
    return (
      <main className="mx-auto flex min-h-dvh max-w-md flex-col items-center justify-center gap-4 p-6 text-center">
        <p className="text-sm text-muted-foreground">{mensajeError(errorComprobacion)}</p>
        <Button onClick={() => void comprobar()}>
          <RefreshCwIcon /> Reintentar
        </Button>
      </main>
    );
  }

  if (estado !== "autenticado") return <ShellSkeleton />;

  return (
    <ResumenProvider>
      <Shell>{children}</Shell>
    </ResumenProvider>
  );
}

function Shell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const { user, logout } = useAuth();
  const { resumen } = useResumen();
  const [menuAbierto, setMenuAbierto] = React.useState(false);
  const esSuperAdmin = user?.rol === "SuperAdmin";
  const items = NAV.filter((i) => !i.soloSuperAdmin || esSuperAdmin);
  const porAtender = resumen ? resumen.pendientesDePago + resumen.requierenRevision : 0;

  React.useEffect(() => setMenuAbierto(false), [pathname]);

  const salir = () => void logout();

  return (
    <div className="min-h-dvh bg-muted/40 md:flex">
      {/* Desktop: sidebar */}
      <aside className="sticky top-0 hidden h-dvh w-60 shrink-0 flex-col border-r bg-card md:flex">
        <Marca className="px-5 py-5" />
        <nav aria-label="Backoffice" className="flex flex-1 flex-col gap-1 px-3">
          {items.map((item) => (
            <EnlaceNav key={item.href} item={item} activo={activo(pathname, item.href)} contador={item.contador ? porAtender : 0} />
          ))}
        </nav>
        <div className="border-t p-3">
          <p className="truncate px-3 pb-2 text-xs text-muted-foreground" title={user?.email}>
            {user?.nombre} · {user?.rol}
          </p>
          <button
            type="button"
            onClick={salir}
            className="flex w-full items-center gap-3 rounded-lg px-3 py-2 text-sm text-muted-foreground hover:bg-muted hover:text-foreground"
          >
            <LogOutIcon className="size-4" aria-hidden /> Salir
          </button>
        </div>
      </aside>

      {/* Phone: top bar */}
      <header className="sticky top-0 z-30 flex items-center justify-between border-b bg-card/95 px-4 py-3 backdrop-blur md:hidden">
        <Marca />
        <Button variant="ghost" size="icon" aria-label="Más opciones" onClick={() => setMenuAbierto(true)}>
          <MenuIcon />
        </Button>
      </header>

      <main className="mx-auto w-full max-w-5xl min-w-0 px-4 pb-28 pt-4 md:px-8 md:pb-10 md:pt-8">{children}</main>

      {/* Phone: bottom bar */}
      <nav
        aria-label="Backoffice"
        className="fixed inset-x-0 bottom-0 z-30 grid grid-cols-4 border-t bg-card pb-[env(safe-area-inset-bottom)] md:hidden"
      >
        {items
          .filter((i) => i.principal)
          .map((item) => {
            const esActivo = activo(pathname, item.href);
            const contador = item.contador ? porAtender : 0;
            return (
              <Link
                key={item.href}
                href={item.href}
                aria-current={esActivo ? "page" : undefined}
                className={cn(
                  "relative flex flex-col items-center gap-0.5 px-1 py-2 text-[11px] font-medium",
                  esActivo ? "text-primary" : "text-muted-foreground",
                )}
              >
                <span className="relative">
                  <item.icon className="size-5" aria-hidden />
                  {contador > 0 ? <Contador valor={contador} className="absolute -right-3 -top-2" /> : null}
                </span>
                <span className="truncate">{item.label}</span>
              </Link>
            );
          })}
      </nav>

      <Dialog open={menuAbierto} onOpenChange={setMenuAbierto}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Menú</DialogTitle>
            <DialogDescription>
              {user?.nombre} · {user?.rol}
            </DialogDescription>
          </DialogHeader>
          <nav aria-label="Más opciones" className="grid gap-1">
            {items
              .filter((i) => !i.principal)
              .map((item) => (
                <EnlaceNav key={item.href} item={item} activo={activo(pathname, item.href)} contador={0} />
              ))}
            <button
              type="button"
              onClick={salir}
              className="flex items-center gap-3 rounded-lg px-3 py-3 text-sm hover:bg-muted"
            >
              <LogOutIcon className="size-4" aria-hidden /> Salir
            </button>
          </nav>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function EnlaceNav({ item, activo: esActivo, contador }: { item: ItemNav; activo: boolean; contador: number }) {
  return (
    <Link
      href={item.href}
      aria-current={esActivo ? "page" : undefined}
      className={cn(
        "flex items-center gap-3 rounded-lg px-3 py-3 text-sm md:py-2",
        esActivo ? "bg-secondary font-medium text-secondary-foreground" : "text-muted-foreground hover:bg-muted hover:text-foreground",
      )}
    >
      <item.icon className="size-4" aria-hidden />
      <span className="flex-1">{item.label}</span>
      {contador > 0 ? <Contador valor={contador} /> : null}
    </Link>
  );
}

function Contador({ valor, className }: { valor: number; className?: string }) {
  return (
    <span
      className={cn(
        "inline-flex min-w-5 items-center justify-center rounded-full bg-accent px-1.5 text-[11px] font-bold leading-5 text-white",
        className,
      )}
      aria-label={`${valor} por atender`}
    >
      {valor > 99 ? "99+" : valor}
    </span>
  );
}

function Marca({ className }: { className?: string }) {
  return (
    <Link href="/admin" className={cn("flex flex-col leading-tight", className)}>
      <span className="text-base font-semibold tracking-tight text-primary">Zoi Nail Academy</span>
      <span className="text-xs text-muted-foreground">Backoffice</span>
    </Link>
  );
}

function ShellSkeleton() {
  return (
    <div className="min-h-dvh md:flex" aria-busy="true" aria-label="Cargando">
      <div className="hidden h-dvh w-60 border-r bg-card md:block" />
      <div className="mx-auto grid w-full max-w-5xl gap-4 p-4 md:p-8">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-12 w-full" />
        <div className="grid gap-3 md:grid-cols-3">
          <Skeleton className="h-24" />
          <Skeleton className="h-24" />
          <Skeleton className="h-24" />
        </div>
      </div>
    </div>
  );
}
