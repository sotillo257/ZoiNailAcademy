import { SITIO, enlace } from "@/lib/sitio/datos";
import { BotonLink } from "./boton";

/** "No Te Hagas La Loca" red band that closes the home page. */
export function CtaFinal() {
  return (
    <section aria-labelledby="cta-final" className="bg-z-burgundy px-[18px] py-12 text-center text-white md:py-16">
      <h2 id="cta-final" className="text-[26px] font-bold leading-tight md:text-[34px]">
        No Te Hagas La Loca
      </h2>
      <p className="mx-auto mt-3 max-w-md text-sm leading-relaxed md:mt-4 md:max-w-[520px] md:text-[15px]">
        Si quieres aprender, empieza hoy. Si quieres unas uñas de salón, reserva tu cita.
      </p>
      <div className="mx-auto mt-6 flex max-w-sm flex-col gap-3 md:max-w-none md:flex-row md:justify-center">
        <BotonLink href="/formaciones" variante="blanco">
          Quiero formarme
        </BotonLink>
        <BotonLink href={enlace(SITIO.reservarCitaUrl)} variante="oscuro">
          Reservar cita
        </BotonLink>
      </div>
    </section>
  );
}
