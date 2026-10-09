-- KH2 Management System: reset and seed application data for Supabase/PostgreSQL.
-- Prerequisite: run scripts/apply-backend-migrations.sql first, so the database
-- has the complete EF Core schema (including face-recognition tables and columns).
-- Scope: only the KH2 application tables are cleared. Supabase auth/storage/schema data is untouched.
-- Initial password for every seeded account: Kh2Awal123!

BEGIN;

DO $kh2_santri_seed$
BEGIN
-- Intentionally does not use CASCADE: if an unknown table references one of these,
-- PostgreSQL stops instead of silently deleting data outside the KH2 application scope.
TRUNCATE TABLE
    public."FaceRecognitionEvents",
    public."FaceEnrollmentCaptures",
    public."FaceProfiles",
    public."FaceEnrollments",
    public."Presensis",
    public."FaceAttendanceSessions",
    public."Sesis",
    public."Kegiatans",
    public."Kafarahs",
    public."ProgressKeilmuans",
    public."LogKeluarMasuks",
    public."WaliSantriRelations",
    public."EmailVerificationCodes",
    public."refresh_tokens",
    public."Santris",
    public."Users"
RESTART IDENTITY;

DROP TABLE IF EXISTS _kh2_seed_santris;

CREATE TEMP TABLE _kh2_seed_santris (
    full_name text NOT NULL,
    nis text NOT NULL,
    kampus text NOT NULL,
    jurusan text NOT NULL,
    gender text NOT NULL,
    tim text NOT NULL,
    kelas text NOT NULL,
    catatan text NULL
) ON COMMIT DROP;

INSERT INTO _kh2_seed_santris (full_name, nis, kampus, jurusan, gender, tim, kelas, catatan) VALUES
    ('ABDULLAH JUWAN DAWAIRA', '022121013', 'UNAIR', 'Keselamatan dan Kesehatan Kerja', 'putra', 'PH', 'Cepatan', NULL),
    ('MUHAMMAD FARID FATCHUR', '022222001', 'ITS', 'Sistem Perkapalan', 'putra', 'Kebersihan', 'Cepatan', NULL),
    ('MUHAMMAD FATH RAJIHAN NAFIE', '022222006', 'UNAIR', 'Matematika', 'putra', 'Sekben', 'Lambatan', NULL),
    ('ARDIAS AJI SAPUTRO', '022323002', 'ITS', 'Teknik Infrastruktur Sipil', 'putra', 'KBM', 'Lambatan', NULL),
    ('MUHAMMAD IRSYAD IBRAHIMOVIC', '022323004', 'ITS', 'Teknik Material dan Metalurgi', 'putra', 'PH', 'Lambatan', NULL),
    ('SYAIFUDIN AKBARI ABILUDIN', '022323006', 'PPNS', 'Manajemen Bisnis', 'putra', 'Acara', 'Pegon', NULL),
    ('ALWIDA RAHMAT', '022424001', 'ITS', 'Sistem Informasi', 'putra', 'KTB', 'Lambatan', NULL),
    ('FAHMI ROSYIDIN AL''ULYA', '022424006', 'PENS', 'Teknik Informatika', 'putra', 'Sekben', 'Lambatan', NULL),
    ('KEISHA ZAFIF FAHREZI', '022424007', 'PENS', 'Multimedia Broadcast', 'putra', 'Acara', 'Lambatan', NULL),
    ('MAESTRO RAFA AGNIYA', '022424008', 'PENS', 'Teknik Informatika', 'putra', 'KTB', 'Lambatan', NULL),
    ('MUHAMMAD FARREL AL-AQSA', '022424010', 'UNAIR', 'Fisioterapi', 'putra', 'Upkpt', 'Bacaan', NULL),
    ('MUHAMMAD FARIZKY ALFATH MAHARDIAN PUTRA', '022424011', 'ITS', 'Teknik Sipil', 'putra', 'KBM', 'Pegon', NULL),
    ('MUHAMMAD SETYO ARFAN IBRAHIM', '022424012', 'ITS', 'Desain Produk', 'putra', 'Upkpt', 'Bacaan', NULL),
    ('VIKY KARUNIA PUTRA PRATAMA', '022423017', 'PPNS', 'Teknik Desain dan Manufaktur', 'putra', 'Kebersihan', 'Lambatan', NULL),
    ('ZAKI AFIF ARIF', '022424019', 'ITS', 'Teknik Lingkungan', 'putra', 'KBM', 'Bacaan', NULL),
    ('BRILIANT ACHMAD RAMADHAN', '022525004', 'ITS', 'Teknik Lepas Pantai', 'putra', 'Kebersihan', 'Lambatan', NULL),
    ('DIMAS ADI SANJAYA', '022525005', 'Universitas Dr. Soetomo', 'Manajemen', 'putra', 'Upkpt', 'Bacaan', NULL),
    ('FARIS JULDAN', '022525006', 'PPNS', 'Keselamatan dan Kesehatan Kerja', 'putra', 'Sekben', 'Bacaan', NULL),
    ('HANAFI SATRIYO UTOMO SETIAWAN', '022525007', 'ITS', 'S2 - Teknik Informatika', 'putra', 'Acara', 'Bacaan', NULL),
    ('SOFWAN MIFTAKHUDDIN MAARIF', '022525013', 'UNAIR', 'Farmasi', 'putra', 'KTB', 'Bacaan', NULL),
    ('MUHAMAD BAEHAQI AL MUJAHIDIN', '022524015', 'UNAIR', 'Teknologi Hasil Perikanan', 'putra', 'Acara', 'Pegon', NULL),
    ('TARISSA ADELYA SAFIERA', '022222004', 'ITS', 'Perencanaan Wilayah dan Kota', 'putri', 'Acara', 'Cepatan', NULL),
    ('AISYA WIDYA PRATIWI', '022323001', 'UNAIR', 'Matematika', 'putri', 'PH', 'Cepatan', NULL),
    ('CASEY PALLAS TALITHA HARJANTO', '022323003', 'ITS', 'Desain Komunikasi Visual', 'putri', 'Kebersihan', 'Lambatan', NULL),
    ('RIZKY KHOIRUNNISA', '022323005', 'PENS', 'Teknik Telekomunikasi', 'putri', 'KBM', 'Pegon', NULL),
    ('AYESHA NAYYARA PUTRI WURYADI', '022424002', 'PPNS', 'Teknik Perancangan dan Konstruksi Kapal', 'putri', 'Acara', 'Lambatan', NULL),
    ('AZZAHRA JAMALULLAILY MAFZA', '022424003', 'UNAIR', 'Bahasa dan Sastra Inggris', 'putri', 'Upkpt', 'Lambatan', NULL),
    ('CHERFINE AN-NISAUL AULIYA ULLA', '022424004', 'ITS', 'Teknik Sipil', 'putri', 'KBM', 'Lambatan', NULL),
    ('DEVEN KARTIKA WIJAYA', '022424005', 'ITS', 'Arsitektur', 'putri', 'Kebersihan', 'Lambatan', NULL),
    ('MARITZA DARA ATHIFA', '022424009', 'ITS', 'Sistem Informasi', 'putri', 'Sekben', 'Pegon', NULL),
    ('NABILA KAYSA ADRISTI', '022424013', 'ITS', 'Studi Pembangunan', 'putri', 'Kebersihan', 'Cepatan', NULL),
    ('RARA ARIMBI GITA ATMODJO', '022424014', 'ITS', 'Desain Komunikasi Visual', 'putri', 'PH', 'Pegon', NULL),
    ('RENATA KEYSHA AZALIA KHORUNNISA', '022424015', 'ITS', 'Teknik Geofisika', 'putri', 'Acara', 'Lambatan', NULL),
    ('SYAHDINDA SHERLYTA LAURA', '022424016', 'UNAIR', 'Bahasa dan Sastra Inggris', 'putri', 'KTB', 'Lambatan', NULL),
    ('ZAHRA SUCIANA TRI AMMA MARETHA', '022424018', 'UNAIR', 'Akuntansi', 'putri', 'Sekben', 'Lambatan', NULL),
    ('AMANDA RAMADHANI PUTRI PANGESTI', '022525001', 'PENS', 'Teknik Informatika', 'putri', 'Upkpt', 'Bacaan', NULL),
    ('AURA RENATA ANASYIYA AZKA', '022525002', 'PENS', 'Sains Data Terapan', 'putri', 'Acara', 'Bacaan', NULL),
    ('BALQIS SALWA AURELIA AZZAHRA', '022525003', 'ITS', 'Teknologi Kedokteran', 'putri', 'KTB', 'Bacaan', NULL),
    ('IMELYA URIVARTOUSI', '022525008', 'ITS', 'Sistem Informasi', 'putri', 'KTB', 'Lambatan', NULL),
    ('MAYLAVASA ADIVA BILQIS', '022525009', 'PENS', 'Teknik Elektronika Industri', 'putri', 'Kebersihan', 'Bacaan', NULL),
    ('QISTHI KHIROFATI MADINA SENOAJI', '022525010', 'PENS', 'Teknik Informatika', 'putri', 'Acara', 'Bacaan', NULL),
    ('RASHIDA ZARA FAUZIAH', '022525011', 'ITS', 'Studi Pembangunan', 'putri', 'Sekben', 'Lambatan', NULL),
    ('SAFA KARINDAH KAHAYA AISHA', '022525012', 'UMS', 'Farmasi', 'putri', 'Upkpt', 'Bacaan', NULL),
    ('SYARIFAH HUURI FILJANNAH', '022525014', 'ITS', 'Teknik Kimia', 'putri', 'KBM', 'Bacaan', NULL);

CREATE TEMP TABLE _kh2_seed_staff (
    username text NOT NULL,
    full_name text NOT NULL,
    role text NOT NULL
) ON COMMIT DROP;

INSERT INTO _kh2_seed_staff (username, full_name, role) VALUES
    ('admin', 'Admin KH2', 'Admin'),
    ('0235499001', 'Amir', 'DewanGuru'),
    ('0235499002', 'Anton', 'DewanGuru'),
    ('0235499003', 'Ridho', 'DewanGuru'),
    ('0218354001', 'Saiful', 'Pengurus'),
    ('0218354002', 'Hirul', 'Pengurus'),
    ('0218354003', 'Angga', 'Pengurus'),
    ('0218354004', 'Avan', 'Pengurus'),
    ('0218354005', 'Abdurrahman', 'Pengurus'),
    ('wali', 'Wali Santri KH2', 'WaliSantri'),
    ('wali-putri', 'Wali Santri Putri KH2', 'WaliSantri');

-- The hash was generated with the same ASP.NET Core PasswordHasher<User> used by the backend.
-- md5() is used only to create stable UUIDs; it is not used for passwords or authentication.
INSERT INTO public."Users" (
    "Id", "Username", "FullName", "Email", "Role", "PasswordHash",
    "EmailConfirmed", "IsActive", "MustChangePassword", "CreatedAtUtc", "UpdatedAtUtc"
)
SELECT
    md5('kh2-user:' || nis)::uuid,
    nis,
    full_name,
    NULL,
    'Santri',
    'AQAAAAIAAYagAAAAED3N5sx3HUR0tzyAi+ivf2Y/xbFJ0b1lZwmQ56Z0VVh6sGAXDF/6v8mNTLMniIXB7g==',
    FALSE, TRUE, FALSE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
FROM _kh2_seed_santris
UNION ALL
SELECT
    md5('kh2-user:' || username)::uuid,
    username,
    full_name,
    NULL,
    role,
    'AQAAAAIAAYagAAAAED3N5sx3HUR0tzyAi+ivf2Y/xbFJ0b1lZwmQ56Z0VVh6sGAXDF/6v8mNTLMniIXB7g==',
    FALSE, TRUE, FALSE, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP
FROM _kh2_seed_staff;

INSERT INTO public."Santris" (
    "Id", "UserId", "FullName", "Nis", "Kampus", "Jurusan", "Gender", "Tim", "Kelas", "Catatan", "CreatedAtUtc", "UpdatedAtUtc"
)
SELECT
    md5('kh2-santri:' || nis)::uuid,
    md5('kh2-user:' || nis)::uuid,
    full_name, nis, kampus, jurusan, gender, tim, kelas, catatan,
    CURRENT_TIMESTAMP, NULL
FROM _kh2_seed_santris;

END
$kh2_santri_seed$;

INSERT INTO public."WaliSantriRelations" (
    "Id", "WaliUserId", "SantriId", "RelationshipLabel", "WaliSantriCode", "CreatedAtUtc", "UpdatedAtUtc"
)
SELECT
    md5('kh2-wali:' || wali_username || ':' || nis)::uuid,
    md5('kh2-user:' || wali_username)::uuid,
    md5('kh2-santri:' || nis)::uuid,
    'Orang Tua',
    '354' || substring(nis FROM 3),
    CURRENT_TIMESTAMP, NULL
FROM (VALUES
    ('wali', '022424008'),
    ('wali', '022424001'),
    ('wali-putri', '022424016'),
    ('wali-putri', '022525003')
) AS relations(wali_username, nis);

DO $kh2_session_seed$
BEGIN
DROP TABLE IF EXISTS _kh2_seed_sessions;

CREATE TEMP TABLE _kh2_seed_sessions ON COMMIT DROP AS
SELECT
    sequence,
    md5('kh2-kegiatan:' || kategori || ':' || waktu)::uuid AS kegiatan_id,
    md5('kh2-sesi:' || kategori || ':' || waktu || ':' || (CURRENT_DATE + days_ago)::text)::uuid AS sesi_id,
    CURRENT_DATE + days_ago AS tanggal,
    kategori,
    waktu,
    catatan
FROM (VALUES
    (0, -3, 'sambung', 'pagi', 'Sambung Pagi'),
    (1, -2, 'asrama', 'subuh', 'Asrama Subuh'),
    (2, -1, 'sambung', 'siang', 'Sambung Siang'),
    (3,  0, 'asrama', 'malam', 'Asrama Malam')
) AS sessions(sequence, days_ago, kategori, waktu, catatan);

INSERT INTO public."Kegiatans" ("Id", "Kategori", "Waktu", "Catatan", "CreatedAtUtc", "UpdatedAtUtc")
SELECT kegiatan_id, kategori, waktu, catatan, CURRENT_TIMESTAMP, NULL
FROM _kh2_seed_sessions;

INSERT INTO public."Sesis" ("Id", "KegiatanId", "Tanggal", "Catatan", "CreatedAtUtc", "UpdatedAtUtc")
SELECT sesi_id, kegiatan_id, tanggal, NULL, CURRENT_TIMESTAMP, NULL
FROM _kh2_seed_sessions;

INSERT INTO public."Presensis" (
    "Id", "SantriId", "Nama", "Status", "KegiatanId", "SesiId", "Catatan", "Waktu", "Source", "FaceAttendanceSessionId", "CreatedAtUtc", "UpdatedAtUtc"
)
SELECT
    md5('kh2-presensi:' || santri."Nis" || ':' || session.sesi_id::text)::uuid,
    santri."Id",
    santri."FullName",
    status,
    session.kegiatan_id,
    session.sesi_id,
    CASE status
        WHEN 'izin' THEN 'Izin kegiatan kampus.'
        WHEN 'sakit' THEN 'Sedang kurang fit.'
        WHEN 'alpa' THEN 'Belum ada keterangan.'
        ELSE 'Hadir sesuai jadwal.'
    END,
    session.waktu,
    'Manual',
    NULL,
    CURRENT_TIMESTAMP,
    NULL::timestamp with time zone
FROM (
    SELECT
        s."Id", s."Nis", s."FullName",
        row_number() OVER (ORDER BY s."Gender", s."Nis") - 1 AS santri_index
    FROM public."Santris" AS s
) AS santri
CROSS JOIN _kh2_seed_sessions AS session
CROSS JOIN LATERAL (
    SELECT CASE ((santri.santri_index + session.sequence) % 10)
        WHEN 0 THEN 'sakit'
        WHEN 1 THEN 'izin'
        WHEN 2 THEN 'alpa'
        WHEN 3 THEN 'izin'
        ELSE 'hadir'
    END AS status
) AS attendance;

END
$kh2_session_seed$;

WITH ordered_santris AS (
    SELECT
        s."Id", s."Nis",
        row_number() OVER (ORDER BY s."Gender", s."Nis") - 1 AS item_index
    FROM public."Santris" AS s
),
kafarah AS (
    SELECT
        *,
        CASE item_index
            WHEN 0 THEN 'tidak_sholat_subuh_di_masjid'
            WHEN 1 THEN 'tidak_sambung_pagi'
            WHEN 2 THEN 'tidak_sambung_malam'
            WHEN 3 THEN 'tidak_apel_malam'
            WHEN 4 THEN 'tidak_sholat_malam'
            WHEN 5 THEN 'terlambat_kembali_ke_ppm'
            WHEN 6 THEN 'tidak_asrama_sesi_pagi'
            WHEN 7 THEN 'tidak_asrama_sesi_siang'
            WHEN 8 THEN 'tidak_asrama_sesi_sore'
            WHEN 9 THEN 'tidak_asrama_sesi_malam'
        END AS jenis_pelanggaran
    FROM ordered_santris
    WHERE item_index < 10
)
INSERT INTO public."Kafarahs" (
    "Id", "SantriId", "Tanggal", "JenisPelanggaran", "Kafarah", "JumlahSetor", "Tanggungan", "Tenggat", "CreatedAtUtc", "UpdatedAtUtc"
)
SELECT
    md5('kh2-kafarah:' || "Nis" || ':' || item_index)::uuid,
    "Id",
    CURRENT_DATE - (item_index + 1)::integer,
    jenis_pelanggaran,
    CASE jenis_pelanggaran
        WHEN 'tidak_sholat_subuh_di_masjid' THEN 'Istigfar 250'
        WHEN 'tidak_apel_malam' THEN 'Istigfar 250'
        WHEN 'terlambat_kembali_ke_ppm' THEN 'Membayar 10K/15K/25K'
        ELSE 'Istigfar 150'
    END,
    (item_index % 3)::integer,
    CASE jenis_pelanggaran
        WHEN 'tidak_sholat_subuh_di_masjid' THEN 250
        WHEN 'tidak_apel_malam' THEN 250
        WHEN 'terlambat_kembali_ke_ppm' THEN 10000
        ELSE 150
    END,
    to_char(CURRENT_DATE - (item_index + 1)::integer + 7, 'YYYY-MM-DD'),
    CURRENT_TIMESTAMP,
    NULL::timestamp with time zone
FROM kafarah;

WITH ordered_santris AS (
    SELECT
        s."Id", s."Nis",
        row_number() OVER (ORDER BY s."Gender", s."Nis") - 1 AS item_index
    FROM public."Santris" AS s
)
INSERT INTO public."ProgressKeilmuans" (
    "Id", "SantriId", "Judul", "Target", "Capaian", "Satuan", "Level", "Catatan", "Pembimbing", "TerakhirSetorUtc", "CreatedAtUtc", "UpdatedAtUtc"
)
SELECT
    md5('kh2-progress:quran:' || "Nis")::uuid,
    "Id",
    'Setor Juz 30',
    37,
    least(37, 18 + item_index)::integer,
    'halaman',
    'al-quran',
    'Perkembangan murojaah mingguan',
    'Ustadz Amir',
    CURRENT_TIMESTAMP - ((item_index + 1)::text || ' days')::interval,
    CURRENT_TIMESTAMP,
    NULL::timestamp with time zone
FROM ordered_santris
WHERE item_index < 12
UNION ALL
SELECT
    md5('kh2-progress:hadits:' || "Nis")::uuid,
    "Id",
    'Hafalan Arbain',
    20,
    least(20, 8 + (item_index % 10))::integer,
    'hadits',
    'al-hadits',
    'Setoran hadits pekanan',
    'Ustadz Anton',
    CURRENT_TIMESTAMP - ((item_index + 1)::text || ' days')::interval,
    CURRENT_TIMESTAMP,
    NULL::timestamp with time zone
FROM ordered_santris
WHERE item_index < 12;

WITH ordered_santris AS (
    SELECT
        s."Id", s."Nis",
        row_number() OVER (ORDER BY s."Gender", s."Nis") - 1 AS item_index
    FROM public."Santris" AS s
)
INSERT INTO public."LogKeluarMasuks" (
    "Id", "SantriId", "TanggalPengajuan", "Jenis", "Rentang", "Status", "Petugas", "Catatan", "CreatedAtUtc", "UpdatedAtUtc"
)
SELECT
    md5('kh2-log:' || "Nis" || ':' || item_index)::uuid,
    "Id",
    CURRENT_DATE - (item_index + 1)::integer,
    CASE WHEN item_index % 2 = 0 THEN 'Keluar' ELSE 'Masuk' END,
    '14.00 - 16.00',
    CASE item_index % 3
        WHEN 0 THEN 'tercatat'
        WHEN 1 THEN 'proses'
        ELSE 'disetujui'
    END,
    'Ketertiban',
    CASE WHEN item_index % 2 = 0 THEN 'Izin kegiatan kampus.' ELSE 'Kembali ke pondok.' END,
    CURRENT_TIMESTAMP,
    NULL
FROM ordered_santris
WHERE item_index < 8;

COMMIT;

-- Optional verification (safe to run again):
SELECT 'Users' AS table_name, count(*) AS row_count FROM public."Users"
UNION ALL SELECT 'Santris', count(*) FROM public."Santris"
UNION ALL SELECT 'WaliSantriRelations', count(*) FROM public."WaliSantriRelations"
UNION ALL SELECT 'Kegiatans', count(*) FROM public."Kegiatans"
UNION ALL SELECT 'Sesis', count(*) FROM public."Sesis"
UNION ALL SELECT 'Presensis', count(*) FROM public."Presensis"
UNION ALL SELECT 'Kafarahs', count(*) FROM public."Kafarahs"
UNION ALL SELECT 'ProgressKeilmuans', count(*) FROM public."ProgressKeilmuans"
UNION ALL SELECT 'LogKeluarMasuks', count(*) FROM public."LogKeluarMasuks"
ORDER BY table_name;
