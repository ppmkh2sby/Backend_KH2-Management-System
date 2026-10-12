# Backend API contract matrix

Contract freeze: 2026-10-12. These routes are backed by PostgreSQL and enforce scope on the server; the frontend must not use client-side filtering as an authorization control.

| Method/path | Caller | Scope / request | Response |
| --- | --- | --- | --- |
| `POST /api/v1/auth/login` | Anonymous | `identity`/`username`/`email` plus `password`; email is optional | `LoginResponse` JWT and account flags |
| `GET /api/v1/auth/me` | Any authenticated role | Current claim identity | `AuthMeResponse` |
| `GET /api/v1/wali/me/children` | `WaliSantri` only | Persisted, code-valid `WaliSantriRelations` for caller | `{ items: [{ santriId, nis, fullName, tim }] }`; empty means no valid links |
| `GET /api/v1/santri` | All roles | Staff all; Santri self; Wali linked children | Paged `SantriListResponse` |
| `GET /api/v1/presensi` | Staff, Santri, Wali | Staff all; Santri self; Wali must supply an authorized `santriId` | Paged `PresensiListResponse` |
| `GET /api/v1/kafarah` | Staff, Santri, Wali | Staff all; Santri self; Wali must supply an authorized `santriId` | Paged `KafarahListResponse` |
| `GET /api/v1/log-keluar-masuk` | Staff, Santri, Wali | Wali must supply authorized `santriId`; staff may use `scope=team` | Paged `LogKeluarMasukListResponse` |
| `GET /api/v1/progress-keilmuan` | Staff, Santri, Wali | Wali must supply authorized `santriCode`; no first-child fallback | Role-specific existing progress DTO |
| `POST/PATCH/DELETE /api/v1/presensi...` | KTB Santri only | Existing team privilege | Existing write DTOs |
| `POST/PATCH/DELETE /api/v1/kafarah...` | KTB Santri only | Existing team privilege | Existing write DTOs |
| `GET /api/v1/face-profiles/me/enrollment` | `Santri` only | Current Santri, active canonical profile/enrollment | `MyFaceEnrollmentStatusDto` |
| `POST /api/v1/face-profiles/me/enrollment` | `Santri` only | multipart field `photos`, exactly five JPEG/PNG captures | `FaceProfileEnrollmentDto`, 201 |
| `POST /api/v1/face-profiles/me/verify` | `Santri` only | multipart field `photo`, one JPEG/PNG capture | Self-verification DTO; no attendance row or other identity |

`/api/v1/dashboard/santri/me*` remains a self-Santri endpoint. It deliberately returns unavailable for Wali/staff rather than selecting an arbitrary Santri. Wali UI must select a child, then call the guarded child/read endpoints above.

Face service boundary: backend sends `POST v1/analyze` multipart field `image` with private service key. Accepted responses require exactly one face, a finite nonzero 512-D embedding, and model name/version. The backend never sends vectors or another Santri identity to browser clients.
