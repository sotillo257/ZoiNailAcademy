import * as React from "react";
import { cn } from "@/lib/utils";

export const campoClases =
  "w-full min-w-0 rounded-lg border border-input bg-card px-3 text-base shadow-xs outline-none transition-colors placeholder:text-muted-foreground focus-visible:border-ring focus-visible:ring-2 focus-visible:ring-ring/30 disabled:cursor-not-allowed disabled:opacity-60 aria-invalid:border-destructive aria-invalid:ring-destructive/20 md:text-sm";

export function Input({ className, type, ...props }: React.ComponentProps<"input">) {
  return <input type={type} data-slot="input" className={cn(campoClases, "h-10", className)} {...props} />;
}

export function Textarea({ className, ...props }: React.ComponentProps<"textarea">) {
  return <textarea data-slot="textarea" className={cn(campoClases, "min-h-20 py-2", className)} {...props} />;
}

/** Native select: the best picker on a phone. */
export function NativeSelect({ className, ...props }: React.ComponentProps<"select">) {
  return <select data-slot="select" className={cn(campoClases, "h-10 pr-8", className)} {...props} />;
}

export function Label({ className, ...props }: React.ComponentProps<"label">) {
  return <label data-slot="label" className={cn("text-sm font-medium leading-none", className)} {...props} />;
}

/** Label + control + help/error, with the error tied to the control through aria-describedby. */
export function Campo({
  id,
  label,
  error,
  ayuda,
  children,
  className,
}: {
  id: string;
  label: React.ReactNode;
  error?: string | undefined;
  ayuda?: React.ReactNode;
  children: (props: { id: string; "aria-invalid": boolean; "aria-describedby": string | undefined }) => React.ReactNode;
  className?: string;
}) {
  const describedBy = [error ? `${id}-error` : null, ayuda ? `${id}-ayuda` : null].filter(Boolean).join(" ") || undefined;
  return (
    <div className={cn("grid gap-1.5", className)}>
      <Label htmlFor={id}>{label}</Label>
      {children({ id, "aria-invalid": Boolean(error), "aria-describedby": describedBy })}
      {ayuda && !error ? (
        <p id={`${id}-ayuda`} className="text-xs text-muted-foreground">
          {ayuda}
        </p>
      ) : null}
      {error ? (
        <p id={`${id}-error`} role="alert" className="text-sm text-destructive">
          {error}
        </p>
      ) : null}
    </div>
  );
}
