CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312022601_InitialCreate') THEN
    CREATE TABLE "Users" (
        "Id" uuid NOT NULL,
        "FullName" character varying(200) NOT NULL,
        "Email" character varying(200) NOT NULL,
        "Role" character varying(50) NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312022601_InitialCreate') THEN
    CREATE TABLE refresh_tokens (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        token_hash character varying(500) NOT NULL,
        expires_at_utc timestamp with time zone NOT NULL,
        revoked_at_utc timestamp with time zone,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone,
        CONSTRAINT "PK_refresh_tokens" PRIMARY KEY (id),
        CONSTRAINT "FK_refresh_tokens_Users_user_id" FOREIGN KEY (user_id) REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312022601_InitialCreate') THEN
    CREATE TABLE "Santris" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "FullName" character varying(200) NOT NULL,
        "Nis" character varying(50) NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_Santris" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Santris_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312022601_InitialCreate') THEN
    CREATE TABLE "WaliSantriRelations" (
        "Id" uuid NOT NULL,
        "WaliUserId" uuid NOT NULL,
        "SantriId" uuid NOT NULL,
        "RelationshipLabel" character varying(100) NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_WaliSantriRelations" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_WaliSantriRelations_Santris_SantriId" FOREIGN KEY ("SantriId") REFERENCES "Santris" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_WaliSantriRelations_Users_WaliUserId" FOREIGN KEY ("WaliUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312022601_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_refresh_tokens_token_hash" ON refresh_tokens (token_hash);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312022601_InitialCreate') THEN
    CREATE INDEX "IX_refresh_tokens_user_id" ON refresh_tokens (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312022601_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Santris_Nis" ON "Santris" ("Nis");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312022601_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Santris_UserId" ON "Santris" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312022601_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312022601_InitialCreate') THEN
    CREATE INDEX "IX_WaliSantriRelations_SantriId" ON "WaliSantriRelations" ("SantriId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312022601_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_WaliSantriRelations_WaliUserId_SantriId" ON "WaliSantriRelations" ("WaliUserId", "SantriId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312022601_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260312022601_InitialCreate', '10.0.0');
    END IF;
END $EF$;
COMMIT;
START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312034019_AddUserPasswordHash') THEN
    ALTER TABLE "Users" ADD "PasswordHash" character varying(500) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312034019_AddUserPasswordHash') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260312034019_AddUserPasswordHash', '10.0.0');
    END IF;
END $EF$;
COMMIT;
START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312062057_AddSantriProfileFieldsAndMasterSeed') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260312062057_AddSantriProfileFieldsAndMasterSeed', '10.0.0');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312123000_AddUserSecurityAndEmailVerification') THEN
    ALTER TABLE "Users" ADD "Username" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312123000_AddUserSecurityAndEmailVerification') THEN
    ALTER TABLE "Users" ADD "EmailConfirmed" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312123000_AddUserSecurityAndEmailVerification') THEN
    ALTER TABLE "Users" ADD "IsActive" boolean NOT NULL DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312123000_AddUserSecurityAndEmailVerification') THEN
    ALTER TABLE "Users" ADD "MustChangePassword" boolean NOT NULL DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312123000_AddUserSecurityAndEmailVerification') THEN
    ALTER TABLE "Santris" ADD "Catatan" character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312123000_AddUserSecurityAndEmailVerification') THEN
    ALTER TABLE "Santris" ADD "Gender" character varying(20) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312123000_AddUserSecurityAndEmailVerification') THEN
    ALTER TABLE "Santris" ADD "Jurusan" character varying(200) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312123000_AddUserSecurityAndEmailVerification') THEN
    ALTER TABLE "Santris" ADD "Kampus" character varying(100) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312123000_AddUserSecurityAndEmailVerification') THEN
    ALTER TABLE "Santris" ADD "Kelas" character varying(100) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312123000_AddUserSecurityAndEmailVerification') THEN
    ALTER TABLE "Santris" ADD "Tim" character varying(100) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312123000_AddUserSecurityAndEmailVerification') THEN
    UPDATE "Users"
    SET "Username" = COALESCE(NULLIF("Email", ''), "Id"::text)
    WHERE "Username" IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312123000_AddUserSecurityAndEmailVerification') THEN
    ALTER TABLE "Users" ALTER COLUMN "Username" TYPE character varying(100);
    ALTER TABLE "Users" ALTER COLUMN "Username" SET NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312123000_AddUserSecurityAndEmailVerification') THEN
    ALTER TABLE "Users" ALTER COLUMN "Email" TYPE character varying(200);
    ALTER TABLE "Users" ALTER COLUMN "Email" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312123000_AddUserSecurityAndEmailVerification') THEN
    CREATE TABLE "EmailVerificationCodes" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Email" character varying(200) NOT NULL,
        "CodeHash" character varying(200) NOT NULL,
        "ExpiresAtUtc" timestamp with time zone NOT NULL,
        "UsedAtUtc" timestamp with time zone,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_EmailVerificationCodes" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312123000_AddUserSecurityAndEmailVerification') THEN
    CREATE INDEX "IX_EmailVerificationCodes_UserId_Email" ON "EmailVerificationCodes" ("UserId", "Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312123000_AddUserSecurityAndEmailVerification') THEN
    CREATE UNIQUE INDEX "IX_Users_Username" ON "Users" ("Username");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312123000_AddUserSecurityAndEmailVerification') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260312123000_AddUserSecurityAndEmailVerification', '10.0.0');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313010000_AddSantriActivityModules') THEN
    CREATE TABLE "Kegiatans" (
        "Id" uuid NOT NULL,
        "Kategori" character varying(30) NOT NULL,
        "Waktu" character varying(20) NOT NULL,
        "Catatan" text,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_Kegiatans" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313010000_AddSantriActivityModules') THEN
    CREATE TABLE "Kafarahs" (
        "Id" uuid NOT NULL,
        "SantriId" uuid NOT NULL,
        "Tanggal" date NOT NULL,
        "JenisPelanggaran" character varying(100) NOT NULL,
        "Kafarah" character varying(255) NOT NULL,
        "JumlahSetor" integer NOT NULL,
        "Tanggungan" integer NOT NULL,
        "Tenggat" text,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_Kafarahs" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Kafarahs_Santris_SantriId" FOREIGN KEY ("SantriId") REFERENCES "Santris" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313010000_AddSantriActivityModules') THEN
    CREATE TABLE "LogKeluarMasuks" (
        "Id" uuid NOT NULL,
        "SantriId" uuid NOT NULL,
        "TanggalPengajuan" date NOT NULL,
        "Jenis" character varying(255) NOT NULL,
        "Rentang" character varying(255),
        "Status" character varying(30) NOT NULL,
        "Petugas" character varying(255),
        "Catatan" text,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_LogKeluarMasuks" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_LogKeluarMasuks_Santris_SantriId" FOREIGN KEY ("SantriId") REFERENCES "Santris" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313010000_AddSantriActivityModules') THEN
    CREATE TABLE "ProgressKeilmuans" (
        "Id" uuid NOT NULL,
        "SantriId" uuid NOT NULL,
        "Judul" character varying(255) NOT NULL,
        "Target" integer NOT NULL,
        "Capaian" integer NOT NULL,
        "Satuan" character varying(30),
        "Level" character varying(50),
        "Catatan" text,
        "Pembimbing" character varying(100),
        "TerakhirSetorUtc" timestamp with time zone,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_ProgressKeilmuans" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ProgressKeilmuans_Santris_SantriId" FOREIGN KEY ("SantriId") REFERENCES "Santris" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313010000_AddSantriActivityModules') THEN
    CREATE TABLE "Sesis" (
        "Id" uuid NOT NULL,
        "KegiatanId" uuid NOT NULL,
        "Tanggal" date NOT NULL,
        "Catatan" character varying(255),
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_Sesis" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Sesis_Kegiatans_KegiatanId" FOREIGN KEY ("KegiatanId") REFERENCES "Kegiatans" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313010000_AddSantriActivityModules') THEN
    CREATE TABLE "Presensis" (
        "Id" uuid NOT NULL,
        "SantriId" uuid NOT NULL,
        "Nama" character varying(200) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "KegiatanId" uuid NOT NULL,
        "SesiId" uuid,
        "Catatan" text,
        "Waktu" character varying(20) NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_Presensis" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Presensis_Kegiatans_KegiatanId" FOREIGN KEY ("KegiatanId") REFERENCES "Kegiatans" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_Presensis_Santris_SantriId" FOREIGN KEY ("SantriId") REFERENCES "Santris" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_Presensis_Sesis_SesiId" FOREIGN KEY ("SesiId") REFERENCES "Sesis" ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313010000_AddSantriActivityModules') THEN
    CREATE INDEX "IX_Kafarahs_SantriId_Tanggal" ON "Kafarahs" ("SantriId", "Tanggal");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313010000_AddSantriActivityModules') THEN
    CREATE UNIQUE INDEX "IX_Kegiatans_Kategori_Waktu" ON "Kegiatans" ("Kategori", "Waktu");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313010000_AddSantriActivityModules') THEN
    CREATE INDEX "IX_LogKeluarMasuks_SantriId_TanggalPengajuan" ON "LogKeluarMasuks" ("SantriId", "TanggalPengajuan");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313010000_AddSantriActivityModules') THEN
    CREATE INDEX "IX_LogKeluarMasuks_Status" ON "LogKeluarMasuks" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313010000_AddSantriActivityModules') THEN
    CREATE INDEX "IX_LogKeluarMasuks_TanggalPengajuan" ON "LogKeluarMasuks" ("TanggalPengajuan");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313010000_AddSantriActivityModules') THEN
    CREATE INDEX "IX_Presensis_CreatedAtUtc_LegacyNullSesi" ON "Presensis" ("CreatedAtUtc") WHERE "SesiId" IS NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313010000_AddSantriActivityModules') THEN
    CREATE INDEX "IX_Presensis_KegiatanId_Waktu" ON "Presensis" ("KegiatanId", "Waktu");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313010000_AddSantriActivityModules') THEN
    CREATE INDEX "IX_Presensis_SantriId_CreatedAtUtc" ON "Presensis" ("SantriId", "CreatedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313010000_AddSantriActivityModules') THEN
    CREATE INDEX "IX_Presensis_SantriId_Status" ON "Presensis" ("SantriId", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313010000_AddSantriActivityModules') THEN
    CREATE INDEX "IX_Presensis_SesiId_CreatedAtUtc" ON "Presensis" ("SesiId", "CreatedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313010000_AddSantriActivityModules') THEN
    CREATE INDEX "IX_Presensis_SesiId_SantriId" ON "Presensis" ("SesiId", "SantriId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313010000_AddSantriActivityModules') THEN
    CREATE INDEX "IX_Presensis_Status" ON "Presensis" ("Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313010000_AddSantriActivityModules') THEN
    CREATE INDEX "IX_Presensis_UpdatedAtUtc" ON "Presensis" ("UpdatedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313010000_AddSantriActivityModules') THEN
    CREATE INDEX "IX_ProgressKeilmuans_Level" ON "ProgressKeilmuans" ("Level");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313010000_AddSantriActivityModules') THEN
    CREATE INDEX "IX_ProgressKeilmuans_Level_SantriId" ON "ProgressKeilmuans" ("Level", "SantriId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313010000_AddSantriActivityModules') THEN
    CREATE INDEX "IX_ProgressKeilmuans_SantriId_Judul" ON "ProgressKeilmuans" ("SantriId", "Judul");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313010000_AddSantriActivityModules') THEN
    CREATE INDEX "IX_ProgressKeilmuans_SantriId_UpdatedAtUtc" ON "ProgressKeilmuans" ("SantriId", "UpdatedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313010000_AddSantriActivityModules') THEN
    CREATE INDEX "IX_Sesis_KegiatanId" ON "Sesis" ("KegiatanId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313010000_AddSantriActivityModules') THEN
    CREATE INDEX "IX_Sesis_Tanggal" ON "Sesis" ("Tanggal");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313010000_AddSantriActivityModules') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260313010000_AddSantriActivityModules', '10.0.0');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815004846_AddFaceRecognitionAttendance') THEN
    ALTER TABLE "Presensis" ADD "FaceAttendanceSessionId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815004846_AddFaceRecognitionAttendance') THEN
    ALTER TABLE "Presensis" ADD "Source" character varying(30) NOT NULL DEFAULT 'Manual';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815004846_AddFaceRecognitionAttendance') THEN
    CREATE TABLE "FaceAttendanceSessions" (
        "Id" uuid NOT NULL,
        "Kelas" character varying(100) NOT NULL,
        "Kegiatan" character varying(200) NOT NULL,
        "Waktu" character varying(20) NOT NULL,
        "Tanggal" date NOT NULL,
        "OpenerUserId" uuid NOT NULL,
        "KegiatanId" uuid NOT NULL,
        "SesiId" uuid NOT NULL,
        "Status" character varying(30) NOT NULL,
        "VerifiedAtUtc" timestamp with time zone,
        "ClosedAtUtc" timestamp with time zone,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_FaceAttendanceSessions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_FaceAttendanceSessions_Kegiatans_KegiatanId" FOREIGN KEY ("KegiatanId") REFERENCES "Kegiatans" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_FaceAttendanceSessions_Sesis_SesiId" FOREIGN KEY ("SesiId") REFERENCES "Sesis" ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_FaceAttendanceSessions_Users_OpenerUserId" FOREIGN KEY ("OpenerUserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815004846_AddFaceRecognitionAttendance') THEN
    CREATE TABLE "FaceEnrollments" (
        "Id" uuid NOT NULL,
        "SantriId" uuid NOT NULL,
        "Status" character varying(30) NOT NULL,
        "CaptureCount" integer NOT NULL,
        "RegisteredAtUtc" timestamp with time zone,
        "EmbeddingUpdatedAtUtc" timestamp with time zone,
        "RejectionReason" character varying(500),
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_FaceEnrollments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_FaceEnrollments_Santris_SantriId" FOREIGN KEY ("SantriId") REFERENCES "Santris" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815004846_AddFaceRecognitionAttendance') THEN
    CREATE TABLE "FaceProfiles" (
        "Id" uuid NOT NULL,
        "SantriId" uuid NOT NULL,
        "ProviderProfileId" character varying(200) NOT NULL,
        "EmbeddingUpdatedAtUtc" timestamp with time zone NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_FaceProfiles" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_FaceProfiles_Santris_SantriId" FOREIGN KEY ("SantriId") REFERENCES "Santris" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815004846_AddFaceRecognitionAttendance') THEN
    CREATE TABLE "FaceRecognitionEvents" (
        "Id" uuid NOT NULL,
        "SessionId" uuid NOT NULL,
        "SantriId" uuid,
        "Confidence" numeric(5,4),
        "CapturedAtUtc" timestamp with time zone NOT NULL,
        "Status" character varying(20) NOT NULL,
        "RejectionReason" character varying(500),
        "PresensiId" uuid,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_FaceRecognitionEvents" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_FaceRecognitionEvents_FaceAttendanceSessions_SessionId" FOREIGN KEY ("SessionId") REFERENCES "FaceAttendanceSessions" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_FaceRecognitionEvents_Presensis_PresensiId" FOREIGN KEY ("PresensiId") REFERENCES "Presensis" ("Id") ON DELETE SET NULL,
        CONSTRAINT "FK_FaceRecognitionEvents_Santris_SantriId" FOREIGN KEY ("SantriId") REFERENCES "Santris" ("Id") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815004846_AddFaceRecognitionAttendance') THEN
    CREATE TABLE "FaceEnrollmentCaptures" (
        "Id" uuid NOT NULL,
        "EnrollmentId" uuid NOT NULL,
        "Sequence" integer NOT NULL,
        "Pose" character varying(40) NOT NULL,
        "StorageKey" character varying(500) NOT NULL,
        "ContentType" character varying(100) NOT NULL,
        "IsValid" boolean NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_FaceEnrollmentCaptures" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_FaceEnrollmentCaptures_FaceEnrollments_EnrollmentId" FOREIGN KEY ("EnrollmentId") REFERENCES "FaceEnrollments" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815004846_AddFaceRecognitionAttendance') THEN
    CREATE UNIQUE INDEX "UX_Presensis_FaceAttendanceSessionId_SantriId" ON "Presensis" ("FaceAttendanceSessionId", "SantriId") WHERE "FaceAttendanceSessionId" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815004846_AddFaceRecognitionAttendance') THEN
    CREATE INDEX "IX_FaceAttendanceSessions_KegiatanId" ON "FaceAttendanceSessions" ("KegiatanId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815004846_AddFaceRecognitionAttendance') THEN
    CREATE INDEX "IX_FaceAttendanceSessions_OpenerUserId" ON "FaceAttendanceSessions" ("OpenerUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815004846_AddFaceRecognitionAttendance') THEN
    CREATE INDEX "IX_FaceAttendanceSessions_SesiId" ON "FaceAttendanceSessions" ("SesiId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815004846_AddFaceRecognitionAttendance') THEN
    CREATE INDEX "IX_FaceAttendanceSessions_Tanggal_Status" ON "FaceAttendanceSessions" ("Tanggal", "Status");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815004846_AddFaceRecognitionAttendance') THEN
    CREATE UNIQUE INDEX "IX_FaceEnrollmentCaptures_EnrollmentId_Sequence" ON "FaceEnrollmentCaptures" ("EnrollmentId", "Sequence");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815004846_AddFaceRecognitionAttendance') THEN
    CREATE UNIQUE INDEX "IX_FaceEnrollments_SantriId" ON "FaceEnrollments" ("SantriId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815004846_AddFaceRecognitionAttendance') THEN
    CREATE UNIQUE INDEX "IX_FaceProfiles_ProviderProfileId" ON "FaceProfiles" ("ProviderProfileId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815004846_AddFaceRecognitionAttendance') THEN
    CREATE UNIQUE INDEX "IX_FaceProfiles_SantriId" ON "FaceProfiles" ("SantriId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815004846_AddFaceRecognitionAttendance') THEN
    CREATE INDEX "IX_FaceRecognitionEvents_PresensiId" ON "FaceRecognitionEvents" ("PresensiId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815004846_AddFaceRecognitionAttendance') THEN
    CREATE INDEX "IX_FaceRecognitionEvents_SantriId" ON "FaceRecognitionEvents" ("SantriId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815004846_AddFaceRecognitionAttendance') THEN
    CREATE INDEX "IX_FaceRecognitionEvents_SessionId_CapturedAtUtc" ON "FaceRecognitionEvents" ("SessionId", "CapturedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815004846_AddFaceRecognitionAttendance') THEN
    ALTER TABLE "Presensis" ADD CONSTRAINT "FK_Presensis_FaceAttendanceSessions_FaceAttendanceSessionId" FOREIGN KEY ("FaceAttendanceSessionId") REFERENCES "FaceAttendanceSessions" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260815004846_AddFaceRecognitionAttendance') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260815004846_AddFaceRecognitionAttendance', '10.0.0');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260816081324_GeneralizeFaceEnrollmentToUsers') THEN
    ALTER TABLE "FaceEnrollments" DROP CONSTRAINT "FK_FaceEnrollments_Santris_SantriId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260816081324_GeneralizeFaceEnrollmentToUsers') THEN
    ALTER TABLE "FaceProfiles" DROP CONSTRAINT "FK_FaceProfiles_Santris_SantriId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260816081324_GeneralizeFaceEnrollmentToUsers') THEN
    UPDATE "FaceEnrollments" AS enrollment
    SET "SantriId" = santri."UserId"
    FROM "Santris" AS santri
    WHERE enrollment."SantriId" = santri."Id";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260816081324_GeneralizeFaceEnrollmentToUsers') THEN
    UPDATE "FaceProfiles" AS profile
    SET "SantriId" = santri."UserId"
    FROM "Santris" AS santri
    WHERE profile."SantriId" = santri."Id";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260816081324_GeneralizeFaceEnrollmentToUsers') THEN
    ALTER TABLE "FaceProfiles" RENAME COLUMN "SantriId" TO "UserId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260816081324_GeneralizeFaceEnrollmentToUsers') THEN
    ALTER INDEX "IX_FaceProfiles_SantriId" RENAME TO "IX_FaceProfiles_UserId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260816081324_GeneralizeFaceEnrollmentToUsers') THEN
    ALTER TABLE "FaceEnrollments" RENAME COLUMN "SantriId" TO "UserId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260816081324_GeneralizeFaceEnrollmentToUsers') THEN
    ALTER INDEX "IX_FaceEnrollments_SantriId" RENAME TO "IX_FaceEnrollments_UserId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260816081324_GeneralizeFaceEnrollmentToUsers') THEN
    ALTER TABLE "FaceEnrollments" ADD CONSTRAINT "FK_FaceEnrollments_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260816081324_GeneralizeFaceEnrollmentToUsers') THEN
    ALTER TABLE "FaceProfiles" ADD CONSTRAINT "FK_FaceProfiles_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260816081324_GeneralizeFaceEnrollmentToUsers') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260816081324_GeneralizeFaceEnrollmentToUsers', '10.0.0');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818051851_AddWaliSantriCodes') THEN
    ALTER TABLE "WaliSantriRelations" ADD "WaliSantriCode" character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818051851_AddWaliSantriCodes') THEN
    UPDATE "WaliSantriRelations" AS relation SET "WaliSantriCode" = '354' || SUBSTRING(santri."Nis" FROM 3) FROM "Santris" AS santri WHERE relation."SantriId" = santri."Id";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818051851_AddWaliSantriCodes') THEN
    ALTER TABLE "WaliSantriRelations" ALTER COLUMN "WaliSantriCode" SET NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818051851_AddWaliSantriCodes') THEN
    CREATE UNIQUE INDEX "IX_WaliSantriRelations_WaliSantriCode" ON "WaliSantriRelations" ("WaliSantriCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260818051851_AddWaliSantriCodes') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260818051851_AddWaliSantriCodes', '10.0.0');
    END IF;
END $EF$;
COMMIT;
-- End of idempotent backend migration script.
