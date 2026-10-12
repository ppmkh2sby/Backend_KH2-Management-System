# Backend API contract matrix

Contract freeze: 2026-10-12. PostgreSQL is the authority for every scope below. Client-side filtering is not authorization.

| Method/path | Authorization and database scope | Request | Success response | Relevant failures |
| --- | --- | --- | --- | --- |
| `POST /api/v1/auth/login` | Anonymous | identity/username/email and password | `200 LoginResponse` (JWT/account flags) | `400`, `401` |
| `GET /api/v1/auth/me` | Any authenticated role; current claims only | — | `200 AuthMeResponse` | `401` |
| `GET /api/v1/wali/me/children` | `WaliSantri`; joins `WaliSantriRelations`, `Users`, `Santris`; relation code must equal the caller's username | — | `200 { items: [{ santriId, nis, fullName, tim }] }` | `401`, `403`; stale/no link is `200` with an empty array |
| `GET /api/v1/santri` and detail | Admin/Pengurus/DewanGuru: permitted rows; Santri: self; Wali: linked children only | Existing query parameters | Existing paged/detail DTO | `401`, `403`, guarded not-found |
| `GET /api/v1/presensi` | Staff: all permitted rows; Santri: self; Wali: selected, linked `santriId` only | Existing query, Wali supplies `santriId` | Existing `PresensiListResponse` | `401`, `403` |
| `GET /api/v1/kafarah` | Same scoped-read model as Presensi | Existing query, Wali supplies `santriId` | Existing `KafarahListResponse` | `401`, `403` |
| `GET /api/v1/log-keluar-masuk` | Staff: permitted rows/team view; Santri: self; Wali: selected, linked `santriId` only | Existing query including `santriId`/scope where applicable | Existing `LogKeluarMasukListResponse` | `401`, `403` |
| `GET /api/v1/progress-keilmuan` | Staff: permitted rows; Santri: self; Wali: selected, linked `santriCode` only—never a first-child fallback | Existing query with `santriCode` for Wali | Existing role-specific progress DTO | `401`, `403` |
| Existing Presensi/Kafarah write routes | Existing KTB Santri team privilege; read expansion grants no write privilege | Existing write DTO | Existing DTO/status | Existing validation/authorization status |
| `GET /api/v1/face-profiles/me/enrollment` | `Santri`; resolves only caller's `Santri → FaceProfile → Active FaceEnrollment → FaceEmbeddings` | — | `200 { isEnrolled, status, sampleCount, modelName, modelVersion, enrolledAtUtc }` | `401`, `403`; no profile/enrollment is `200` with `isEnrolled:false` |
| `POST /api/v1/face-profiles/me/enrollment` | `Santri`; canonical face tables only | multipart `photos`: exactly 5 JPEG/PNG files, each <=5 MiB | `201 FaceProfileEnrollmentDto` | `400` invalid input/domain rejection, `403`, `409`, `502`, `503`, `504` |
| `POST /api/v1/face-profiles/me/verify` | `Santri`; verifies only caller's canonical profile/enrollment through Face Service and pgvector; never writes Presensi | multipart `photo`: one JPEG/PNG <=5 MiB | `200 { verified, similarity, reason }` | `400`, `401`, `403`, `500`, `502`, `503`, `504` |

Enrollment status is true only for an active profile, active enrollment, and exactly `FaceRecognition:RequiredEnrollmentSamples` embeddings (currently five). Face self-verification returns neither embeddings nor another Santri's identity; an unmatched/other face is a normal non-verification result.

`/api/v1/dashboard/santri/me*` remains self-Santri only. It deliberately does not select an arbitrary child for Wali/staff. Wali clients first call `/api/v1/wali/me/children`, then call a guarded child/read endpoint with the selection.

## Face-service boundary

The backend calls `POST /v1/analyze` with multipart field `image` and the private service credential. It accepts one-face, finite nonzero 512-D embeddings with model name/version only. Browser clients never receive vectors, service keys, or competing identities.

## Interactive API documentation

Native .NET OpenAPI is available only when `ASPNETCORE_ENVIRONMENT=Development`:

- `GET /openapi/v1.json`
- `GET /scalar`

Both routes are intentionally absent from Testing, Staging, and Production. They must not be exposed by reverse-proxy routing in those environments.
