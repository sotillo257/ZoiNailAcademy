import { Badge } from "@/components/ui/badge";
import type { EdicionAdminDto } from "@/lib/admin/types";

export function EdicionBadges({ edicion: e, ahora }: { edicion: EdicionAdminDto; ahora: Date }) {
  const pasada = new Date(e.inicio) <= ahora;
  return (
    <span className="flex flex-wrap gap-1">
      {e.estado === "Programada" ? <Badge variant="success">Publicada</Badge> : null}
      {e.estado === "Oculta" ? <Badge variant="muted">Oculta</Badge> : null}
      {e.estado === "Cancelada" ? <Badge variant="destructive">Cancelada</Badge> : null}
      {pasada ? <Badge variant="outline">Pasada</Badge> : null}
      {!pasada && e.estado !== "Cancelada" && e.libres === 0 ? <Badge variant="secondary">Completa</Badge> : null}
      {!pasada && e.estado === "Programada" && !e.reservable && e.libres > 0 ? (
        <Badge variant="warning">Reservas cerradas</Badge>
      ) : null}
    </span>
  );
}
