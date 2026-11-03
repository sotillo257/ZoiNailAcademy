import type { Metadata } from "next";
import { PaginaLegal } from "@/components/sitio/legal";

export const metadata: Metadata = { title: "Política de cookies", alternates: { canonical: "/cookies" } };

export default function CookiesPage() {
  return (
    <PaginaLegal titulo="Política de cookies" actualizado="[fecha]">
      <p>Esta web no usa cookies de analítica ni de publicidad.</p>
      <p>
        Solo se usa una cookie técnica en la zona privada de gestión (inicio de sesión), necesaria para su funcionamiento.
        Al pagar con tarjeta, Stripe puede usar sus propias cookies técnicas en su página de pago.
      </p>
    </PaginaLegal>
  );
}
