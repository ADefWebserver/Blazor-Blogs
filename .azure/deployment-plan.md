# Blazor Blogs Azure Upgrade Verification Plan

**Status:** Completed - Verification Passed and Resources Deleted

## 1. Objective

Deploy a previous released version of Blazor Blogs to a temporary, isolated Azure
App Service and Azure SQL environment in the approved test subscription, deploy
the current workspace version over it, verify the upgrade preserves application
data and behavior, and then delete the dedicated temporary resource group.

## 2. Scope

- Prepare temporary Azure infrastructure with Infrastructure as Code.
- Deploy the prior released GitHub version.
- Initialize version `09.00.00`, create an administrator and a uniquely named
  upgrade-verification post/category.
- Deploy the current workspace version in place.
- Authenticate as administrator, run the guided upgrade, and verify database
  version `10.05.00`, preserved login/content, and core application routes.
- Delete only the dedicated temporary resource group after verification.
- Excluded: Stripe payment execution and Google login.

## 3. Azure Context

- Subscription: redacted.
- Region: West US 3 (`westus3`).
- Isolation: Dedicated temporary resource group.
- Destructive scope explicitly requested by user: delete the temporary website
  and its resource group after verification.

## 4. Architecture

- Azure App Service plan: B1, Linux or Windows-compatible deployment for the
  Blazor Server app, HTTPS-only, TLS 1.2+, Always On, no client affinity
  requirement.
- Azure SQL logical server and Basic database with Entra-only authentication.
- User-assigned managed identity used by the App Service and configured as the
  SQL Entra administrator. No SQL administrator login/password will be
  generated.
- Connection string uses `Active Directory Managed Identity` and the identity
  client ID.
- Persistent user content under `%HOME%\data\BlazorBlogs`; ZIP deployment keeps
  existing files.
- Bicep under `infra/upgrade-test/`; resource naming includes a test suffix.

## 5. Analysis and Decisions

- Previous version: GitHub release/tag `09.00.00`, targeting .NET 9.
- Current version: workspace version `10.05.00`, targeting .NET 11.
- Upgrade path: current install wizard executes scripts `10.00.00.sql` through
  `10.05.00.sql` in order against the existing `09.00.00` database.
- Deployment recipe: Bicep infrastructure plus local `dotnet publish` ZIP
  deployment with Azure CLI; each App Service deployment is validated before
  code deployment.
- Policy review: only the subscription's Defender for Cloud audit initiative is
  assigned; no deny policy blocks the proposed resources.
- Region/quota: B1 is available in West US 3; Azure SQL Basic is `Available` there.
  App Service plan usage reports no hard limit. SQL capability output confirms
  provisioning availability.

## 5.1 Verification Procedure

1. Export and build tag `09.00.00` without modifying the current checkout.
2. Provision temporary Bicep infrastructure with a deployment what-if preview.
3. Deploy the `09.00.00` publish ZIP.
4. Complete initial schema setup and create the administrator through the
   supported install wizard.
5. Create a uniquely named post/category and confirm schema version `09.00.00`.
6. Deploy the current workspace publish ZIP to the same App Service without
   deleting existing user content.
7. Sign in, follow `/installwizard/UPGRADE`, and complete the guided migration.
8. Confirm schema version `10.05.00` and preservation of the administrator,
   post, category, profile, settings, RSS, and download surfaces.
9. Confirm temporary resources in the dedicated resource group, then delete that
   resource group and verify it no longer exists.

## 6. Execution Gates

1. Obtain user approval before provisioning.
2. Generate and validate infrastructure.
3. Invoke `azure-validate` and record proof in this plan.
4. Change plan status only when validation completes.
5. Deploy prior version, create verification data, and verify baseline.
6. Deploy current version in place and verify upgrade.
7. Delete only the dedicated temporary resource group and verify deletion.

### All validation checks pass

- [x] 1. Core Validation: CLI, auth, Bicep build, deployment validation, and
  resource-group what-if.
- [x] 2. Bicep linting.
- [x] 3. Azure Policy validation.

## Role Assignment Verification

- Status: Verified.
- Identities checked: `user-assigned managed identity` assigned to the App
  Service and configured as the Azure SQL server Entra administrator.
- Roles confirmed: Azure SQL Entra administrator grants the identity the required
  server/database data-plane access; no storage, Key Vault, or control-plane
  runtime access is required by this design.
- Issues: None. The signed-in deployment principal has `Owner` at the target
  subscription.

## 7. Validation Proof

- 2026-11-11: `Bicep-build_bicep` completed successfully for
  `infra/upgrade-test/main.bicep`.
- 2026-11-11: `Bicep-build_bicepparam` completed successfully for
  `baseline.bicepparam` and `current.bicepparam`.
- 2026-11-11: release `09.00.00` published successfully to a temporary ZIP
  package from an isolated `git archive`.
- 2026-11-11: current workspace version `10.05.00` published successfully to a
  temporary ZIP package; publish output contains no private user content.
- 2026-11-11: `dotnet test ... --no-restore` passed 5/5 tests.
- 2026-11-11: `dotnet build BlazorBlogs.sln -c Release --no-restore`
  succeeded with 0 warnings and 0 errors.
- 2026-11-11: subscription policy review found only the Defender for Cloud audit
  initiative and no blocking deny assignment.
- 2026-11-11: App Service B1 and Azure SQL Basic are available in `westus3`;
  resource provider registrations are present and App Service quota is
  unrestricted.
- 2026-11-11: `validate-deployment.ps1` completed with `OVERALL: PASS`;
  what-if reported 12 creates, 0 modifies, and 0 deletes.
- 2026-11-11: `az bicep lint` completed with no lint errors.
- 2026-11-11: static role review passed; the managed identity is the Entra-only
  SQL administrator and the deployment principal has subscription `Owner`.
- 2026-11-11: baseline infrastructure deployment completed successfully in a
  dedicated temporary resource group.
- 2026-11-11: release `09.00.00` initialized successfully. The administrator,
  post `Azure Upgrade Baseline 09.00.00`, category
  `Upgrade Baseline Category`, and post/category association were persisted.
- 2026-11-11: current infrastructure deployment and .NET 11 runtime update
  completed successfully. The initial framework-dependent package returned
  500.31 because the worker did not provide the .NET 11 shared runtime; a
  self-contained `win-x64` package recovered the deployment on the same site.
- 2026-11-11: guided upgrade completed from schema version `09.00.00` to
  `10.05.00`.
- 2026-11-11: post content, category/filter assignment, administrator login,
  settings, administration routes, downloads view, purchases view, logs,
  account pages, and RSS all passed after upgrade. Every checked route returned
  HTTP 200; Stripe payment execution and Google login were not exercised.
- 2026-11-11: root health endpoint returned HTTP 200. `/health` and `/alive`
  return 404 in the current production configuration, so root-route health was
  used for the final verification.
- 2026-11-11: live identity verification passed. The web app's user-assigned
  identity principal ID exactly matched the Azure SQL Entra administrator SID,
  Entra-only authentication was enabled, and no unrelated Azure RBAC roles were
  required.
- 2026-11-11: the dedicated temporary resource group was deleted successfully;
  `az group exists` returned `false`.

### Live Role Verification

- Status: Pass.
- Identity: temporary managed identity name redacted.
- SQL administrator SID matched the web app identity principal ID exactly.
- Azure SQL Entra-only authentication: Enabled.
- Unrelated Azure RBAC assignments: 0 at the SQL server scope.
