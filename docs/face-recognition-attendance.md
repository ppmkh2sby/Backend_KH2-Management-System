# Face Recognition Attendance

Fitur ini menambah presensi wajah tanpa mengubah endpoint presensi manual. Semua penulisan data dilakukan oleh API KH2; browser dan layanan AI tidak memiliki akses PostgreSQL.

## Konfigurasi layanan AI

Layanan AI harus hanya dapat dijangkau dari jaringan internal backend. Konfigurasikan pada secret/environment deployment, bukan pada frontend:

```json
{
  "FaceRecognition": {
    "BaseUrl": "http://face-recognition.internal/",
    "ApiKey": "REPLACE_WITH_A_RANDOM_SECRET_MIN_32_CHARS",
    "SimilarityThreshold": null,
    "TimeoutSeconds": 15,
    "ExpectedEmbeddingDimension": 512,
    "RequiredEnrollmentSamples": 5
  },
  "LegacyFaceProvider": {
    "BaseUrl": "http://legacy-face-provider.internal/",
    "ApiKey": "REPLACE_WITH_A_RANDOM_SECRET_MIN_32_CHARS",
    "ConfidenceThreshold": 0.85,
    "TimeoutSeconds": 15,
    "CaptureStoragePath": "/var/lib/kh2/private-face-captures"
  }
}
```

`FaceRecognition` hanya untuk layanan embedding kanonis (`/v1/analyze` dan
`/v1/health`). `SimilarityThreshold` adalah ambang cosine-similarity kanonis
dan tidak setara dengan confidence provider lama; biarkan `null` hingga
kalibrasi tersedia. `LegacyFaceProvider` hanya untuk route transitional yang
masih memanggil provider lama, termasuk penyimpanan capture staged.

Migrasi deployment yang sebelumnya memakai field legacy di `FaceRecognition`
harus memindahkan `BaseUrl`, `ServiceApiKey` (menjadi `ApiKey`),
`ConfidenceThreshold`, `TimeoutSeconds`, dan `CaptureStoragePath` ke
`LegacyFaceProvider`. Tidak ada fallback otomatis ke field lama.

Kontrak internal AI yang dipanggil backend adalah `POST v1/enrollment/validate-capture`, `POST v1/enrollment`, `POST v1/attendance/verify-opener`, `POST v1/attendance/recognize`, dan `DELETE v1/enrollment/{providerProfileId}`. AI mengembalikan `santriId` GUID, tidak pernah nama santri, pada recognition. Jika layanan tidak dapat dijangkau, API mengembalikan `503`; tidak ada presensi otomatis yang dibuat.

Enrollment kanonis tidak menyimpan lima foto capture di server. Klien menahan capture sementara di memori, kemudian mengirim tepat lima foto sekaligus ke endpoint kanonis. Endpoint API tidak pernah mengirim embedding maupun storage key.

> **Deprecation:** workflow staged pada `/api/v1/face-enrollment/me` bersifat transisional untuk client lama. Client baru wajib menggunakan `POST /api/v1/face-profiles/me/enrollment`; jangan membangun integrasi baru pada endpoint staged atau server-side capture storage.

## Endpoint

Semua endpoint memerlukan JWT Bearer token.

| Endpoint | Hak akses | Keterangan |
| --- | --- | --- |
| `POST /api/v1/face-profiles/me/enrollment` | Santri | **Canonical.** `multipart/form-data` dengan tepat lima file `photos`; semua capture dikirim dalam satu request. |
| `GET /api/v1/face-enrollment/me` | Santri | Status `belum-terdaftar`, `proses`, `terdaftar`, atau `ditolak`; juga mengembalikan lima panduan pose. |
| `POST /api/v1/face-enrollment/me/captures` | Santri pemilik | `multipart/form-data`: `captureOrder` (1-5) dan `photo`. Urutan pose: lurus, sedikit kiri, sedikit kanan, menengadah, menunduk. |
| `POST /api/v1/face-enrollment/me/complete` | Santri pemilik | Membuat profil AI hanya jika lima capture valid tersedia. |
| `DELETE /api/v1/face-enrollment/me` | Santri pemilik | Menghapus profil AI, metadata, dan private captures milik sendiri. Jika AI tidak tersedia reset ditolak dengan `503`, sehingga profil AI tidak tertinggal. |
| `POST /api/v1/face-attendance/sessions` | Admin, DewanGuru, Pengurus, atau Santri tim KTB/Ketertiban | Membuat sesi `menunggu-verifikasi`. |
| `POST /api/v1/face-attendance/sessions/{id}/verify-opener` | Petugas pembuka | Mengirim `photo` multipart; sesi hanya berubah ke `open` bila wajah petugas diverifikasi AI. |
| `POST /api/v1/face-attendance/sessions/{id}/check-in` | Santri | Mengirim `photo` multipart pada sesi `open`. |
| `POST /api/v1/face-attendance/sessions/{id}/close` | Petugas pembuka atau Admin | Menutup sesi. |
| `GET /api/v1/face-attendance/sessions[?tanggal=YYYY-MM-DD]` | Operator sesi | Daftar sesi. |
| `GET /api/v1/face-attendance/sessions/{id}` | Operator sesi | Detail sesi. |
| `GET /api/v1/face-attendance/sessions/{id}/records` | Operator sesi | Event accepted/review untuk review manual. |

Contoh membuat sesi:

```http
POST /api/v1/face-attendance/sessions
Authorization: Bearer <jwt>
Content-Type: application/json

{
  "kelas": "Kelas A",
  "kegiatan": "Kajian malam",
  "waktu": "malam",
  "tanggal": "2026-08-15"
}
```

Contoh canonical enrollment:

```http
POST /api/v1/face-profiles/me/enrollment
Authorization: Bearer <jwt>
Content-Type: multipart/form-data

photos=@lurus.jpg;type=image/jpeg
photos=@sedikit-kiri.jpg;type=image/jpeg
photos=@sedikit-kanan.jpg;type=image/jpeg
photos=@menengadah.jpg;type=image/jpeg
photos=@menunduk.jpg;type=image/jpeg
```

Client harus menyimpan lima capture hanya di memori sementara, mengizinkan penggantian sebelum submit, dan menghapusnya dari memori setelah sukses atau batal. Jangan menyimpan capture biometrik ke `localStorage` tanpa review keamanan eksplisit.

## Aturan pencatatan

`check-in` hanya mencatat `Presensi` berstatus `hadir` dan source `FaceRecognition` jika satu wajah terdeteksi, AI mengembalikan `SantriId` yang sama dengan Santri JWT, dan confidence memenuhi threshold. Constraint unik `FaceAttendanceSessionId + SantriId` mencegah duplikasi bahkan pada request bersamaan.

Wajah tidak dikenali, multi-face, identitas tidak sama, confidence rendah, atau AI unavailable dicatat sebagai event `review` (kecuali input yang tidak valid) dan tidak membuat record hadir. Presensi manual tetap memakai source `Manual` dan endpoint yang ada tidak berubah.
