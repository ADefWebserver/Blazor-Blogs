# Feature Test Matrix

This matrix covers the feature inventory advertised in the README. Use synthetic
users, posts, email addresses, files, and provider values. Never record passwords,
tokens, connection strings, personal data, real cloud resource identifiers, or
production hostnames/IP addresses.

Statuses:

- **Pass**: evidence exists for the exact expected result.
- **Automated**: deterministic coverage runs in CI; use the latest CI result as evidence.
- **Blocked**: the owner-controlled external prerequisite is unavailable.
- **Pending**: a local/manual scenario still needs execution.

## Issue features

| ID | Area | Level | Preconditions / scenario | Expected result | Owner | Bucket | Status and evidence |
|---|---|---|---|---|---|---|---|
| I55 | Social metadata | Unit and source/raw render | Build posts with summaries, HTML, Unicode, missing data, safe and unsafe image URLs | Complete normalized canonical, Open Graph, and X/Twitter metadata; 200-code-point bound; safe image fallback; 404 has no stale post metadata | Implementing developer | T08 | **Automated** — `SocialPreviewBuilderTests` |
| I49 | Optional site search | Unit and settings/browser | Missing/disabled/enabled setting; spaces, delimiters, Unicode, existing `site:` text, JavaScript disabled | Absent while disabled; enabled native GET flow has one trusted encoded `site:` restriction and does not mutate terms | Implementing developer | T08 | **Automated** — `GoogleSiteSearchServiceTests`; browser raw-GET evidence pending |
| I43-1 | Theme package validation | Unit/storage | Valid package plus unsafe paths, links, executable types, signatures, CSS, sizes, entry limits, and duplicates | Valid static package imports inactive; every invalid package leaves no package row/final folder; no executable or remote CSS is accepted | Implementing developer | T08 | **Automated** — `ThemePackageValidatorTests`, `ThemePackageServiceTests` |
| I43-2 | Theme lifecycle and authorization | Unit/source/browser | Administrator and anonymous contexts; import, preview, activate, deactivate, restart, missing asset, delete, active delete | Only administrators mutate; preview is temporary/noindex; lifecycle is persistent; missing package falls back; active package cannot be deleted | Implementing developer | T08 | **Automated** — `ThemePackageServiceTests`, `ThemeSystemSourceTests`; responsive browser evidence pending |

## Install, content, and local development

| ID | Area | Level | Preconditions / scenario | Expected result | Owner | Bucket | Status and evidence |
|---|---|---|---|---|---|---|---|
| C01 | Fresh install | Manual integration | Throwaway SQL database, no settings, administrator fixture | Wizard runs scripts in order, creates required rows, and lands on the usable site | Implementing developer | T09 | Pending |
| C02 | Upgrade from 10.06.00 | Integration | Database with 10.06.00 schema and synthetic settings/user content | 10.07.00 and 10.08.00 run once, version ends at 10.08.00, data remains intact | Implementing developer | T09 | Pending; schema registration is **Automated** in `SchemaRegistrationTests` |
| C03 | Upgrade retry/failure | Integration | Deliberately interrupt one synthetic schema execution | Retry is safe; later scripts do not run until the failed script succeeds; no setting/content loss | Implementing developer | T09 | Pending |
| C04 | Blogs and paging | Browser | Anonymous session plus synthetic posts/category | List, read, previous/next, category filtering, and article navigation work | Implementing developer | T08 | Pending |
| C05 | Blog administration | Browser | Blogger and administrator fixtures | Authorized create/edit/delete/image upload work; unauthorized edit/delete is rejected | Implementing developer | T08 | Pending |
| C06 | Categories and tags | Browser/integration | Synthetic posts with categories/tags | Assignment and filtering render correct posts and preserve relationships | Implementing developer | T08 | Pending |
| C07 | Rich editor | Browser | Synthetic post, summary, header, newsletter, and local image | Text, formatting, source round trip, image upload, and labels work by keyboard | Implementing developer | T08 | Pending |
| C08 | DNN import | Integration/browser | Sanitized fixture database/export | Imported rich content, metadata, and relationships are retained | Implementing developer | T08 | Pending |
| C09 | Live Writer/MetaWeblog | Integration | Synthetic local endpoint client and blog fixture | Endpoint responds and publish/edit/delete round trip preserves content | Implementing developer | T08 | Pending |
| C10 | RSS | Raw HTTP | Synthetic posts and feed request | Feed parses and includes expected titles, links, dates, and content | Implementing developer | T08 | Pending |
| C11 | Logs | Unit/browser | Generate a synthetic log entry; anonymous and administrator sessions | Administrator can read logs; anonymous caller cannot access them | Implementing developer | T08 | Pending |
| C12 | User content | Unit/publish/integration | Default/custom roots, seeded legacy files, restart, two clean publishes | Missing files copy without overwrite; restart preserves them; protected uploads/blogs/themes/downloads are absent from publish | Implementing developer | T08/T09 | **Automated** — user-content/schema/publish tests; restart copy scenario pending |
| C13 | Local Aspire | CLI integration | Docker-compatible runtime; clean named local environment | Fresh AppHost starts, SQL data survives restart, publish/deploy graph checks succeed | Implementing developer | T09 | Pending |

## Identity, integrations, commerce, and messaging

| ID | Area | Level | Preconditions / scenario | Expected result | Owner | Bucket | Status and evidence |
|---|---|---|---|---|---|---|---|
| A01 | Registration and verification | Browser/integration | Synthetic local users; confirmation boundaries | Register, verification, login, logout, recovery, and lockout states behave as configured | Implementing developer | T09 | Pending |
| A02 | Roles and administration | Browser | User, blogger, and administrator fixtures | Role checks consistently gate settings, mutation, and account administration | Implementing developer | T09 | Pending |
| A03 | Google sign-in settings | Unit/browser | Disabled and synthetic invalid/configured options | Disabled/configured state is explicit; canceled/failed provider flows do not create a false login | Repository owner | T10 | Blocked — approved test provider account/callback URL required |
| A04 | Newsletter preferences | Browser/integration | Synthetic users and local storage | Subscribe, preference changes, confirm boundaries, unsubscribe, and resubscribe persist correctly | Implementing developer | T09 | Pending |
| A05 | Newsletter delivery | Unit/integration | Fake SMTP; resumable synthetic campaign | Resume/retry state is correct; SMTP failure is visible; generated headers and links contain only synthetic values | Implementing developer | T09 | Pending |
| A06 | SMTP delivery | Manual external | Owner-managed test mailbox and disposable SMTP account | Test message arrives and failure behavior is explicit | Repository owner | T10 | Blocked — test-only SMTP access required |
| A07 | Disqus and analytics | Browser | Disabled and synthetic provider settings; block outbound requests | Disabled state makes no provider request; configured state emits the documented public values only | Implementing developer | T08 | Pending |
| D01 | Free/guest downloads | Browser/integration | Synthetic free and protected files | Guest and signed-in access follows policy; protected physical paths are unavailable | Implementing developer | T09 | Pending |
| D02 | Paid-download state | Unit/integration | Fake Stripe gateway, synthetic checkout/webhook/refund/replay | Authorization, purchase history, replay defense, refund, expiry/limit, and resend states are correct | Implementing developer | T09 | Pending |
| D03 | Stripe end to end | Manual external | Owner-managed Stripe test mode, webhook endpoint, synthetic buyer | Checkout, cancel, webhook, purchase, resend, refund, and failure states pass in test mode | Repository owner | T10 | Blocked — Stripe test account and approved test endpoint required |

## Branding, accessibility, and cloud

| ID | Area | Level | Preconditions / scenario | Expected result | Owner | Bucket | Status and evidence |
|---|---|---|---|---|---|---|---|
| B01 | Branding and settings | Browser | Synthetic name/logo/header/accent/site URL | Settings persist; logo/header/accent/base URL render correctly and invalid base URL is rejected | Implementing developer | T08 | Pending |
| B02 | Theme responsive browser | Browser | Valid test package; 375 px and 1440 px viewports | Preview/activate/deactivate do not hide critical controls, clip text, or block keyboard navigation | Implementing developer | T08 | Pending |
| B03 | Accessibility | Browser | Keyboard-only pass, zoom, labels, focus; automated accessibility scanner if available | All controls have names; focus is visible; zoom is usable; no critical findings | Implementing developer | T08 | Pending |
| X01 | Azure provisioning/deployment | Manual external | Repository-owner-approved throwaway resource group/subscription access | Provision, first deploy, update, persistent content, and rollback package flow succeed | Repository owner | T10 | Blocked — explicit throwaway Azure access/approval required |
| X02 | Azure rollback/recovery | Manual external | Existing throwaway deployment plus prior clean package | Previous package restores cleanly without deleting persistent uploads/blogs/themes/downloads or changing the database | Repository owner | T10 | Blocked — depends on X01 |

## Execution record

For each run, append a concise, non-sensitive evidence line:

```text
YYYY-MM-DD | IDs | runner/owner | result | evidence location | exception link (if any)
```

Do not put command output containing connection strings, account identifiers, email
addresses, tokens, real site names, resource groups, subscription IDs, or IP addresses
in this file. External rows remain **Blocked**, not Pass, until the repository owner
supplies controlled test access or records a dated scope exception.
