/** Booking flow: its own top bar (no site header), cream background; a phone column on mobile, two columns on desktop. */
export default function ReservaLayout({ children }: { children: React.ReactNode }) {
  return <div className="min-h-dvh bg-z-cream font-montserrat text-z-ink">{children}</div>;
}
