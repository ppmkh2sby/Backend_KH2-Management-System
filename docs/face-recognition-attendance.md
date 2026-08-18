# Face Recognition Attendance

Fitur ini menambah presensi wajah tanpa mengubah endpoint presensi manual. Semua penulisan data dilakukan oleh API KH2; browser dan layanan AI tidak memiliki akses PostgreSQL.

## Konfigurasi layanan AI

Layanan AI harus hanya dapat dijangkau dari jaringan internal backend. Konfigurasikan pada secret/environment deployment, bukan pada frontend:

```json
{
  "FaceRecognition": {
    "BaseUrl": "http://face-recognition.internal/",
    "ConfidenceThreshold": 0.85,
    "TimeoutSeconds": 15,
    "CaptureStoragePath": "/var/lib/kh2/private-face-captures"
  }
}
```

Kontrak internal AI yang dipanggil backend adalah `POST v1/enrollment/validate-capture`, `POST v1/enrollment`, `POST v1/attendance/verify-opener`, `POST v1/attendance/recognize`, dan `DELETE v1/enrollment/{providerProfileId}`. AI mengembalikan `santriId` GUID, tidak pernah nama santri, pada recognition. Jika layanan tidak dapat dijangkau, API mengembalikan `503`; tidak ada presensi otomatis yang dibuat.

Foto capture disimpan di private storage lokal yang dikonfigurasi dan PostgreSQL hanya menyimpan storage key, content type, dan metadata. Gunakan volume yang private/terenkripsi pada deployment. Endpoint API tidak pernah mengirim embedding maupun storage key.

## Endpoint

Semua endpoint memerlukan JWT Bearer token.

| Endpoint | Hak akses | Keterangan |
| --- | --- | --- |
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

Contoh capture ke-1:

```http
POST /api/v1/face-enrollment/me/captures
Authorization: Bearer <jwt>
Content-Type: multipart/form-data

captureOrder=1
photo=@lurus.jpg;type=image/jpeg
```

## Aturan pencatatan

`check-in` hanya mencatat `Presensi` berstatus `hadir` dan source `FaceRecognition` jika satu wajah terdeteksi, AI mengembalikan `SantriId` yang sama dengan Santri JWT, dan confidence memenuhi threshold. Constraint unik `FaceAttendanceSessionId + SantriId` mencegah duplikasi bahkan pada request bersamaan.

Wajah tidak dikenali, multi-face, identitas tidak sama, confidence rendah, atau AI unavailable dicatat sebagai event `review` (kecuali input yang tidak valid) dan tidak membuat record hadir. Presensi manual tetap memakai source `Manual` dan endpoint yang ada tidak berubah.
