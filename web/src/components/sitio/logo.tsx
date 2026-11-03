import { cn } from "@/lib/utils";

/** The circled "Z" of the design. */
export function LogoZ({ className }: { className?: string }) {
  return (
    <span
      aria-hidden
      className={cn(
        "inline-flex size-11 shrink-0 items-center justify-center rounded-full border-[1.5px] border-z-ink bg-white text-lg font-bold text-z-ink",
        className,
      )}
    >
      Z
    </span>
  );
}
