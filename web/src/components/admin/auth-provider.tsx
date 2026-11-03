"use client";

import * as React from "react";
import { SessionExpiredError } from "@/lib/admin/errors";
import { getSessionClient, type MotivoCambio, type SessionClient } from "@/lib/admin/session";
import type { UserDto } from "@/lib/admin/types";

export type EstadoAuth = "comprobando" | "autenticado" | "anonimo" | "sin-conexion";

interface AuthContextValue {
  estado: EstadoAuth;
  user: UserDto | null;
  /** Why the session last changed (to tell an expiry from a logout). */
  motivo: MotivoCambio | null;
  /** Why the last session check failed (estado "sin-conexion"). */
  errorComprobacion: unknown;
  session: SessionClient;
  /** Restores the session with the `rt` cookie (on mount of /admin). */
  comprobar: () => Promise<void>;
  login: (email: string, password: string) => Promise<UserDto>;
  logout: () => Promise<void>;
}

const AuthContext = React.createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const session = React.useMemo(() => getSessionClient(), []);
  const [user, setUser] = React.useState<UserDto | null>(() => session.actual?.user ?? null);
  const [estado, setEstado] = React.useState<EstadoAuth>(() => (session.actual ? "autenticado" : "comprobando"));
  const [motivo, setMotivo] = React.useState<MotivoCambio | null>(null);
  const [errorComprobacion, setErrorComprobacion] = React.useState<unknown>(null);

  React.useEffect(
    () =>
      session.subscribe((sesion, nuevoMotivo) => {
        setUser(sesion?.user ?? null);
        setEstado(sesion ? "autenticado" : "anonimo");
        setMotivo(nuevoMotivo);
      }),
    [session],
  );

  const comprobar = React.useCallback(async () => {
    if (session.actual) {
      setEstado("autenticado");
      return;
    }
    setEstado("comprobando");
    setErrorComprobacion(null);
    try {
      const sesion = await session.refresh();
      if (!sesion) setEstado("anonimo");
    } catch (error) {
      setErrorComprobacion(error);
      setEstado(error instanceof SessionExpiredError ? "anonimo" : "sin-conexion");
    }
  }, [session]);

  const login = React.useCallback(
    async (email: string, password: string) => (await session.login(email, password)).user,
    [session],
  );

  const logout = React.useCallback(() => session.logout(), [session]);

  const value = React.useMemo(
    () => ({ estado, user, motivo, errorComprobacion, session, comprobar, login, logout }),
    [estado, user, motivo, errorComprobacion, session, comprobar, login, logout],
  );
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const value = React.useContext(AuthContext);
  if (!value) throw new Error("useAuth fuera de AuthProvider");
  return value;
}
