import type { MetadataRoute } from "next";

export default function robots(): MetadataRoute.Robots {
  const base = process.env.NEXT_PUBLIC_SITE_URL;
  return {
    rules: [{ userAgent: "*", allow: "/", disallow: ["/admin", "/reserva/", "/reservar/", "/baja"] }],
    sitemap: base ? `${base}/sitemap.xml` : undefined,
  };
}
