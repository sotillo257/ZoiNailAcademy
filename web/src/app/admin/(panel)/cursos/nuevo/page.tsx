"use client";

import { CursoForm } from "@/components/admin/cursos/curso-form";
import { Cabecera } from "@/components/admin/estados";
import { SoloSuperAdmin } from "@/components/admin/solo-superadmin";

export default function NuevoCursoPage() {
  return (
    <SoloSuperAdmin>
      <Cabecera titulo="Nuevo curso" />
      <CursoForm />
    </SoloSuperAdmin>
  );
}
