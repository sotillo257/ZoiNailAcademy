"use client";

import { useAuth } from "./auth-provider";
import { SinPermiso } from "./estados";

/** Hides SuperAdmin screens from an Admin (the API enforces it anyway with 403). */
export function SoloSuperAdmin({ children }: { children: React.ReactNode }) {
  const { user } = useAuth();
  return user?.rol === "SuperAdmin" ? <>{children}</> : <SinPermiso />;
}
