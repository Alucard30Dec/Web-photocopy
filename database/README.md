# WebPhotocopyHub PostgreSQL TKS Canonical Database

Apply order:

1. Run `database/bootstrap/001_create_webphotocopyhub_database.sql` from the `postgres` maintenance database.
2. Connect to `"WebPhotocopyHub"`.
3. Run `database/patches/V20260704_001_tks_canonical_webphotocopyhub.sql`.
4. If migrating existing app data, run `database/patches/V20260704_002_migrate_app_schema_to_tks_canonical.sql` after the current `app`, `system`, `audit`, and Identity tables exist in `"WebPhotocopyHub"`.

Example local connection string:

```text
Host=localhost;Port=5432;Database=WebPhotocopyHub;Username=postgres;Application Name=WebPhotocopyHub
```

Use user-secrets or an environment variable for the real password.

Local helper:

```powershell
powershell -ExecutionPolicy Bypass -File .\database\apply_webphotocopyhub.ps1
```

To clone the existing local app database first, then add canonical TKS objects and migrate data:

```powershell
powershell -ExecutionPolicy Bypass -File .\database\apply_webphotocopyhub.ps1 -SourceDatabase DTBwebphotocopyhub -MigrateExistingAppSchema
```

Add `-MigrateExistingAppSchema` after the current `app`, `system`, `audit`, and Identity tables exist in `"WebPhotocopyHub"`.
