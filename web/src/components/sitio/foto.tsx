import Image from "next/image";
import { CameraIcon } from "lucide-react";
import { cn } from "@/lib/utils";

/**
 * A photo of the design. Until the client sends it (`src` null) it shows the design's placeholder: a warm grey box
 * with what goes there.
 */
export function Foto({
  src,
  alt,
  className,
  sizes = "(min-width: 768px) 50vw, 100vw",
  icono = false,
  nota,
  prioridad = false,
}: {
  src: string | null;
  alt: string;
  className?: string;
  sizes?: string;
  icono?: boolean;
  nota?: string;
  prioridad?: boolean;
}) {
  if (src) {
    return (
      <div className={cn("relative overflow-hidden rounded-lg bg-z-photo", className)}>
        <Image src={src} alt={alt} fill sizes={sizes} className="object-cover" priority={prioridad} />
      </div>
    );
  }
  return (
    <div
      role="img"
      aria-label={alt}
      className={cn("flex flex-col items-center justify-center gap-1 rounded-lg bg-z-photo p-4 text-center", className)}
    >
      {icono ? <CameraIcon className="mb-2 size-7 text-[#6b6766]" strokeWidth={1.5} aria-hidden /> : null}
      <span className="text-xs font-semibold text-[#4d4948]">{alt}</span>
      {nota ? <span className="text-[11px] text-[#5f5a59]">{nota}</span> : null}
    </div>
  );
}
