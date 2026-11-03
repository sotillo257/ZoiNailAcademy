"use client";

import { SearchIcon } from "lucide-react";
import * as React from "react";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";

/** Big search box: "Buscar reserva por código, nombre o teléfono". */
export function BuscadorReservas({
  valorInicial = "",
  onBuscar,
  autoFocus,
  className,
}: {
  valorInicial?: string;
  onBuscar: (q: string) => void;
  autoFocus?: boolean;
  className?: string;
}) {
  const [q, setQ] = React.useState(valorInicial);
  React.useEffect(() => setQ(valorInicial), [valorInicial]);
  return (
    <form
      role="search"
      className={cn("relative", className)}
      onSubmit={(e) => {
        e.preventDefault();
        onBuscar(q.trim());
      }}
    >
      <label htmlFor="buscar-reserva" className="sr-only">
        Buscar reserva por código, nombre o teléfono
      </label>
      <SearchIcon className="pointer-events-none absolute left-3.5 top-1/2 size-5 -translate-y-1/2 text-muted-foreground" />
      <Input
        id="buscar-reserva"
        type="search"
        enterKeyHint="search"
        autoComplete="off"
        autoFocus={autoFocus}
        placeholder="Código, nombre o teléfono"
        className="h-12 rounded-xl pl-11 text-base shadow-sm"
        value={q}
        onChange={(e) => setQ(e.target.value)}
      />
    </form>
  );
}
