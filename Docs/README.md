# Web de venta de cursos — PRD y specs

Paquete de definición para implementar la web: el PRD, el contrato común de la API y una spec por feature lista para pasar a la IA (OpenSpec o directamente como contexto).

## Contenido

| Archivo | Qué es |
| --- | --- |
| `PRD.md` | Documento de producto: alcance, arquitectura, modelo, flujos, reglas y preguntas abiertas |
| `00-contrato-y-convenciones.md` | Rutas, DTOs, códigos de error, eventos de dominio y convenciones. **Manda sobre las specs** si hay discrepancia |
| `features/F01-base-plantilla.md` | Preparar la plantilla: `/api`, roles, seed, cookie, IP real, rate limiting |
| `features/F02-cursos-y-ediciones.md` | Cursos, ediciones, plazas libres, CRUDs |
| `features/F03-reserva-stripe.md` | Reserva común, Checkout, webhook, idempotencia |
| `features/F04-reserva-manual.md` | Transferencia/Bizum, job de caducidad, confirmar/cancelar/ampliar |
| `features/F05-lista-espera.md` | Lista de espera, consentimiento, CSV |
| `features/F06-emails.md` | Resend, bandeja de salida, plantillas, contacto |
| `features/F07-backoffice-api.md` | Portada, inscritos, pendientes, búsqueda por código |
| `features/F08-front-publico.md` | Next.js público, calendario, flujo de reserva, SEO |
| `features/F09-front-backoffice.md` | Backoffice mobile first, auth en cliente |
| `features/F10-infraestructura.md` | Railway, dominio, DNS, Stripe, CI, variables, checklist de producción |
| `features/F11-aviso-nuevas-ediciones.md` | Extra (regalo): aviso a la lista de espera y baja |
| `pendientes/README.md` | Lo que falta: front, configuración de producción, pruebas manuales y temas con la clienta |

## Orden de trabajo

```
F01 → F02 → F03 → F04 → F05 → F06 → F07 → F11 (si da tiempo)
        ↘ F08 (en paralelo desde F02, con tipos del Swagger)
                       ↘ F09 (desde F04/F05)
F10: primer despliegue a staging tras F01; checklist de producción al final
```

## Cómo usar las specs con la IA

1. Dar siempre como contexto: `00-contrato-y-convenciones.md` + la spec de la feature + la feature Notes de la plantilla como ejemplo de patrón (antes de borrarla en F01, guárdala en una rama).
2. Pedir primero el plan (archivos a crear/modificar y tests) y revisarlo antes de que escriba código.
3. Una feature no está terminada hasta que pasan sus tests de integración y se cumplen sus criterios de aceptación.
4. **F01, F03 y F04 se revisan línea a línea** (auth, dinero y concurrencia). En el resto, revisión normal de PR.
5. Si la IA propone cambiar una ruta o un DTO, se cambia primero el contrato y se regeneran los tipos del front.

## Antes de empezar: preguntas para la clienta y el diseñador

- ¿El precio incluye IVA? ¿Quién emite facturas?
- ¿Cursos presenciales u online? (dirección en los emails)
- ¿Datos bancarios editables desde el backoffice o fijos?
- ¿Cuántas horas antes del inicio se cierran las reservas?
- ¿El Figma incluye las pantallas del flujo de reserva o solo las 2 vistas y contacto?
- Textos legales, textos de los 4 cursos e imágenes.
