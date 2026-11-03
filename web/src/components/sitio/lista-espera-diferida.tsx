"use client";

import dynamic from "next/dynamic";
import * as React from "react";
import type { OpcionCurso } from "./lista-espera-form";

// The form (react-hook-form + zod) is the heaviest JS of Formaciones and sits at the bottom: it is loaded only when
// its section gets close to the screen (or straight away when the page is opened at #lista-espera).
const ListaEsperaForm = dynamic(() => import("./lista-espera-form").then((m) => m.ListaEsperaForm), {
  ssr: false,
  loading: () => <Reserva />,
});

function Reserva() {
  return <div aria-hidden className="min-h-[540px]" />;
}

export function ListaEsperaDiferida(props: {
  cursos: OpcionCurso[];
  cursoInicial?: string;
  edicionPreferidaId?: string;
  edicionPreferidaTexto?: string;
}) {
  const ref = React.useRef<HTMLDivElement>(null);
  const [visible, setVisible] = React.useState(false);

  React.useEffect(() => {
    const el = ref.current;
    if (!el) return;
    if (!("IntersectionObserver" in window)) {
      setVisible(true);
      return;
    }
    const observer = new IntersectionObserver(
      (entradas) => {
        if (entradas.some((e) => e.isIntersecting)) {
          setVisible(true);
          observer.disconnect();
        }
      },
      { rootMargin: "600px 0px" },
    );
    observer.observe(el);
    return () => observer.disconnect();
  }, []);

  return <div ref={ref}>{visible ? <ListaEsperaForm {...props} /> : <Reserva />}</div>;
}
