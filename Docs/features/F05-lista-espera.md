# F05 — Lista de espera

**Revisión línea a línea:** No.
**Depende de:** F01, F02.

## Objetivo

Captar a los interesados en un curso (sin fecha o con la edición completa) para que la clienta les llame cuando abra fechas, y darle a ella un listado por curso con marca de "contactado" y exportación.

## Alcance

**Incluye**
- Entidad `ListaEspera`.
- `POST /api/lista-espera` (público).
- Listado, marca de contactado y CSV en el backoffice.
- Publicación de `ListaEsperaAltaEvent` (el email a la clienta lo envía F06).

**No incluye**
- El aviso automático de nuevas ediciones (F11), aunque el modelo ya deja preparados el token de baja y el consentimiento.

## Modelo de datos

### ListaEspera
| Campo | Tipo | Reglas |
| --- | --- | --- |
| `Id` | Guid | PK |
| `CursoId` | Guid | FK, `Restrict` |
| `EdicionPreferidaId` | Guid? | FK opcional a una edición (si viene de una edición completa) |
| `FechaPreferida` | string(200)? | Texto libre ("fines de semana", "en marzo") |
| `Nombre` | string(120) | Obligatorio |
| `Email` | string(200) | Obligatorio, normalizado |
| `Telefono` | string(20) | Obligatorio, E.164 (mismo normalizador que F03) |
| `ConsentimientoEn` | DateTimeOffset | Momento en que marcó la casilla |
| `TextoConsentimiento` | string(500) | Copia literal del texto aceptado (prueba de consentimiento RGPD) |
| `Contactado` | bool | |
| `ContactadoEn` | DateTimeOffset? | |
| `Notas` | string(500)? | Interno |
| `BajaToken` | string(64) | Aleatorio (32 bytes en base64url), único |
| `BajaEn` | DateTimeOffset? | |
| `CreadoEn`, `ActualizadoEn` | DateTimeOffset | |

Índice único: `(CursoId, Email)`.

## Reglas

- **Sin duplicados:** mismo `Email` + `CursoId` → se **actualiza** el registro existente (nombre, teléfono, fecha preferida, consentimiento) y se pone `Contactado = false`, `BajaEn = null`. Respuesta 201 igual que un alta nueva (no revelar si ya existía).
- El curso debe existir y estar activo → si no, 404.
- `EdicionPreferidaId`, si viene, debe pertenecer al curso → si no, 400.
- `aceptaPrivacidad` debe ser `true`. El texto de la casilla (en el front y guardado en `TextoConsentimiento`) **debe cubrir también el aviso por email de nuevas fechas**, para que F11 no necesite pedir consentimiento otra vez. Texto propuesto:
  > Acepto la política de privacidad y que me contactéis por teléfono, WhatsApp o email para avisarme cuando haya nuevas fechas de este curso.
- Honeypot `website` como en F03.
- Rate limit: política `formularios`.
- Publicar `ListaEsperaAltaEvent` tanto en alta nueva como en actualización.

## Endpoints

### `POST /api/lista-espera`
```json
{ "cursoId": "…", "edicionPreferidaId": null, "fechaPreferida": "Sábados de marzo",
  "nombre": "…", "email": "…", "telefono": "…",
  "aceptaPrivacidad": true, "textoConsentimiento": "…", "website": "" }
```
→ 201 sin body.

`textoConsentimiento` lo envía el front; el backend valida que no esté vacío. Alternativa más robusta: el backend guarda su propia constante versionada (`ConsentimientoListaEspera.V1`) e ignora lo que mande el front. **Usar la alternativa.**

### `GET /api/admin/lista-espera?cursoId=&contactado=&q=&page=&pageSize=`
```ts
interface ListaEsperaAdminDto {
  id: string; cursoId: string; curso: string;
  edicionPreferida: string | null;   // fecha formateada si la hay
  fechaPreferida: string | null;
  nombre: string; email: string; telefono: string;
  contactado: boolean; contactadoEn: string | null;
  baja: boolean; notas: string | null; creadoEn: string;
}
```
- `q` busca en nombre, email y teléfono (contiene, sin distinguir mayúsculas ni acentos: `unaccent` + `ILIKE`, o `EF.Functions.ILike` sobre columnas normalizadas).
- Orden: no contactados primero, después por `CreadoEn` ascendente (los que más esperan, arriba).
- Paginado (pageSize por defecto 50, máximo 200). Respuesta `{ items, total, page, pageSize }`.

### `PUT /api/admin/lista-espera/{id}/contactado`
Body `{ contactado: boolean, notas?: string }` → 200 `ListaEsperaAdminDto`. Pone o quita `ContactadoEn`.

### `GET /api/admin/lista-espera.csv?cursoId=`
- `text/csv; charset=utf-8` con BOM (para que Excel abra bien los acentos), separador `;` (Excel en español).
- Columnas: Curso; Nombre; Teléfono; Email; Fecha preferida; Edición preferida; Contactado; Fecha de alta; Notas; Baja.
- Fechas en hora de Madrid `dd/MM/yyyy HH:mm`.
- Escapar comillas y prevenir inyección de fórmulas: si un campo empieza por `=`, `+`, `-` o `@`, anteponer `'`.
- Nombre de archivo: `lista-espera-{slug}-{yyyyMMdd}.csv`.

> El helper de CSV (BOM, `;`, escapado, anti-fórmulas) se crea aquí como utilidad compartida y lo reutiliza F07.

## Tests

**Unitarios**
- Validador: teléfono obligatorio y válido, email válido, privacidad aceptada.
- Helper CSV: comillas, saltos de línea, `;` dentro de un campo, prefijos de fórmula, BOM.

**Integración**
- Alta nueva → 201 y evento publicado.
- Misma persona y curso → actualiza (no duplica) y reinicia `Contactado`.
- Mismo email en otro curso → registro nuevo.
- Curso inactivo → 404. Honeypot → no guarda.
- Listado con filtros y búsqueda con acentos (`Jose` encuentra `José`).
- Marcar y desmarcar contactado.
- CSV con BOM, `;` y fechas en hora de Madrid.
- Sin token → 401.

## Criterios de aceptación

- [ ] La clienta ve la lista por curso, con los no contactados arriba, y puede marcarlos.
- [ ] El CSV se abre en Excel con acentos correctos y columnas separadas.
- [ ] El consentimiento guardado cubre el aviso de nuevas fechas (requisito de F11).
