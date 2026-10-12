# Backend remediation completion report

Date: 2026-10-12
Working branch / audited HEAD: `agent/face-attendance-deployment` / `c3012dc24df38bc0fcd6fd520d692e4818f6f28f`

## Completed changes

- Preserved the existing Wali-child and role-scoped PostgreSQL queries: Wali only sees code-valid persisted relations; staff can read their allowed management data; Santri remains self-only. Presensi, Kafarah, Progress Keilmuan, and Log Keluar/Masuk retain their valid team write restrictions.
- Corrected canonical enrollment status: `isEnrolled` is now true only when the caller has an active profile, an active enrollment, and exactly the configured five canonical embedding rows.
- Preserved self-verification through `RecognizeFaceImage`, Face Service, and pgvector. The endpoint returns no embedding or competing identity and creates no Presensi record.
- Replaced the vulnerable transitive `Microsoft.OpenApi 2.0.0` with an explicit compatible `Microsoft.OpenApi 2.7.5` reference. Normal NuGet audit now passes; audit was not disabled.
- Implemented native .NET 10 OpenAPI and Scalar only for `Development`: `/openapi/v1.json` and `/scalar`. They are unmapped outside Development.
- Set sample Staging/Production configuration to `LegacyFaceApi:Enabled=false`; database migration and seed flags remain false. This is source configuration only—no environment was changed.
- Added the API contract matrix and Frontend/Face handoff. Legacy C2 behavior remains intact: disabled legacy requests authenticate/authorize first, then receive `410 Gone`/`Deprecation: true` before legacy controller activation.

## Files changed in this remediation pass

- `src/KH2.ManagementSystem.Api/KH2.ManagementSystem.Api.csproj`
- `src/KH2.ManagementSystem.Api/Program.cs`
- `src/KH2.ManagementSystem.Api/Controllers/SantriFaceProfileController.cs`
- `src/KH2.ManagementSystem.Api/appsettings.Staging.example.json`
- `src/KH2.ManagementSystem.Api/appsettings.Production.example.json`
- `tests/KH2.ManagementSystem.UnitTests/PostgreSqlPgvectorIntegrationTests.cs`
- `tests/KH2.ManagementSystem.UnitTests/LegacyFaceApiHostStartupTests.cs`
- `docs/BE_API_MATRIX.md`
- `docs/agent-handoff/BE_CONTRACT_HANDOFF.md`
- `BE_COMPLETION_REPORT.md`

## Actual verification

| Command | Result |
| --- | --- |
| `dotnet restore KH2.ManagementSystem.slnx` | PASS, normal NuGet audit enabled |
| `dotnet list KH2.ManagementSystem.slnx package --include-transitive` | PASS; resolved `Microsoft.OpenApi 2.7.5` |
| `dotnet build src/KH2.ManagementSystem.Api/KH2.ManagementSystem.Api.csproj --no-restore --verbosity:minimal` | PASS, 0 warnings / 0 errors |
| `dotnet build tests/KH2.ManagementSystem.UnitTests/KH2.ManagementSystem.UnitTests.csproj --no-restore --verbosity:minimal` | PASS, 0 warnings / 0 errors |
| `dotnet test ... --filter FullyQualifiedName~PostgreSqlPgvectorIntegrationTests` | PASS, 5/5 on disposable PostgreSQL 16 + pgvector Testcontainers |
| `dotnet test ... --filter FullyQualifiedName~LegacyFaceApiHostStartupTests` | PASS, 10/10; includes 410 gate, auth-negative tests, disabled-startup proof, and Development-only OpenAPI/Scalar proof |
| `dotnet test KH2.ManagementSystem.slnx --no-build --no-restore` | BLOCKED: 184/185 passed; the only failure requires the absent `KH2_FACE_E2E_FIXTURES_DIR` live-face fixture |

The full solution build now passes. The complete test suite was executed and its sole failure is an external live-face fixture prerequisite, not an asserted application regression. CI should provide approved fixtures only when running that E2E test and retain the complete log.

## Scope and release status

No migration was generated, edited, or applied. No Supabase, staging, or production database was written; no operational account data was seeded; no deployment or commit was performed.

`APP_READY`: not approved pending complete CI suite and broader HTTP scope coverage.

`BIOMETRIC_READY`: blocked pending model license, threshold calibration, consent/privacy, liveness/manual-fallback review, and explicit approval.

`PROD_INFRA_READY`: not assessed.
`CUTOVER_APPROVED`: no.

## Remaining blockers / follow-up

- Run the complete solution test suite, including API contract and negative authorization coverage for every scoped resource, in CI with retained final output.
- Existing face host E2E tests that require `KH2_FACE_E2E_FIXTURES_DIR` still need explicitly approved local fixtures; this pass did not fabricate fixtures.
- Do not begin C3 legacy runtime removal. Follow C2 operational cutover: deploy telemetry with legacy enabled, migrate known callers, observe usage, communicate sunset, explicitly disable, observe 410 callers, then obtain approval.

## Gate 1 — backend CI stabilization (2026-10-12)

HEAD audited for this gate: `d5ab3dbfd982f4b1077e5d5f41684d954f01caad` on `agent/face-attendance-deployment`.

The prior GitHub Actions failure was environmental, not an application assertion: `CanonicalFaceHostE2ETests.HostEnrollmentPersistsFiveRealEmbeddingsRollsBackInvalidCaptureAndMatchesProbe` needs approved private image fixtures through `KH2_FACE_E2E_FIXTURES_DIR` and a live Face Service at `127.0.0.1:8000`. The shared Ubuntu PR runner does not provide either prerequisite.

The test is retained and now carries `[Trait("Category", "LiveFaceE2E")]`. Standard CI runs `Category!=LiveFaceE2E`, retaining all 184 non-live tests, including PostgreSQL/pgvector Testcontainers, authorization-negative host tests, mock Face Service contract tests, and Development-only OpenAPI/Scalar tests. The separate `Backend Live Face E2E` workflow is manually dispatched only onto a labelled self-hosted trusted runner; it checks the private fixture directory, Face Service health, and Docker before it runs `Category=LiveFaceE2E`.

Local Gate 1 evidence:

| Command | Status |
| --- | --- |
| `dotnet restore KH2.ManagementSystem.slnx` | `LOCAL_PASS` — normal NuGet audit enabled |
| `dotnet build KH2.ManagementSystem.slnx --configuration Release --no-restore` | `LOCAL_PASS` — 0 warnings, 0 errors |
| `dotnet test ... --filter "Category!=LiveFaceE2E"` | `LOCAL_PASS` — 184 passed, 0 failed, 0 skipped |
| `dotnet test ... --filter "Category=LiveFaceE2E"` without fixture | `BLOCKED_EXTERNAL_FIXTURE` — one test fails explicitly with the required fixture-variable message |

GitHub Actions has not been dispatched by this change, therefore `GITHUB_CI_PASS` is not claimed. `STANDARD_CI_GATE` is locally validated but awaits GitHub execution; `LIVE_FACE_E2E_GATE` is blocked pending the approved trusted runner, model, service, and private fixture; `PRODUCTION_APPROVED` remains `NO`.

Graphify evidence: `graphify --help`, `graphify update .` before and after the change, and `graphify explain` for the Live Face E2E and legacy host test nodes all completed. The post-change graph contains 2,421 nodes and 4,678 edges; no application/API/legacy dependency was added by the test trait or workflow split.
