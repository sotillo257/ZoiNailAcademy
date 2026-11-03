/** Where to go after login: only backoffice paths of this site (no open redirects, no loop back to login). */
export function destinoSeguro(next: string | null | undefined): string {
  if (!next || !next.startsWith("/admin") || next.startsWith("//") || next.includes("\\")) return "/admin";
  if (next === "/admin/login" || next.startsWith("/admin/login?") || next.startsWith("/admin/login/")) return "/admin";
  return next;
}
