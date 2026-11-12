# Blazor Blogs Production Backup and Deployment Plan

**Status:** Completed - Deployment Verified

## 1. Objective

Create a recoverable backup of the existing production Azure App Service deployment,
then deploy the current workspace release with no destructive site cleanup.

## 2. Azure Target

- Subscription: Existing production subscription
- Tenant: Existing production tenant
- Resource group: Existing production resource group
- App Service: Existing production app
- Location: Existing production region
- Current SKU: Production App Service tier
- Current runtime: Windows, .NET 9
- Current endpoint: Existing public production endpoint

## 3. Scope

- Verify the authenticated Azure context and target resource.
- Build and test the current workspace release.
- Capture a local recovery package and a point-in-time copy of production
  application content and configuration required for rollback.
- Confirm the production database is protected independently.
- Publish a self-contained Windows x64 package and deploy it with `--clean false`.
- Verify the production endpoint and application behavior after deployment.
- Excluded: infrastructure replacement, resource-group deletion, database reset,
  schema rollback, and deletion of user content.

## 4. Backup and Rollback

- Preserve the production site package as a timestamped rollback artifact.
- Preserve the live `site\wwwroot` tree, which contains the legacy persistent user
  content because this production app does not define `Storage__UserContentPath`.
- Confirm a database restore point or documented database backup before upload.
- Deploy with ZIP deployment and `--clean false`; if verification fails, redeploy the
  captured rollback package and verify again.

## 5. Deployment Recipe

- Local `dotnet publish` in `Release` mode with the existing
  `deploy/Deploy-Website.ps1` safeguards.
- Azure App Service ZIP deployment to the existing production resource.
- No Bicep or ARM infrastructure modification is planned.

## 6. Validation and Execution Gates

### All validation checks pass

- [x] 1. Core Validation (CLI, auth, build, package safety, target App Service).
- [x] 2. Docker Build (not applicable; the App Service deployment is self-contained).
- [x] 3. Azure Policy Validation.

- [x] Confirm production subscription and App Service target.
- [x] Confirm current app state, runtime, SKU, slots, and backup capabilities.
- [x] Confirm database and persistent content backup status.
- [x] Correct the stale schema-registration test for release `10.06.00`.
- [x] Run tests and build/publish validation.
- [x] Obtain deployment approval.
- [x] Create rollback artifacts and back up production.
- [x] Deploy the current package.
- [x] Verify endpoint availability and application health.
- [x] Record rollback and verification evidence.

## 7. Validation Proof

- 2026-11-11: Azure authentication selected the approved production tenant and
  subscription.
- 2026-11-11: The production App Service is Running; HTTPS-only and Always On are
  enabled.
- 2026-11-11: No deployment slots or configured App Service backup schedules were
  found.
- 2026-11-11: Current production root endpoint returned HTTP 200. The production
  runtime reports .NET 9 and no `Storage__UserContentPath` override, so rollback
  must include the live site files.
- 2026-11-11: Release build succeeded. Tests reported 4/5 passing because the
  schema-registration test still expects `10.05.00` while the app and complete
  `10.06.00` script registration target `10.06.00`.
- 2026-11-11: Updated the schema-registration test to assert version `10.06.00`.
  All 5/5 tests then passed. The Release build completed with 0 errors, and the
  self-contained `win-x64` publish succeeded with the protected-content guard
  finding no uploaded/private files in the package.
- 2026-11-11: Policy review found only the inherited region restriction, MFA write/
  delete enforcement, and Defender for Cloud audit policies; none blocks a West US
  code-only ZIP deployment.
- 2026-11-11: Core validation passed for the code-only recipe: Azure CLI 2.84.0
  authenticated to the approved tenant and subscription, the target App Service was
  resolved and healthy, 5/5 tests passed, Release build completed, and the
  self-contained `win-x64` publish passed the protected-content check. No
  infrastructure template is changed, so ARM/Bicep deployment validation is not
  applicable.
- 2026-11-11: Captured and hash-verified a temporary live-site rollback archive;
  the archive was deleted after deployment.
- 2026-11-11: Verified the production database was Online and exported a
  hash-verified temporary BACPAC; the archive was deleted after deployment.
- 2026-11-11: Deployed the Release self-contained `win-x64` package with
  `--clean false`; the OneDeploy deployment completed successfully.
- 2026-11-11: Restored the legacy connection string into
  `appsettings.Local.json`, applied registered schema scripts `10.00.00` through
  `10.06.00` after the pre-upgrade BACPAC, and verified database version
  `10.06.00`.
- 2026-11-11: Verified the site is Running and `/`, `/api/RSSFeed/Rss`,
  `/Account/Login`, and `/filedownloads` all returned HTTP 200. Database counts
  remained 10 blogs, 4 categories, 1 file, and 666 users.
- 2026-11-11: Restored 2 legacy `wwwroot/files` entries omitted by the incremental
  deployment. Final byte-for-byte checks verified 5 uploads, 174 blog files, and
  2 downloadable files with 0 missing and 0 changed.

## Role Assignment Verification

- Status: Not applicable to this code-only deployment.
- The deployment changes no identities or RBAC assignments. Existing production
  application configuration and database access remain unchanged.
