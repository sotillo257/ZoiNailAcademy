"use client";

import * as React from "react";
import { SessionExpiredError } from "@/lib/admin/errors";
import { useAuth } from "./auth-provider";

export interface ApiState<T> {
  data: T | undefined;
  error: unknown;
  cargando: boolean;
  recargar: () => Promise<void>;
  /** Local update (optimistic changes). */
  setData: React.Dispatch<React.SetStateAction<T | undefined>>;
}

/** GET a backoffice resource. `path = null` waits (e.g. until a dependency is known). Stale answers are dropped. */
export function useApi<T>(path: string | null): ApiState<T> {
  const { session } = useAuth();
  const [data, setData] = React.useState<T | undefined>(undefined);
  const [error, setError] = React.useState<unknown>(null);
  const [cargando, setCargando] = React.useState(path !== null);
  const ultima = React.useRef(0);

  const cargar = React.useCallback(async () => {
    if (path === null) return;
    const id = ++ultima.current;
    setCargando(true);
    setError(null);
    try {
      const resultado = await session.json<T>(path);
      if (id === ultima.current) setData(resultado);
    } catch (e) {
      // An expired session is handled by the panel guard (redirect to login).
      if (id === ultima.current && !(e instanceof SessionExpiredError)) setError(e);
    } finally {
      if (id === ultima.current) setCargando(false);
    }
  }, [path, session]);

  React.useEffect(() => {
    void cargar();
  }, [cargar]);

  return { data, error, cargando, recargar: cargar, setData };
}

/** Re-renders every `ms` so relative times ("caduca en 5 h") stay right. */
export function useAhora(ms = 60_000): Date {
  const [ahora, setAhora] = React.useState(() => new Date());
  React.useEffect(() => {
    const id = setInterval(() => setAhora(new Date()), ms);
    return () => clearInterval(id);
  }, [ms]);
  return ahora;
}

/** Value that follows `value` after `ms` without changes (search boxes). */
export function useDebounced<T>(value: T, ms = 300): T {
  const [debounced, setDebounced] = React.useState(value);
  React.useEffect(() => {
    const id = setTimeout(() => setDebounced(value), ms);
    return () => clearTimeout(id);
  }, [value, ms]);
  return debounced;
}
