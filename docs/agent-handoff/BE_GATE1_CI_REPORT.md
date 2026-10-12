# Gate 1 — Backend CI stabilization

Date: 2026-10-12

## Scope and root cause

Audited HEAD: `d5ab3dbfd982f4b1077e5d5f41684d954f01caad` on `agent/face-attendance-deployment`.

The previous standard workflow ran all 185 tests on `ubuntu-latest`. One test failed before exercising its assertions because `CanonicalFaceHostE2ETests.HostEnrollmentPersistsFiveRealEmbeddingsRollsBackInvalidCaptureAndMatchesProbe` requires both:

- an approved private image fixture directory in `KH2_FACE_E2E_FIXTURES_DIR`; and
- a live Face Service at `http://127.0.0.1:8000` with the compatible model.

The GitHub-hosted standard runner has neither. This is an external-fixture/runner-configuration failure, not a unit or integration code regression.

## Test classification

| Category | Standard CI disposition | Evidence |
| --- | --- | --- |
| Unit | Run | Domain, seed, matching, health, and service-validation tests have no live fixture requirement. |
| Integration | Run | PostgreSQL 16 + pgvector Testcontainers tests run on Docker-capable `ubuntu-latest`. |
| Contract | Run | Face Service contract tests use controlled handlers/stubs. |
| Security | Run | Authorization-negative and legacy-gate host tests remain in the standard filter. |
| LiveFaceE2E | Separate gate | `CanonicalFaceHostE2ETests` is marked `[Trait("Category", "LiveFaceE2E")]`; it uses real image bytes, a real Face Service, Testcontainers, enrollment, probe matching, device authentication, duplicate attendance, audit persistence, and rollback. |

The filter `Category!=LiveFaceE2E` excludes only the marked live test. It does not skip PostgreSQL, host, contract, or authorization tests.

## Workflows

### Standard CI: `.github/workflows/dotnet.yml`

Runs on pushes and pull requests to `main` using `global.json`:

1. checks Docker availability for Testcontainers;
2. restores the entire solution with normal NuGet security audit;
3. builds Release with existing warning/error policy;
4. runs `Category!=LiveFaceE2E` and stores a TRX artifact.

### Live Face E2E: `.github/workflows/face-e2e.yml`

Manual only. It requires an explicit `RUN_LIVE_FACE_E2E` confirmation and a labelled self-hosted `linux` / `face-e2e` runner. Before tests it verifies the fixture environment variable and directory, at least one image, private Face Service liveness, and Docker. It then builds Release and runs only `Category=LiveFaceE2E`, publishing a TRX result. No biometric image, path value, or credential is printed. It is not triggered by untrusted pull requests.

## Validation evidence

| Command | Result |
| --- | --- |
| `graphify --help` | PASS; Graphify 0.9.43 command surface inspected |
| `graphify update .` before the CI change | PASS; 2,410 nodes / 4,668 edges |
| `graphify update .` after the CI change | PASS; 2,421 nodes / 4,678 edges |
| `graphify explain "CanonicalFaceHostE2ETests"` and `graphify explain "LegacyFaceApiHostStartupTests"` | PASS; live E2E remains isolated to its test/host/Face dependencies while authorization and OpenAPI host tests remain standard consumers |
| `dotnet restore KH2.ManagementSystem.slnx` | `LOCAL_PASS` |
| `dotnet build KH2.ManagementSystem.slnx --configuration Release --no-restore` | `LOCAL_PASS`, 0 warnings / 0 errors |
| `dotnet test KH2.ManagementSystem.slnx --configuration Release --no-build --filter "Category!=LiveFaceE2E"` | `LOCAL_PASS`, 184 passed / 0 failed / 0 skipped |
| `dotnet test KH2.ManagementSystem.slnx --configuration Release --no-build --filter "Category=LiveFaceE2E"` | `BLOCKED_EXTERNAL_FIXTURE`; 0 passed / 1 failed with the explicit missing-fixture prerequisite |

`Microsoft.OpenApi` resolves at version `2.7.5`; no `NuGetAudit=false` setting was introduced.

## Gate status

- `STANDARD_CI_GATE`: `LOCAL_PASS`; `GITHUB_CI_PASS` is not claimed until the workflow runs in GitHub Actions.
- `LIVE_FACE_E2E_GATE`: `BLOCKED_EXTERNAL_FIXTURE` until an approved trusted runner provisions the service, compatible model, and private fixture.
- `PRODUCTION_APPROVED`: `NO`. License, threshold calibration, consent/privacy, liveness/manual fallback, and operational approval remain independent blockers.

## Safety

No migration, database write, Supabase operation, deployment, commit, push, or secret/fixture publication was performed.
