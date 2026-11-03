import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { ReservaAdminDto } from "@/lib/admin/types";
import { ReservaCard } from "./reserva-card";

const ahora = new Date("2026-11-03T10:00:00Z");

function reserva(cambios: Partial<ReservaAdminDto> = {}): ReservaAdminDto {
  return {
    id: "r1",
    codigo: "CUR-7KQ4XM",
    edicionId: "e1",
    curso: "Uñas acrílicas",
    inicio: "2026-12-05T09:00:00+00:00",
    metodo: "Manual",
    estado: "Pendiente",
    nombre: "Lucía Pérez",
    email: "lucia@example.com",
    telefono: "612345678",
    importe: 149,
    precioCurso: 149,
    creadaEn: "2026-11-02T10:00:00Z",
    pagadaEn: null,
    expiraEn: "2026-11-03T15:00:00Z",
    requiereRevision: false,
    notaInterna: null,
    stripePaymentIntentId: null,
    version: "42",
    ...cambios,
  };
}

function boton(nombre: RegExp) {
  return screen.queryByRole("button", { name: nombre });
}

describe("ReservaCard", () => {
  it("ManualPending_ShowsConfirmExtendCancelAndUrgentExpiry", async () => {
    const onAccion = vi.fn();
    render(<ReservaCard reserva={reserva()} ahora={ahora} onAccion={onAccion} />);

    expect(screen.getByText("CUR-7KQ4XM")).toBeInTheDocument();
    expect(screen.getByText(/caduca en 5 h/)).toHaveClass("text-destructive");
    expect(boton(/confirmar pago/i)).toBeEnabled();
    expect(boton(/ampliar plazo/i)).toBeInTheDocument();
    expect(boton(/cancelar reserva/i)).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /612345678/ })).toHaveAttribute("href", "tel:612345678");
    expect(screen.getByRole("link", { name: /whatsapp/i }).getAttribute("href")).toMatch(/^https:\/\/wa\.me\/34612345678\?text=/);

    await userEvent.click(boton(/confirmar pago/i) as HTMLElement);
    expect(onAccion).toHaveBeenCalledWith("confirmar", expect.objectContaining({ id: "r1" }));
  });

  it("ManualPendingFarFromExpiry_IsNotRed", () => {
    render(<ReservaCard reserva={reserva({ expiraEn: "2026-11-04T10:00:00Z" })} ahora={ahora} onAccion={vi.fn()} />);
    expect(screen.getByText(/caduca en 24 h/)).not.toHaveClass("text-destructive");
  });

  it("StripePending_CannotBeConfirmedByHand", () => {
    render(<ReservaCard reserva={reserva({ metodo: "Stripe" })} ahora={ahora} onAccion={vi.fn()} />);
    expect(boton(/confirmar/i)).toBeNull();
    expect(boton(/ampliar/i)).toBeNull();
    expect(screen.getByText(/se confirma solo/i)).toBeInTheDocument();
  });

  it("ExpiredWithSeat_OffersConfirmAnyway", () => {
    render(<ReservaCard reserva={reserva({ estado: "Expirada", plazaDisponible: true })} ahora={ahora} onAccion={vi.fn()} />);
    expect(boton(/confirmar igualmente/i)).toBeEnabled();
    expect(boton(/cancelar/i)).toBeNull();
  });

  it("ExpiredWithoutSeat_DisablesConfirmAndExplainsWhy", () => {
    render(<ReservaCard reserva={reserva({ estado: "Expirada", plazaDisponible: false })} ahora={ahora} onAccion={vi.fn()} />);
    expect(boton(/confirmar igualmente/i)).toBeDisabled();
    expect(screen.getByText(/la plaza ya está ocupada/i)).toBeInTheDocument();
  });

  it("RequiresReview_ShowsOnlyMarkAsReviewedAndTheNote", () => {
    render(
      <ReservaCard
        reserva={reserva({ estado: "Pagada", requiereRevision: true, notaInterna: "Pago tras caducar sin plaza" })}
        ahora={ahora}
        onAccion={vi.fn()}
      />,
    );
    expect(screen.getByText(/requiere revisión: pago tras caducar sin plaza/i)).toBeInTheDocument();
    expect(boton(/marcar como revisada/i)).toBeInTheDocument();
    expect(boton(/confirmar/i)).toBeNull();
  });
});
