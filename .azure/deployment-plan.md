# Blazor Help Website 10.06.00 Deployment Plan

**Status:** Completed - Deployment Verified

## 1. Objective

Deploy the validated Blazor Blogs 10.06.00 release to the existing production
Blazor Help Website Azure App Service without changing infrastructure or deleting
user content.

## 2. Target

- Existing production App Service named by the user.
- Existing production subscription and resource group.
- Code-only Windows App Service deployment.
- Release package: self-contained .NET 11 `win-x64`.

## 3. Scope

- Confirm the App Service exists, is running, and is a Blazor Blogs installation.
- Build and test the current `main` branch.
- Capture and verify a temporary live-site rollback archive.
- Export and verify a temporary database BACPAC before schema changes.
- Preserve the existing database connection as `appsettings.Local.json` when
  required.
- Deploy with `--clean false` and reject protected content from the publish package.
- Apply only schema scripts newer than the site's stored database version.
- Verify public routes, schema version, and protected-content hashes.
- Delete all temporary archives and deployment artifacts after success.

## 4. Exclusions

- No infrastructure, App Service plan, slot, custom-domain, database-tier, or
  resource-group changes.
- No resource deletion or destructive database rollback.
- No subscription IDs, tenant details, credentials, connection strings, or backup
  files in source control.

## 5. Rollback

If verification fails, redeploy the captured pre-deployment site archive with
`--clean false`. Database changes are forward-only, so no schema update will begin
until the temporary BACPAC is verified.

## 6. Deployment Recipe

- Recipe type: Azure CLI, code-only App Service ZIP deployment.
- Build: Release tests, build, and self-contained Windows x64 publish.
- Upload: `az webapp deploy --type zip --clean false`.

## 7. Validation Gates

### All validation checks pass

- [x] 1. Core Validation (CLI, authentication, build, tests, package safety).
- [x] 2. Docker Build (not applicable).
- [x] 3. Azure Policy Validation.

- [x] Confirm the target App Service and current runtime.
- [x] Confirm the current public endpoint is healthy.
- [x] Receive deployment approval.
- [x] Create and verify temporary site and database backups.
- [x] Deploy, preserve connection configuration, and upgrade schema.
- [x] Verify endpoint and protected-content integrity.
- [x] Delete temporary artifacts and record sanitized proof.

## 8. Validation Proof

- 2026-11-12: The target App Service is Running on Windows PremiumV3 in the
  production region, uses .NET 9, has no deployment slots, and its root endpoint
  returned HTTP 200.
- 2026-11-12: All 5/5 automated tests passed, and the Release build completed with
  0 errors.
- 2026-11-12: Policy review found only audit, region-restriction, and MFA
  enforcement policies; none blocks an in-region code-only ZIP deployment.
- 2026-11-12: Self-contained `win-x64` publish succeeded and contained no
  protected user-content files.
- 2026-11-12: Captured and verified temporary pre-deployment site and database
  rollback archives before any schema change.
- 2026-11-12: OneDeploy completed successfully with `--clean false`; the existing
  connection configuration was preserved.
- 2026-11-12: Applied only pending upgrade scripts and verified database schema
  version `10.06.00`.
- 2026-11-12: Verified 3,560 protected files with 0 missing and 0 changed after
  restoring 56 legacy files omitted by incremental deployment.
- 2026-11-12: The root, RSS, login, and downloads routes all returned HTTP 200.
- 2026-11-12: All temporary site archives, database BACPACs, publish output, and
  deployment packages were deleted after verification.

## Role Assignment Verification

- Status: Not applicable to this code-only deployment.
- No identities or RBAC assignments are changed.
