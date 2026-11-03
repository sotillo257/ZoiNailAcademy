# notes Specification

## Purpose

Example feature shipped with the template. Describes per-user CRUD of notes and demonstrates the end-to-end pattern (entity with invariants → command/query + handler + validator → controller → unit and integration tests). Remove this spec together with the feature once the project has real features.

## Requirements

### Requirement: Note invariants

A `Note` SHALL belong to exactly one user (`UserId`) and MUST have a non-blank title, stored trimmed. Title is at most 200 characters and content at most 4000 characters; content MAY be empty.

#### Scenario: Blank title is rejected by the entity
- **WHEN** a `Note` is created or updated with an empty or whitespace-only title
- **THEN** a `DomainException` is thrown and the note state is unchanged
- **[UNIT]**

#### Scenario: Title is trimmed
- **WHEN** a `Note` is created with a title surrounded by whitespace
- **THEN** the stored title has no surrounding whitespace
- **[UNIT]**

### Requirement: Create note

The system SHALL expose `POST /notes` accepting `{ title, content }` and return `201` with the created note `{ id, title, content, createdAt, updatedAt }` and a `Location` header pointing to `/notes/{id}`. The note is owned by the authenticated user.

#### Scenario: Valid note is created
- **WHEN** an authenticated user posts a valid title and content
- **THEN** the response is `201` with the note body and a `Location` header ending in `/notes/{id}`
- **[INTEGRATION]**

#### Scenario: Invalid payload is rejected
- **WHEN** the title is empty or exceeds 200 characters, or the content exceeds 4000 characters
- **THEN** the response is `400` `ValidationProblemDetails` with an error for the offending field
- **[UNIT]** (validator) and **[INTEGRATION]** (empty title)

#### Scenario: Anonymous request is rejected
- **WHEN** `POST /notes` is called without a Bearer token
- **THEN** the response is `401`
- **[INTEGRATION]**

### Requirement: List notes

The system SHALL expose `GET /notes?page={page}&pageSize={pageSize}` returning `200` with `{ items, page, pageSize, totalCount }` containing only the authenticated user's notes, newest first. `page` defaults to 1 and MUST be ≥ 1; `pageSize` defaults to 20 and MUST be between 1 and 100.

#### Scenario: Only own notes, newest first, paged
- **WHEN** a user with three notes requests page 1 with page size 2 while another user also has notes
- **THEN** the response contains the user's two newest notes and `totalCount` is 3
- **[UNIT]** and **[INTEGRATION]**

#### Scenario: Invalid paging is rejected
- **WHEN** `page` < 1 or `pageSize` is outside 1..100
- **THEN** the response is `400` `ValidationProblemDetails`
- **[UNIT]** and **[INTEGRATION]**

### Requirement: Get, update and delete a note

The system SHALL expose `GET /notes/{id}` (`200` with the note), `PUT /notes/{id}` accepting `{ title, content }` (`200` with the updated note, `updatedAt` set), and `DELETE /notes/{id}` (`204`). A note that does not exist or belongs to another user MUST return `404` in `application/problem+json`, without revealing that it exists.

#### Scenario: Owner reads, updates and deletes a note
- **WHEN** the owner gets, updates, and then deletes a note
- **THEN** the responses are `200`, `200` with the new values and non-null `updatedAt`, and `204`; a subsequent get returns `404`
- **[INTEGRATION]**

#### Scenario: Another user's note is not found
- **WHEN** a user gets, updates, or deletes a note owned by someone else
- **THEN** the response is `404` and the note is not modified
- **[UNIT]** and **[INTEGRATION]**
