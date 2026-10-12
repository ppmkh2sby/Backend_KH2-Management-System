# Backend remediation completion report

## Scope completed

Baseline branch/HEAD: `agent/face-attendance-deployment` / `99aab32`. No migration was created or applied and no external, staging, or production system was changed.

- Added `GET /api/v1/wali/me/children`, restricted to `WaliSantri`. It returns only persisted relations whose `WaliSantriCode` still agrees with the authenticated account username. A missing or stale relation returns an empty `items` array and is never guessed.
- Separated Presensi and Kafarah read permissions from existing KTB write permission. Admin, Pengurus, and DewanGuru can monitor all rows; a Santri can read own rows; a Wali must select an authorized linked child. Existing write operations remain KTB Santri only.
- Added linked-child read behavior for logs using `santriId`; Wali write operations remain denied.
- Removed the Progress Wali first-child fallback. A Wali must provide an authorized `santriCode`; unauthorized and nonexistent selections remain distinguishable only through existing guarded behavior.
- Removed dashboard reader fallbacks that could select an arbitrary Santri or return a stub. The `dashboard/santri/me*` reader is self-Santri only; Wali must select a child and use guarded child/read routes.
- Documented frozen route and role contracts in `docs/BE_API_MATRIX.md` and `docs/BE_ROLE_ACCESS_MATRIX.md`.

## Files changed

- `src/KH2.ManagementSystem.Api/Controllers/WaliController.cs`
- `src/KH2.ManagementSystem.Api/Controllers/PresensiController.cs`
- `src/KH2.ManagementSystem.Api/Controllers/KafarahController.cs`
- `src/KH2.ManagementSystem.Api/Controllers/LogKeluarMasukController.cs`
- `src/KH2.ManagementSystem.Api/Controllers/ProgressKeilmuanController.cs`
- `src/KH2.ManagementSystem.Api/Contracts/LogKeluarMasuk/LogKeluarMasukQueryRequest.cs`
- `src/KH2.ManagementSystem.Api/Contracts/ProgressKeilmuan/ProgressKeilmuanQueryRequest.cs`
- `src/KH2.ManagementSystem.Infrastructure/Dashboard/SantriDashboardReader.cs`
- `src/KH2.ManagementSystem.Api/Program.cs`
- `tests/KH2.ManagementSystem.UnitTests/WaliChildrenControllerTests.cs`
- `docs/BE_API_MATRIX.md`, `docs/BE_ROLE_ACCESS_MATRIX.md`

## Verification

- `dotnet restore KH2.ManagementSystem.slnx -p:NuGetAudit=false`: passed. This was necessary only because the previously restored lock/assets recorded `Microsoft.OpenApi 2.0.0` advisory `NU1903` as an error; no package version was changed.
- `dotnet build KH2.ManagementSystem.slnx --no-restore`: passed, 0 warnings and 0 errors.
- `dotnet test ... --filter FullyQualifiedName~WaliChildrenControllerTests`: passed, 1/1. It used a disposable PostgreSQL Testcontainers instance and proves valid Wali links are returned while a stale relation code is excluded.
- Full `dotnet test` was started after build. The local runner ended without emitting its final test summary, so it is **inconclusive**, not reported as a pass. Full suite must be rerun in CI/local with its complete result retained. Some existing face host E2E tests require `KH2_FACE_E2E_FIXTURES_DIR` and are expected to need an explicit approved local fixture.

## Remaining risks / release gates

- Wali dashboard does not have a new aggregate endpoint; the safe behavior is no arbitrary dashboard result. FE must use the child selector then guarded read routes.
- The role/read changes need broader host HTTP tests covering each status code and Presensi/Kafarah pagination/recap scopes before application readiness can be granted.
- Biometric production remains blocked pending model license, threshold calibration, consent/privacy, liveness/manual-fallback approval. This backend change does not enable production biometrics.
- `APP_READY`: not approved. `BIOMETRIC_READY`: blocked. `PROD_INFRA_READY`: not assessed here. `CUTOVER_APPROVED`: no.

## Later human-approved deployment / rollback

No schema operation is required for this change. After approval, deploy the immutable application image through staging, run role-negative API tests with synthetic accounts, then promote. Roll back by restoring the prior application image; no database rollback is needed for these source-only changes.
