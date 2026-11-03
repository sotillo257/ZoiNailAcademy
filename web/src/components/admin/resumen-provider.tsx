"use client";

import { usePathname } from "next/navigation";
import * as React from "react";
import type { ResumenDto } from "@/lib/admin/types";
import { useApi } from "./use-api";

interface ResumenContextValue {
  resumen: ResumenDto | undefined;
  error: unknown;
  cargando: boolean;
  recargar: () => Promise<void>;
}

const ResumenContext = React.createContext<ResumenContextValue | null>(null);

/** Counters of the home page and of the Reservas badge; reloaded on every navigation and after each action. */
export function ResumenProvider({ children }: { children: React.ReactNode }) {
  const { data, error, cargando, recargar } = useApi<ResumenDto>("/api/admin/resumen");
  const pathname = usePathname();
  const primera = React.useRef(true);

  React.useEffect(() => {
    if (primera.current) {
      primera.current = false;
      return;
    }
    void recargar();
  }, [pathname, recargar]);

  const value = React.useMemo(() => ({ resumen: data, error, cargando, recargar }), [data, error, cargando, recargar]);
  return <ResumenContext.Provider value={value}>{children}</ResumenContext.Provider>;
}

export function useResumen(): ResumenContextValue {
  const value = React.useContext(ResumenContext);
  if (!value) throw new Error("useResumen fuera de ResumenProvider");
  return value;
}
