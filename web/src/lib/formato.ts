const euros = new Intl.NumberFormat("es-ES", { style: "currency", currency: "EUR" });

export function formatearImporte(importe: number): string {
  return euros.format(importe);
}

/**
 * Digits for wa.me: international numbers keep their prefix (+ or 00), a Spanish number without prefix
 * (9 digits starting with 6, 7, 8 or 9) gets 34.
 */
export function telefonoInternacional(telefono: string): string {
  const limpio = telefono.trim();
  let digitos = limpio.replace(/\D/g, "");
  if (limpio.startsWith("00")) digitos = digitos.slice(2);
  else if (!limpio.startsWith("+") && /^[6-9]\d{8}$/.test(digitos)) digitos = `34${digitos}`;
  return digitos;
}

export function whatsappUrl(telefono: string, mensaje?: string): string {
  const base = `https://wa.me/${telefonoInternacional(telefono)}`;
  return mensaje ? `${base}?text=${encodeURIComponent(mensaje)}` : base;
}

export function telUrl(telefono: string): string {
  return `tel:${telefono.replace(/[^\d+]/g, "")}`;
}
