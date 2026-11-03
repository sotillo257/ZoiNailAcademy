import { describe, expect, it } from "vitest";
import { antiguedad, caducidad, formatearFechaHora, HoraInexistenteError, isoAMadrid, madridAIso } from "./fechas";

describe("madridAIso", () => {
  it("madridAIso_Winter_UsesPlusOne", () => {
    expect(madridAIso({ fecha: "2026-12-05", hora: "10:00" })).toBe("2026-12-05T10:00:00+01:00");
  });

  it("madridAIso_Summer_UsesPlusTwo", () => {
    expect(madridAIso({ fecha: "2026-07-01", hora: "10:00" })).toBe("2026-07-01T10:00:00+02:00");
  });

  // March 2027: clocks go forward on Sunday 28 at 02:00 → 03:00.
  it("madridAIso_DayOfTheMarchChange_UsesTheRightOffsetBeforeAndAfter", () => {
    expect(madridAIso({ fecha: "2027-03-28", hora: "01:30" })).toBe("2027-03-28T01:30:00+01:00");
    expect(madridAIso({ fecha: "2027-03-28", hora: "10:00" })).toBe("2027-03-28T10:00:00+02:00");
    expect(madridAIso({ fecha: "2027-03-27", hora: "10:00" })).toBe("2027-03-27T10:00:00+01:00");
  });

  it("madridAIso_HourSkippedInMarch_Throws", () => {
    expect(() => madridAIso({ fecha: "2027-03-28", hora: "02:30" })).toThrow(HoraInexistenteError);
  });

  // October 2026: clocks go back on Sunday 25 at 03:00 → 02:00.
  it("madridAIso_DayOfTheOctoberChange_UsesTheRightOffsetBeforeAndAfter", () => {
    expect(madridAIso({ fecha: "2026-10-24", hora: "10:00" })).toBe("2026-10-24T10:00:00+02:00");
    expect(madridAIso({ fecha: "2026-10-25", hora: "10:00" })).toBe("2026-10-25T10:00:00+01:00");
    expect(madridAIso({ fecha: "2026-10-26", hora: "10:00" })).toBe("2026-10-26T10:00:00+01:00");
  });

  it("madridAIso_RepeatedOctoberHour_TakesTheFirstOne", () => {
    expect(madridAIso({ fecha: "2026-10-25", hora: "02:30" })).toBe("2026-10-25T02:30:00+02:00");
  });

  it("madridAIso_BadFormat_Throws", () => {
    expect(() => madridAIso({ fecha: "05/12/2026", hora: "10:00" })).toThrow();
  });
});

describe("isoAMadrid", () => {
  it("isoAMadrid_UtcInstants_ReturnMadridWallClockOnBothSidesOfEachChange", () => {
    expect(isoAMadrid("2027-03-28T00:30:00Z")).toEqual({ fecha: "2027-03-28", hora: "01:30" });
    expect(isoAMadrid("2027-03-28T01:30:00Z")).toEqual({ fecha: "2027-03-28", hora: "03:30" });
    expect(isoAMadrid("2026-10-25T00:30:00Z")).toEqual({ fecha: "2026-10-25", hora: "02:30" });
    expect(isoAMadrid("2026-10-25T01:30:00Z")).toEqual({ fecha: "2026-10-25", hora: "02:30" });
    expect(isoAMadrid("2026-10-25T09:00:00Z")).toEqual({ fecha: "2026-10-25", hora: "10:00" });
  });

  it("roundTrip_AnyDayOfTheYear_KeepsTheLocalTime", () => {
    for (let mes = 1; mes <= 12; mes++) {
      const fecha = `2027-${String(mes).padStart(2, "0")}-15`;
      expect(isoAMadrid(madridAIso({ fecha, hora: "18:45" }))).toEqual({ fecha, hora: "18:45" });
    }
  });
});

describe("formato", () => {
  it("formatearFechaHora_ShowsMadridTimeInSpanish", () => {
    // The tests run in America/New_York (vitest.config.ts): the output must still be Madrid.
    expect(formatearFechaHora("2026-12-05T09:00:00+00:00")).toBe("sáb 5 dic 2026, 10:00");
  });

  it("caducidad_BelowSixHours_IsUrgent", () => {
    const ahora = new Date("2026-11-03T10:00:00Z");
    expect(caducidad("2026-11-03T15:00:00Z", ahora)).toEqual({ texto: "caduca en 5 h", urgente: true, caducada: false });
    expect(caducidad("2026-11-03T10:35:00Z", ahora).texto).toBe("caduca en 35 min");
    expect(caducidad("2026-11-04T10:00:00Z", ahora)).toEqual({ texto: "caduca en 24 h", urgente: false, caducada: false });
    expect(caducidad("2026-11-03T09:00:00Z", ahora).caducada).toBe(true);
  });

  it("antiguedad_ShowsDaysAgo", () => {
    expect(antiguedad("2026-10-22T10:00:00Z", new Date("2026-11-03T12:00:00Z"))).toBe("hace 12 días");
  });
});
