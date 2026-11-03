## ADDED Requirements

### Requirement: CSV for Excel in Spanish
`CsvBuilder` SHALL produce UTF-8 with a BOM, `;` as separator and CRLF line endings. A field containing `;`, `"`, CR or LF MUST be wrapped in quotes with inner quotes doubled. A field starting with `=`, `+`, `-`, `@`, tab or CR MUST get a leading `'` so spreadsheets do not evaluate it as a formula. Every backoffice CSV export (F05, F07) MUST use it.

#### Scenario: Escaping
- **WHEN** fields contain quotes, line breaks, a `;`, or start with `=`, `+`, `-` or `@`
- **THEN** they are quoted and/or prefixed with `'` as described, and plain fields are unchanged
- **[UNIT]**

#### Scenario: Encoding
- **WHEN** a CSV is turned into bytes
- **THEN** it starts with `EF BB BF` and accents are UTF-8
- **[UNIT]**
