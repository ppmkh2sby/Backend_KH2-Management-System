# KH2 Gate 2 — Shared Integration Protocol

Date: 2026-10-12. Three repositories, three independent agent tabs.

## Baseline (verify current HEAD; never reset newer work)

| Owner | Repository | Last observed branch/HEAD |
|---|---|---|
| BE | `ppmkh2sby/Backend_KH2-Management-System` | `agent/face-attendance-deployment` @ `8b465a65c022de8c6544796417c8905450b9375f` |
| FE | `ppmkh2sby/FrontEnd_KH2-Management-System` | `agent/face-attendance-deployment` @ `7476b56c04ea84cb4bdb070a9c8020e5f5f62362` |
| Face | `maestrorafa05/Face_Recognition_KH2` | `agent/canonical-backend-contract` @ `f9a5e338ac8bf8462a12cc86e2f85cdf8ade72de` |

Prior evidence: BE Standard CI 184 tests passed on GitHub; separate LiveFaceE2E remains blocked pending approved private fixture and live service. These are historical baselines, not proof of a new Gate 2 run.

## Source-of-truth ownership

- BE alone owns controllers, DTOs, JWT/RBAC, database authorization, face match, pgvector and all user-linked data.
- FE alone owns route/state/UI, browser camera and API client. All data access via BE.
- Face alone owns canonical private `/v1/analyze`, preprocessing, image validation, embedding and model operational gate. No identity or attendance DB in Face.
- BE contract is canonical: `docs/BE_API_MATRIX.md` and `docs/agent-handoff/BE_CONTRACT_HANDOFF.md`. All agents may read others' folders but MUST NOT write there.
- If contract mismatch exists, document method, path, expected/actual DTO/status, test/evidence and owner. Do not silently create a second API contract.

## Mandatory Graphify protocol for EACH agent

1. `git status --short`, `git branch --show-current`, `git rev-parse HEAD`; read `AGENTS.md` and locally available Graphify instructions before editing.
2. `graphify --help`, then `graphify update .` using the installed CLI (version surface may differ); query/explain impacted entry points with supported commands; record graph nodes/edges or failure details.
3. Use Graphify dependency analysis before each significant change; choose targeted regression tests based on affected nodes and edges.
4. After implementation run `graphify update .` again and inspect graph/impact; record exact commands/results in report. Do not claim usage without evidence. Graphify failure is a reported tooling blocker, not permission to invent CLI commands.
5. Keep Graphify outputs/secrets out of commits if they are generated/private; respect existing ignore rules.

## Gate 2 environments and safety

- Use only disposable PostgreSQL 16 + pgvector and a local, isolated BE/FE/Face stack. `Database__MigrateOnStartup=false` and `Database__SeedOnStartup=false` for persistent services. Disposable test DB migrations/seed using synthetic, non-sensitive identities are allowed only inside isolated test workflows.
- No writes to staging/production databases, no Supabase operations, no VPS SSH/deploy, no DNS change, no rotation or printing of secrets, no hard reset/force push.
- No raw face images, JWTs, API keys, device credentials or embeddings in reports/logs. Approved live biometric fixtures stay off public repo/artifacts.
- Public `buffalo_l` weights remain research/development and are NOT authorized for production biometric usage. Do not bypass production model gate or set an uncalibrated production threshold. If real inference unavailable, test deterministic *contracts* with a controlled fake and mark `LIVE_FACE_E2E=BLOCKED`, not PASS.
- No commit/push/PR unless explicitly authorized. Avoid interfering with other running agents; no shared output or source ownership conflicts.

## Shared Gate 2 acceptance matrix

| Domain | Required integration evidence |
|---|---|
| Login/Auth | 5 roles login, JWT, dashboard redirect, mustChangePassword enforcement, expired/invalid token rejection |
| Authorization | Staff sees permitted Santri data; self Santri only own; Wali A can see only linked child, cannot request Wali B child; list AND detail AND each feature query guarded |
| Wali linkage | Valid/empty/stale and multiple children, selector switching, refresh, pagination, no stale/cross-child data |
| Read features | Presensi, Kafarah, Progress Keilmuan, Log; data from disposable PostgreSQL and authorized scoping; no static fallback or fabricated metrics |
| Canonical face | 5 captures -> BE store active embedding -> GET status -> refresh -> new photo self verify -> BE pgvector matching; wrong identity/unknown/ambiguous/no model/timeout/invalid input fail safely; self-verify DOES NOT record Presensi |
| Trusted attendance | Separate authenticated device flow; invalid key, invalid session and duplicate attendance rejected; device keys never in FE |
| Camera | Desktop/mobile layout and request capture orientation; no forced crop/rotation; actual device validation distinguished from mocked browser |
| Contracts | OpenAPI Development documentation, BE–FE method/path/DTO/status alignment, private BE–Face `/v1/analyze` image field and service-key |
| Operational | Isolated stack readiness, correlation IDs/safe logs, tests with raw pass/fail evidence, no production cutover |

## Reporting conventions

Each owner produces `docs/agent-handoff/<OWNER>_GATE2_HANDOFF.md` with baseline and final commit, Graphify command/evidence, code edits, tests executed and exact totals, test artifacts/paths, missing prerequisites, contract drift, and status: `LOCAL_COMPONENT`, `BE_FE_RUNTIME`, `BE_FACE_RUNTIME`, `BROWSER_E2E`, `LIVE_FACE_E2E`. Never report a class as PASS without execution against its defined environment. Gate 2 overall cannot pass until BE/FE live authenticated runtime integration is demonstrated; biometric production approval remains a separate, blocked gate.
