"use client";

import { EyeIcon, EyeOffIcon, LoaderCircleIcon } from "lucide-react";
import { useRouter, useSearchParams } from "next/navigation";
import * as React from "react";
import { useAuth } from "@/components/admin/auth-provider";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Campo, Input } from "@/components/ui/input";
import { isApiError } from "@/lib/admin/errors";
import { destinoSeguro } from "@/lib/admin/next";

export default function LoginPage() {
  return (
    <main className="flex min-h-dvh items-center justify-center bg-muted/40 px-4 py-10">
      <div className="w-full max-w-sm">
        <div className="mb-6 text-center">
          <p className="text-xl font-semibold tracking-tight text-primary">Zoi Nail Academy</p>
          <p className="text-sm text-muted-foreground">Backoffice</p>
        </div>
        <React.Suspense fallback={<Card className="h-72" />}>
          <LoginForm />
        </React.Suspense>
      </div>
    </main>
  );
}

function LoginForm() {
  const { login } = useAuth();
  const router = useRouter();
  const params = useSearchParams();
  const [email, setEmail] = React.useState("");
  const [password, setPassword] = React.useState("");
  const [verPassword, setVerPassword] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const [enviando, setEnviando] = React.useState(false);

  const enviar = async (e: React.FormEvent) => {
    e.preventDefault();
    if (enviando) return;
    setError(null);
    setEnviando(true);
    try {
      await login(email.trim(), password);
      router.replace(destinoSeguro(params.get("next")));
    } catch (err) {
      setError(
        isApiError(err, 429)
          ? "Demasiados intentos. Espera unos minutos y vuelve a probar."
          : isApiError(err, 401) || isApiError(err, 400)
            ? "Email o contraseña incorrectos."
            : "No hay conexión con el servidor. Inténtalo de nuevo.",
      );
      setEnviando(false);
    }
  };

  return (
    <Card className="p-5">
      <h1 className="mb-4 text-lg font-semibold">Entrar</h1>
      <form onSubmit={enviar} className="grid gap-4" noValidate>
        <Campo id="email" label="Email">
          {(props) => (
            <Input
              {...props}
              type="email"
              autoComplete="username"
              inputMode="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />
          )}
        </Campo>
        <Campo id="password" label="Contraseña">
          {(props) => (
            <div className="relative">
              <Input
                {...props}
                type={verPassword ? "text" : "password"}
                autoComplete="current-password"
                required
                className="pr-11"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />
              <button
                type="button"
                onClick={() => setVerPassword((v) => !v)}
                className="absolute inset-y-0 right-0 flex w-11 items-center justify-center text-muted-foreground hover:text-foreground"
                aria-label={verPassword ? "Ocultar contraseña" : "Mostrar contraseña"}
                aria-pressed={verPassword}
              >
                {verPassword ? <EyeOffIcon className="size-4" /> : <EyeIcon className="size-4" />}
              </button>
            </div>
          )}
        </Campo>
        {error ? (
          <p role="alert" className="rounded-lg bg-destructive-soft px-3 py-2 text-sm text-destructive">
            {error}
          </p>
        ) : null}
        <Button type="submit" size="lg" disabled={enviando || !email || !password}>
          {enviando ? <LoaderCircleIcon className="animate-spin" /> : null}
          Entrar
        </Button>
      </form>
    </Card>
  );
}
