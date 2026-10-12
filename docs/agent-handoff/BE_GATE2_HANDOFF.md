# KH2 Gate 2 — Backend Integration Handoff

Updated: 2026-10-12. Scope: backend only; no frontend, Face Service, staging, production, migration, seed, deployment, commit, or push was performed.

## Result

Gate 1 remains intact. Gate 2 adds a disposable HTTP integration test that starts the real ASP.NET host against PostgreSQL 16 + pgvector through the existing Testcontainers fixture. It uses synthetic Admin, Pengurus, DewanGuru, three Santri, and three Wali accounts.

The test executes the normal HTTP authentication/authorization pipeline and real EF/PostgreSQL queries; it does not substitute a repository or an in-memory database.

## Verified integration behavior

| Area | Evidence |
| --- | --- |
| Wali child lookup | `GET /api/v1/wali/me/children` returns Wali A's code-valid child; an unlinked Wali receives an empty collection; a stale/mismatched relation does not grant child access. |
| Cross-child isolation | Wali A, Wali B, and an unlinked Wali are denied every guarded Presensi, Kafarah, Log Keluar/Masuk, and Progress Keilmuan read for a child outside their valid scope. |
| Santri isolation | Santri A can read self. Requests that accept a foreign ID are denied; self-scoped Log/Progress calls return only self data, never Santri C data. |
| Staff read scope | Admin, Pengurus, and DewanGuru receive `200 OK` for the exercised staff Santri, Presensi, Kafarah, Log, and Progress routes. |
| Wali writes | Wali receives `403 Forbidden` on exercised Presensi and Kafarah write routes. |
| Canonical enrollment | Five multipart PNG samples return `201 Created`; PostgreSQL contains one active enrollment with exactly five persisted 512-D embeddings. |
| Enrollment state | `GET /api/v1/face-profiles/me/enrollment` returns active and sample count 5. |
| Self verification | Own vector verifies; a vector matching another active Santri returns `verified: false` with `FaceDoesNotBelongToAccount`, without another Santri ID or embedding. |
| Face failure mapping | Invalid image returns `400`; controlled canonical-service unknown face returns `400`; timeout returns `504`; service unavailable returns `503`. |
| No attendance side effect | Presensi row count is unchanged across self-verification cases. |

The controlled face component implements `IFaceRecognitionService` and produces only canonical analysis results (512 dimensions and exact model name/version). It is an isolated backend contract stub, not a live Face Service deployment and not Live Face E2E evidence.

## Final API contract

The authoritative route matrix remains `docs/BE_API_MATRIX.md`; no public request/response contract changed in this Gate 2 work.

- `GET /api/v1/wali/me/children` — Wali only; code-valid persisted child links only.
- `GET /api/v1/presensi?santriId={id}` and `GET /api/v1/kafarah?santriId={id}` — staff or the authorized Santri/Wali scope.
- `GET /api/v1/log-keluar-masuk?santriId={id}` and `GET /api/v1/progress-keilmuan?santriCode={code}` — server-side scope remains authoritative; Santri self scope cannot be overridden by a foreign query value.
- `POST /api/v1/face-profiles/me/enrollment` — Santri, multipart `photos`, exactly five image files; `201` on canonical enrollment.
- `GET /api/v1/face-profiles/me/enrollment` — Santri enrollment state.
- `POST /api/v1/face-profiles/me/verify` — Santri, multipart `photo`; no Presensi write and no foreign biometric identity disclosure.
- Development-only OpenAPI remains `/openapi/v1.json` with `/scalar`; it is not mapped in Staging/Production.

## Schema blocker — multi-child Wali

The Gate 2 test attempted to persist a second child for Wali A with the same Wali code. Real PostgreSQL rejected it with `23505` on `IX_WaliSantriRelations_WaliSantriCode` because `WaliSantriRelationConfiguration` makes `WaliSantriCode` globally unique. Consequently, the requested one-Wali-to-multiple-children production flow cannot be represented by the current schema.

This is recorded as a blocker, not worked around. No schema/configuration/migration/snapshot change was made because the current architecture phase expressly forbids migration work. A separately approved migration/design decision must relax or replace that global uniqueness rule before multi-child Wali can be declared supported.

## Commands and results

| Command | Result |
| --- | --- |
| `graphify --help` | Executed before edits. |
| `graphify update .` (pre-change) | 2,429 nodes, 4,685 edges, 183 communities. |
| Graphify controller/service/test queries | Inspected WaliController, SantriFaceProfileController, authorization readers, canonical face services, PostgreSQL fixture, pgvector tests, and feature controllers. |
| `dotnet restore KH2.ManagementSystem.slnx` | PASS; all projects up to date. |
| `dotnet build KH2.ManagementSystem.slnx --configuration Release --no-restore -p:UseSharedCompilation=false` | PASS; 0 warnings, 0 errors. |
| Targeted Gate 2 HTTP/PostgreSQL test | PASS; 1 test. |
| `dotnet test KH2.ManagementSystem.slnx --configuration Release --no-build --no-restore --filter "Category!=LiveFaceE2E"` | PASS; 185 passed, 0 failed, 0 skipped, 39 s. Live Face E2E is intentionally excluded. |
| `git diff --check` | PASS. |
| `graphify update .` (final) | PASS; 2,481 nodes, 4,798 edges, 183 communities. |

Final Graphify inspection shows the new `Gate2HttpPostgreSqlIntegrationTests` has 20 dependencies, limited to its factory, fixture, synthetic data, assertions, and controlled canonical Face Service. It does not add production controller, schema, or runtime dependencies. WaliController remains connected to its child action; SantriFaceProfileController remains limited to verify/enrollment actions and the existing PostgreSQL test creator.

## Remaining blockers / non-claims

- Multi-child Wali is blocked by the current global unique Wali code constraint.
- Live Face E2E is **not** passed: this run did not call a deployed Face Service, model, camera, or liveness system.
- No new trusted-device attendance HTTP E2E was added in this Gate 2 test; canonical self-verification was explicitly verified to have no attendance side effect.
- No CI provider run was observed; these are local command results only.
