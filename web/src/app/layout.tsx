import type { Metadata, Viewport } from "next";
import localFont from "next/font/local";
import "./globals.css";

// Montserrat (variable), self-hosted from @fontsource so the build needs no network.
const montserrat = localFont({
  src: [
    { path: "../../node_modules/@fontsource-variable/montserrat/files/montserrat-latin-wght-normal.woff2", style: "normal" },
    { path: "../../node_modules/@fontsource-variable/montserrat/files/montserrat-latin-wght-italic.woff2", style: "italic" },
  ],
  weight: "100 900",
  display: "swap",
  variable: "--font-montserrat-var",
});

export const metadata: Metadata = {
  title: { default: "Zoila Buzetta · Nail Artist & Educadora", template: "%s · Zoila Buzetta" },
  metadataBase: process.env.NEXT_PUBLIC_SITE_URL ? new URL(process.env.NEXT_PUBLIC_SITE_URL) : undefined,
};

export const viewport: Viewport = {
  width: "device-width",
  initialScale: 1,
  themeColor: "#8b0b0a",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="es" className={montserrat.variable}>
      <body className="min-h-dvh antialiased">{children}</body>
    </html>
  );
}
