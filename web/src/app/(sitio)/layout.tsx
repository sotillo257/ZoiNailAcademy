/** Public web: Montserrat, the design's ink colour. Header and footer vary per page, so each page renders them. */
export default function SitioLayout({ children }: { children: React.ReactNode }) {
  return <div className="flex min-h-dvh flex-col bg-white font-montserrat text-z-ink">{children}</div>;
}
