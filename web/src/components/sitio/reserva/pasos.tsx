import { CheckIcon } from "lucide-react";
import { cn } from "@/lib/utils";

/** "1 Tus datos —— 2 Pago" stepper of the design. */
export function Pasos({ actual }: { actual: 1 | 2 }) {
  const hecho = actual === 2;
  return (
    <ol className="flex items-center gap-2.5 text-[13px] font-bold" aria-label="Pasos de la reserva">
      <li className="flex items-center gap-2" aria-current={actual === 1 ? "step" : undefined}>
        <span
          className={cn(
            "inline-flex size-[26px] items-center justify-center rounded-full text-xs text-white",
            hecho ? "bg-z-green" : "bg-z-burgundy",
          )}
        >
          {hecho ? <CheckIcon className="size-3.5" strokeWidth={3} aria-label="Hecho" /> : "1"}
        </span>
        Tus datos
      </li>
      <li aria-hidden className={cn("h-0.5 flex-1 rounded", hecho ? "bg-z-burgundy" : "bg-[#dcd6d5]")} />
      <li className="flex items-center gap-2" aria-current={actual === 2 ? "step" : undefined}>
        <span
          className={cn(
            "inline-flex size-[26px] items-center justify-center rounded-full text-xs",
            actual === 2 ? "bg-z-burgundy text-white" : "border-[1.5px] border-[#b9b3b2] bg-white text-z-ink",
          )}
        >
          2
        </span>
        Pago
      </li>
    </ol>
  );
}

export function Tarjeta({ className, children }: { className?: string; children: React.ReactNode }) {
  return <div className={cn("rounded-[10px] border border-z-line bg-white p-5", className)}>{children}</div>;
}
