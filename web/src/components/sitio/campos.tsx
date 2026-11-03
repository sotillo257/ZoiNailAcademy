import * as React from "react";
import { cn } from "@/lib/utils";

export const claseControl =
  "block w-full rounded-md border text-left border-[#d9d3d2] bg-white px-4 text-[15px] text-z-ink placeholder:text-[#a8a2a1] " +
  "focus:border-z-burgundy focus:outline-none focus:ring-2 focus:ring-z-burgundy/20 aria-[invalid=true]:border-z-burgundy";

/** Label + control + error, wired with aria-describedby. */
export function Campo({
  id,
  label,
  error,
  className,
  children,
}: {
  id: string;
  label: string;
  error?: string;
  className?: string;
  children: (props: { id: string; "aria-invalid": boolean; "aria-describedby"?: string }) => React.ReactNode;
}) {
  const errorId = `${id}-error`;
  return (
    <div className={className}>
      <label htmlFor={id} className="mb-1.5 block text-xs font-bold text-z-ink">
        {label}
      </label>
      {children({ id, "aria-invalid": Boolean(error), "aria-describedby": error ? errorId : undefined })}
      {error ? (
        <p id={errorId} className="mt-1 text-xs font-medium text-z-burgundy">
          {error}
        </p>
      ) : null}
    </div>
  );
}

/** Checkbox with its text and error. */
export function Casilla({
  id,
  error,
  children,
  ...props
}: Omit<React.ComponentProps<"input">, "type" | "id"> & { id: string; error?: string; children: React.ReactNode }) {
  const errorId = `${id}-error`;
  return (
    <div>
      <div className="flex items-start gap-3">
        <input
          id={id}
          type="checkbox"
          aria-invalid={Boolean(error)}
          aria-describedby={error ? errorId : undefined}
          className="mt-0.5 size-[18px] shrink-0 cursor-pointer rounded-sm border border-[#b9b3b2] accent-z-burgundy"
          {...props}
        />
        <label htmlFor={id} className="cursor-pointer text-xs leading-relaxed text-z-text">
          {children}
        </label>
      </div>
      {error ? (
        <p id={errorId} className="mt-1 text-xs font-medium text-z-burgundy">
          {error}
        </p>
      ) : null}
    </div>
  );
}

/** Field only bots fill: off screen (never display:none), out of the tab order and autocomplete. */
export const Honeypot = React.forwardRef<HTMLInputElement, React.ComponentProps<"input">>(function Honeypot(props, ref) {
  return (
    <div aria-hidden className="absolute -left-[9999px] top-auto h-px w-px overflow-hidden">
      <label>
        Web
        <input ref={ref} type="text" tabIndex={-1} autoComplete="off" {...props} />
      </label>
    </div>
  );
});

export function AvisoError({ children, className }: { children: React.ReactNode; className?: string }) {
  return (
    <div role="alert" className={cn("rounded-md border border-z-burgundy/30 bg-z-red-soft px-4 py-3 text-sm text-z-burgundy", className)}>
      {children}
    </div>
  );
}
