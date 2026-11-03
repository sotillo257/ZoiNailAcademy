import { ApiError } from "./errors";
import type { SessionClient } from "./session";

/**
 * Downloads a backoffice file (CSV): an `<a href>` would not carry the Bearer token, so the file is fetched with it,
 * turned into a Blob and saved through a temporary object URL.
 */
export async function descargarArchivo(session: SessionClient, path: string, nombrePorDefecto: string): Promise<void> {
  const res = await session.fetch(path, { headers: { Accept: "text/csv" } });
  if (!res.ok) {
    let body: unknown = null;
    try {
      body = await res.json();
    } catch {
      // not JSON
    }
    throw new ApiError(res.status, body);
  }
  const blob = await res.blob();
  const url = URL.createObjectURL(blob);
  try {
    const a = document.createElement("a");
    a.href = url;
    a.download = nombreArchivo(res.headers.get("Content-Disposition")) ?? nombrePorDefecto;
    document.body.appendChild(a);
    a.click();
    a.remove();
  } finally {
    // Give the browser a moment to start the download before revoking.
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  }
}

/** File name of a Content-Disposition header (RFC 6266 `filename*` first). */
export function nombreArchivo(header: string | null): string | undefined {
  if (!header) return undefined;
  const extendido = /filename\*\s*=\s*(?:UTF-8'')?([^;]+)/i.exec(header);
  if (extendido?.[1]) {
    try {
      return decodeURIComponent(extendido[1].trim().replace(/^"|"$/g, ""));
    } catch {
      // fall back to the plain name
    }
  }
  const simple = /filename\s*=\s*("?)([^";]+)\1/i.exec(header);
  return simple?.[2]?.trim();
}
