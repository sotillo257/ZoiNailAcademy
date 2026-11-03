import type { Metadata } from "next";
import { PaginaLegal } from "@/components/sitio/legal";
import { SITIO } from "@/lib/sitio/datos";

export const metadata: Metadata = { title: "Política de privacidad", alternates: { canonical: "/privacidad" } };

export default function PrivacidadPage() {
  return (
    <PaginaLegal titulo="Política de privacidad" actualizado="[fecha]">
      <p>[PENDIENTE: texto definitivo de la clienta o su gestoría. Lo que sigue describe lo que hace la web.]</p>
      <h2>Qué datos recogemos y para qué</h2>
      <ul>
        <li>Reserva de plaza: nombre, email y teléfono, para gestionar tu plaza, el pago y avisarte de cambios.</li>
        <li>Lista de espera: nombre, email, teléfono y la formación que te interesa, para avisarte de nuevas fechas.</li>
        <li>Contacto: nombre, email, teléfono (opcional) y tu mensaje, para contestarte.</li>
      </ul>
      <h2>Pagos</h2>
      <p>Los pagos con tarjeta los procesa Stripe; esta web no ve ni guarda los datos de tu tarjeta.</p>
      <h2>Tus derechos</h2>
      <p>Puedes pedir acceso, rectificación o supresión de tus datos escribiendo a {SITIO.email}.</p>
    </PaginaLegal>
  );
}
