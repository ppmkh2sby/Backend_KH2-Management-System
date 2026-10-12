# Backend contract handoff

Updated: 2026-10-12. Owner: Backend/API. This document is the integration handoff for Frontend and Face Recognition; it does not authorize changes to their repositories.

## Auth and child scope

Use the backend JWT on every non-public call. The backend is authoritative for role and child scope:

- `GET /api/v1/wali/me/children` is Wali-only and returns only persisted, code-valid child links. An empty `items` array means the client must show no selectable child.
- Staff roles (Admin, Pengurus, DewanGuru) receive their permitted management rows.
- A Santri receives only self data.
- A Wali must pass the chosen authorized `santriId`/`santriCode` to guarded read endpoints. There is no default/first child.
- Existing KTB team write permissions for Presensi/Kafarah did not become general read/write permissions.

See `docs/BE_API_MATRIX.md` for the exact routes, request fields, outcomes, and database scope.

## Canonical face API

| Route | Caller | Request | Response/semantics |
| --- | --- | --- | --- |
| `GET /api/v1/face-profiles/me/enrollment` | Santri | — | Current enrollment state. `isEnrolled` means active profile + active enrollment + exactly five samples. |
| `POST /api/v1/face-profiles/me/enrollment` | Santri | multipart `photos`, exactly five JPEG/PNG files, maximum 5 MiB each | `201` on success. Keep captures in transient client memory until submission; do not use localStorage. |
| `POST /api/v1/face-profiles/me/verify` | Santri | multipart `photo`, one JPEG/PNG, maximum 5 MiB | `200 { verified, similarity, reason }`. It does not record attendance and never returns another identity or an embedding. |

The Face Service is inference-only. Backend sends `POST /v1/analyze`, multipart `image`, authenticated with the internal service key. The service must return a valid one-face 512-D result plus exact model name/version. No frontend caller may invoke it directly.

Current error contract for enrollment/verify: `400` invalid request/domain rejection, `401` no valid JWT, `403` wrong role/no Santri mapping, `409` enrollment conflict, `502` invalid Face Service response, `503` service unavailable, and `504` timeout. Verify can additionally return `500` for an invalid internal match result.

## Documentation and legacy boundary

- Development only: `/openapi/v1.json` and `/scalar`.
- Staging/Production: neither route is mapped.
- Legacy face routes remain transitional. With `LegacyFaceApi:Enabled=false`, requests complete authentication/authorization then return `410 Gone` with `Deprecation: true` before controller activation. Do not build new clients against legacy enrollment or legacy attendance routes.

## Non-production status

Production biometric activation remains blocked pending license, threshold calibration, consent/privacy, liveness/manual-fallback review, and explicit operational approval. No migration, database deployment, seeding, or production configuration change is part of this handoff.
