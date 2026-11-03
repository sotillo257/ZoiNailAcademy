# F02 — Cursos y ediciones

**Revisión línea a línea:** No (salvo el cálculo de plazas libres, que se reutiliza en F03/F04).
**Depende de:** F01.

## Objetivo

Modelar cursos y ediciones, exponerlos a la web pública con sus plazas libres, y dar a la clienta el CRUD de ediciones y al desarrollador el CRUD de cursos.

## Alcance

**Incluye**
- Entidades `Curso` y `Edicion`, configuración EF y migración.
- Seed de los 4 cursos iniciales (datos en un JSON del repo, editable).
- Endpoints públicos de lectura.
- CRUD de ediciones (`Admin`) y de cursos (`SuperAdmin`).
- Servicio de dominio `PlazasCalculator` (cálculo de plazas libres), reutilizado por F03, F04 y F07.

**No incluye**
- La entidad `Reserva` (F03). Hasta F03, las plazas libres = plazas totales. El cálculo se diseña ya contando reservas para no rehacerlo.
- Subida de imágenes (van en `web/public/`).

## Modelo de datos

### Curso
| Campo | Tipo | Reglas |
| --- | --- | --- |
| `Id` | Guid | PK |
| `Nombre` | string(120) | Obligatorio |
| `Slug` | string(120) | Obligatorio, único, `^[a-z0-9]+(-[a-z0-9]+)*$` |
| `DescripcionCorta` | string(300) | Obligatorio |
| `Descripcion` | text | Markdown. El front lo renderiza saneado |
| `Precio` | decimal(10,2) | > 0. IVA: pendiente de confirmar (pregunta abierta del PRD) |
| `Imagen` | string(300) | Ruta relativa en el front, p. ej. `/cursos/acrilico.jpg` |
| `Ubicacion` | string(300)? | Dirección o "Online". Se usa en emails (pregunta abierta del PRD) |
| `DuracionTexto` | string(100)? | P. ej. "2 días, 10:00–18:00". Informativo |
| `Activo` | bool | Solo activos se muestran |
| `Orden` | int | Orden en la web |
| `CreadoEn`, `ActualizadoEn` | DateTimeOffset | |

### Edicion
| Campo | Tipo | Reglas |
| --- | --- | --- |
| `Id` | Guid | PK |
| `CursoId` | Guid | FK, `OnDelete: Restrict` |
| `Inicio` | DateTimeOffset | UTC |
| `Plazas` | int | 1–200 |
| `Estado` | enum | `Programada`, `Oculta`, `Cancelada` (string) |
| `Notas` | string(500)? | Interno, solo backoffice |
| `xmin` | uint | Token de concurrencia (`UseXminAsConcurrencyToken` o `IsRowVersion`) |
| `CreadoEn`, `ActualizadoEn` | DateTimeOffset | |

Índice: `(CursoId, Inicio)`.

## Reglas

### Edición reservable
Una edición es **reservable** si:
- `Estado == Programada`, y
- el curso está activo, y
- `Inicio - ahora > Reservas__CierreHorasAntes` horas.

Una edición es **visible en la web** si es reservable (aunque esté completa: se muestra como "Completo" con botón a lista de espera).

### Plazas libres (`PlazasCalculator`)
```
ocupadas = count(reservas donde EdicionId = X y (Estado = Pagada o (Estado = Pendiente y ExpiraEn > ahora)))
libres   = max(0, Plazas - ocupadas)
```
- Se calcula en la consulta (subquery/`GroupBy`), nunca con N+1.
- Nunca se guarda un contador en la tabla.
- Estado público: `Completo` si `libres == 0`; `UltimasPlazas` si `libres <= Reservas__UmbralUltimasPlazas` (por defecto 3); si no, `Disponible`.

### CRUD de ediciones (Admin)
- **Crear:** curso activo; `Inicio` en el futuro; `Plazas` 1–200; `Estado` por defecto `Programada`. Si nace `Programada` → publica `EdicionPublicadaEvent`.
- **Editar:** requiere el `version` (`xmin`) leído; si no coincide → 409 `concurrencia`.
  - `Plazas` no puede bajar de `ocupadas` → 409 `plazas_por_debajo_de_ocupadas` con `extensions.ocupadas`.
  - Cambiar `Inicio` con reservas pagadas o pendientes vigentes requiere `confirmarCambioFecha: true`; si no viene → 409 `cambio_fecha_requiere_confirmacion` con `extensions.reservas` (número). El front muestra el aviso «Esta edición tiene X inscritos, tendrás que avisarles» y reenvía con la confirmación.
  - Pasar de `Oculta` a `Programada` → publica `EdicionPublicadaEvent`.
  - Pasar a `Cancelada` con reservas → permitido, pero la respuesta incluye `avisos: ["Tiene X inscritos: avisa y reembolsa manualmente"]`. Las pendientes de esa edición pasan a `Cancelada` (lo hará F04; en F02 dejar un TODO con el evento).
  - Una edición `Cancelada` no se puede reactivar (409 `estado_invalido`). Se crea otra.
  - No se puede editar una edición cuyo `Inicio` ya pasó (solo `Notas`).
- **Borrar:** solo si no tiene ninguna reserva (de ningún estado) → si tiene, 409 `edicion_con_reservas`.

### CRUD de cursos (SuperAdmin)
- `Slug` único; si se cambia, avisar en la respuesta (rompe URLs y SEO).
- **Borrar** = desactivar (`Activo = false`). Si tiene ediciones futuras `Programada` → 409 `curso_con_ediciones`.
- El precio cambiado no afecta a reservas ya creadas (la reserva guarda su `Importe`).

## Endpoints

### Públicos (sin auth)

`GET /api/cursos` → `CursoResumenDto[]` de cursos activos ordenados por `Orden`, con `proximaEdicion` (la primera edición visible con plazas libres, o la primera visible si todas están completas, o `null`).

`GET /api/cursos/{slug}` → `CursoDetalleDto` con `ediciones` visibles ordenadas por `Inicio`. 404 si no existe o está inactivo.

`GET /api/cursos/{slug}/ediciones` → `EdicionPublicaDto[]`. Cabecera `Cache-Control: no-store` (el calendario siempre fresco).

### Backoffice

`GET /api/admin/cursos` (Backoffice) → `{id, nombre, slug, activo}[]` para selectores.

`GET /api/admin/ediciones?cursoId=&incluirPasadas=false` →
```ts
interface EdicionAdminDto {
  id: string; cursoId: string; curso: string; inicio: string;
  plazas: number; pagadas: number; pendientes: number; libres: number;
  estado: 'Programada' | 'Oculta' | 'Cancelada';
  reservable: boolean; notas: string | null; version: string;
}
```
Orden: próximas primero; pasadas al final si `incluirPasadas=true`.

`GET /api/admin/ediciones/{id}` → `EdicionAdminDto`.

`POST /api/admin/ediciones` body `{cursoId, inicio, plazas, estado?, notas?}` → 201 `EdicionAdminDto`.

`PUT /api/admin/ediciones/{id}` body `{inicio, plazas, estado, notas?, version, confirmarCambioFecha?}` → 200 `{edicion: EdicionAdminDto, avisos: string[]}`.

`DELETE /api/admin/ediciones/{id}` → 204.

`POST/PUT/DELETE /api/admin/cursos[/{id}]` (SuperAdmin) con todos los campos de `Curso` salvo fechas.

## Seed de cursos

- `src/Plantilla.Infrastructure/Data/Seed/cursos.json` con los 4 cursos.
- Se insertan si no existe un curso con ese `Slug`. No se sobrescriben (el SuperAdmin puede editarlos después).
- Los textos los aporta la clienta; mientras tanto, placeholders marcados con `[PENDIENTE]`.

## Configuración

`Reservas__CierreHorasAntes` (24), `Reservas__UmbralUltimasPlazas` (3), `App__TimeZone` (Europe/Madrid).

## Tests

**Unitarios**
- `PlazasCalculator`: sin reservas; con pagadas; con pendientes vigentes; con pendientes caducadas (no cuentan); con expiradas y canceladas (no cuentan); nunca negativo.
- Reglas de reservable: estado, curso inactivo, dentro del cierre, ya empezada (usar `FakeTimeProvider`).
- Validadores de crear/editar edición y curso.

**Integración**
- Público: solo cursos activos; solo ediciones visibles; estados `Disponible/UltimasPlazas/Completo` correctos (insertando reservas directamente en BD una vez exista F03, o con datos sembrados en el test).
- Editar con `version` antiguo → 409 `concurrencia`.
- Bajar plazas por debajo de ocupadas → 409.
- Cambiar fecha con reservas sin confirmar → 409; con confirmar → 200.
- Borrar con reservas → 409; sin reservas → 204.
- `EdicionPublicadaEvent` se publica al crear `Programada` y al pasar de `Oculta` a `Programada`, y no en otros casos (handler de prueba registrado en el test).
- Admin no puede crear cursos (403); SuperAdmin sí.

## Criterios de aceptación

- [ ] Los 4 cursos aparecen en `GET /api/cursos` tras el primer arranque.
- [ ] La clienta puede crear, editar, ocultar, cancelar y borrar (sin reservas) ediciones.
- [ ] Ninguna consulta de listado hace N+1 (revisar el SQL generado en los logs).
- [ ] Tipos TypeScript regenerados desde Swagger.
