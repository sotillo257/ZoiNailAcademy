## ADDED Requirements

### Requirement: Backoffice users are seeded from configuration
At startup, after migrations are applied, the system SHALL ensure that a user exists for `Admin:SeedEmail` (role `Admin`, password `Admin:SeedPassword`, name `Admin:SeedNombre`, default `Admin`) and for `SuperAdmin:SeedEmail` (role `SuperAdmin`, password `SuperAdmin:SeedPassword`, name `SuperAdmin:SeedNombre`, default `SuperAdmin`). Emails MUST be normalized (trimmed, lower-cased). A user is created only when no user with that email exists; an existing user's password, name and role MUST NOT be modified.

#### Scenario: Seeded users can log in
- **WHEN** the application starts with the seed variables set
- **THEN** `POST /api/auth/login` with each seeded email and password returns `200` with the matching `rol`
- **[INTEGRATION]**

#### Scenario: Restarting does not duplicate users nor change passwords
- **WHEN** the application starts a second time against the same database with a different `Admin:SeedPassword`
- **THEN** only one user exists for the Admin email and the original password still logs in, while the new one returns `401`
- **[INTEGRATION]**

### Requirement: Admin password reset through configuration
When `SuperAdmin:ResetAdminPassword` is `true` at startup, the system SHALL set the existing Admin user's password to `Admin:SeedPassword`, revoke all of that user's refresh tokens, and log a warning. When the flag is unset or `false`, no password is changed.

#### Scenario: Reset flag changes the Admin password
- **WHEN** the application starts with `SuperAdmin:ResetAdminPassword=true` and a new `Admin:SeedPassword`
- **THEN** the new password logs in and the previous one returns `401`
- **[INTEGRATION]**

### Requirement: Missing seed configuration fails fast in Production
In the Production environment, the startup MUST fail with an `InvalidOperationException` naming the missing setting when any of `Admin:SeedEmail`, `Admin:SeedPassword`, `SuperAdmin:SeedEmail` or `SuperAdmin:SeedPassword` is missing, or when a seed password is shorter than 10 characters. Outside Production, a user with incomplete settings is skipped with a warning.

#### Scenario: Production without seed variables does not start
- **WHEN** the host starts in Production with `Admin:SeedEmail` empty
- **THEN** startup throws `InvalidOperationException` whose message mentions `Admin:SeedEmail`
- **[INTEGRATION]**
