/** A ProblemDetails (RFC 7807) answer of the API, with the contract's business code in `code`. */
export class ApiError extends Error {
  readonly status: number;
  readonly code: string | undefined;
  readonly detail: string | undefined;
  /** Validation errors by field (FluentValidation property names). */
  readonly errors: Record<string, string[]>;
  /** Every other member of the ProblemDetails (e.g. `ocupadas`, `reservas`). */
  readonly extensions: Record<string, unknown>;

  constructor(status: number, body: unknown) {
    const problem = isRecord(body) ? body : {};
    const detail = typeof problem.detail === "string" ? problem.detail : undefined;
    const title = typeof problem.title === "string" ? problem.title : undefined;
    super(detail ?? title ?? `Error ${status}`);
    this.name = "ApiError";
    this.status = status;
    this.code = typeof problem.code === "string" ? problem.code : undefined;
    this.detail = detail;
    this.errors = isRecord(problem.errors) ? toErrors(problem.errors) : {};
    this.extensions = problem;
  }

  /** First validation message of a field, matched case-insensitively (the API sends `Plazas`, the form `plazas`). */
  fieldError(field: string): string | undefined {
    const key = Object.keys(this.errors).find((k) => k.toLowerCase() === field.toLowerCase());
    return key ? this.errors[key]?.[0] : undefined;
  }

  numberExtension(name: string): number | undefined {
    const value = this.extensions[name];
    return typeof value === "number" ? value : undefined;
  }
}

/** The session is gone (refresh failed): the user has been sent to the login. */
export class SessionExpiredError extends Error {
  constructor() {
    super("La sesión ha caducado.");
    this.name = "SessionExpiredError";
  }
}

export function isApiError(error: unknown, status?: number, code?: string): error is ApiError {
  return (
    error instanceof ApiError &&
    (status === undefined || error.status === status) &&
    (code === undefined || error.code === code)
  );
}

/** A Spanish message for any error, for toasts and banners. */
export function mensajeError(error: unknown): string {
  if (error instanceof SessionExpiredError) return error.message;
  if (error instanceof ApiError) {
    if (error.status === 403) return "No tienes permiso para hacer esto.";
    if (error.status === 429) return "Demasiados intentos, espera unos minutos.";
    if (error.status >= 500) return "Ha fallado el servidor. Inténtalo de nuevo en un momento.";
    const firstField = Object.values(error.errors)[0]?.[0];
    return error.detail ?? firstField ?? error.message;
  }
  return "No hay conexión con el servidor. Comprueba la red e inténtalo de nuevo.";
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function toErrors(value: Record<string, unknown>): Record<string, string[]> {
  const result: Record<string, string[]> = {};
  for (const [key, messages] of Object.entries(value)) {
    if (Array.isArray(messages)) result[key] = messages.filter((m): m is string => typeof m === "string");
  }
  return result;
}
