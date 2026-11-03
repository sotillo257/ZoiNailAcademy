import type { MetadataRoute } from "next";

export default function sitemap(): MetadataRoute.Sitemap {
  const base = process.env.NEXT_PUBLIC_SITE_URL ?? "http://localhost:3000";
  const rutas: { ruta: string; prioridad: number }[] = [
    { ruta: "/", prioridad: 1 },
    { ruta: "/formaciones", prioridad: 0.9 },
    { ruta: "/contacto", prioridad: 0.6 },
    { ruta: "/condiciones", prioridad: 0.2 },
    { ruta: "/privacidad", prioridad: 0.2 },
    { ruta: "/aviso-legal", prioridad: 0.2 },
    { ruta: "/cookies", prioridad: 0.1 },
  ];
  return rutas.map(({ ruta, prioridad }) => ({ url: `${base}${ruta}`, changeFrequency: "weekly", priority: prioridad }));
}
