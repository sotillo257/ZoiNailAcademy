import type { Metadata } from "next";
import { PaginaLegal } from "@/components/sitio/legal";

export const metadata: Metadata = { title: "Condiciones de reserva", alternates: { canonical: "/condiciones" } };

export default function CondicionesPage() {
  return (
    <PaginaLegal titulo="Condiciones de reserva" actualizado="[fecha]">
      <p>[PENDIENTE: condiciones definitivas de la clienta: cancelaciones, devoluciones, cambios de fecha.]</p>
      <h2>Reserva de plaza</h2>
      <ul>
        <li>Al reservar pagas la reserva de plaza indicada en cada formación; el resto del precio se paga en persona.</li>
        <li>Con tarjeta, la plaza queda confirmada al momento.</li>
        <li>
          Con transferencia o Bizum, la plaza queda guardada durante el plazo que se indica al reservar. Si no recibimos el
          pago a tiempo, la plaza se libera.
        </li>
      </ul>
      <h2>Cancelaciones y cambios</h2>
      <p>[PENDIENTE]</p>
    </PaginaLegal>
  );
}
