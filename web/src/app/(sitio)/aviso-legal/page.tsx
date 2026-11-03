import type { Metadata } from "next";
import { PaginaLegal } from "@/components/sitio/legal";
import { SITIO } from "@/lib/sitio/datos";

export const metadata: Metadata = { title: "Aviso legal", alternates: { canonical: "/aviso-legal" } };

export default function AvisoLegalPage() {
  return (
    <PaginaLegal titulo="Aviso legal" actualizado="[fecha]">
      <p>[PENDIENTE: texto del aviso legal que aporta la clienta o su gestoría.]</p>
      <h2>Titular de la web</h2>
      <ul>
        <li>Titular: [nombre o razón social]</li>
        <li>NIF: [NIF]</li>
        <li>Domicilio: {SITIO.direccion}, {SITIO.codigoPostal} {SITIO.ciudad}</li>
        <li>Email: {SITIO.email}</li>
      </ul>
    </PaginaLegal>
  );
}
