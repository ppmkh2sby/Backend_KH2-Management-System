using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KH2.ManagementSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FinalizeCanonicalFaceRecognitionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add canonical fields before removing their legacy sources so every
            // value can be copied in the same forward-only migration.
            migrationBuilder.AddColumn<string>(name: "AttendanceOutcome", table: "FaceRecognitionEvents", type: "character varying(30)", maxLength: 30, nullable: true);
            migrationBuilder.AddColumn<string>(name: "RecognitionOutcome", table: "FaceRecognitionEvents", type: "character varying(30)", maxLength: 30, nullable: true);
            migrationBuilder.AddColumn<string>(name: "Source", table: "FaceRecognitionEvents", type: "character varying(30)", maxLength: 30, nullable: true);
            migrationBuilder.AddColumn<string>(name: "ReferenceImagePath", table: "FaceEnrollments", type: "character varying(500)", maxLength: 500, nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(name: "KeyRotatedAtUtc", table: "AttendanceDevices", type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<int>(name: "KeyVersion", table: "AttendanceDevices", type: "integer", nullable: true);

            // CurrentEnrollmentId is the only unambiguous owner of the old
            // profile-level reference image, so copy it only to that enrollment.
            migrationBuilder.Sql("""
                UPDATE "FaceEnrollments" AS enrollment
                SET "ReferenceImagePath" = profile."ReferenceImagePath"
                FROM "FaceProfiles" AS profile
                WHERE profile."CurrentEnrollmentId" = enrollment."Id"
                  AND profile."ReferenceImagePath" IS NOT NULL;
                """);

            // Existing rows were written exclusively by the trusted-device
            // attendance endpoint before outcomes were split into two enums.
            // These strings are the exact canonical enum storage values.
            migrationBuilder.Sql("""
                UPDATE "FaceRecognitionEvents"
                SET
                    "Source" = 'AttendanceDevice',
                    "RecognitionOutcome" = CASE
                        WHEN "Recognized" THEN 'Recognized'
                        WHEN "FailureReason" = 'InvalidSession' THEN 'NotAttempted'
                        WHEN "FailureReason" = 'UnknownFace' THEN 'Unknown'
                        WHEN "FailureReason" IN ('AmbiguousMatch', 'InactiveFaceProfile', 'InactiveFaceEnrollment', 'SantriNotFound') THEN 'Rejected'
                        ELSE 'Error'
                    END,
                    "AttendanceOutcome" = CASE
                        WHEN "Recognized" AND "FailureReason" = 'DuplicateAttendance' THEN 'Duplicate'
                        WHEN "Recognized" THEN 'Recorded'
                        WHEN "FailureReason" = 'InvalidSession' THEN 'InvalidSession'
                        WHEN "FailureReason" IN ('UnknownFace', 'AmbiguousMatch', 'InactiveFaceProfile', 'InactiveFaceEnrollment', 'SantriNotFound') THEN 'NotAttempted'
                        ELSE 'Failed'
                    END;
                """);

            // Persisted trusted devices predate rotation support and are version
            // one, not an implicit version zero.
            migrationBuilder.Sql("UPDATE \"AttendanceDevices\" SET \"KeyVersion\" = 1;");

            migrationBuilder.AlterColumn<string>(name: "AttendanceOutcome", table: "FaceRecognitionEvents", type: "character varying(30)", maxLength: 30, nullable: false, oldClrType: typeof(string), oldType: "character varying(30)", oldMaxLength: 30, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "RecognitionOutcome", table: "FaceRecognitionEvents", type: "character varying(30)", maxLength: 30, nullable: false, oldClrType: typeof(string), oldType: "character varying(30)", oldMaxLength: 30, oldNullable: true);
            migrationBuilder.AlterColumn<string>(name: "Source", table: "FaceRecognitionEvents", type: "character varying(30)", maxLength: 30, nullable: false, oldClrType: typeof(string), oldType: "character varying(30)", oldMaxLength: 30, oldNullable: true);
            migrationBuilder.AlterColumn<int>(name: "KeyVersion", table: "AttendanceDevices", type: "integer", nullable: false, oldClrType: typeof(int), oldType: "integer", oldNullable: true);

            migrationBuilder.DropForeignKey(name: "FK_FaceProfiles_FaceEnrollments_CurrentEnrollmentId", table: "FaceProfiles");
            migrationBuilder.DropIndex(name: "IX_FaceProfiles_CurrentEnrollmentId", table: "FaceProfiles");
            migrationBuilder.DropIndex(name: "IX_FaceEmbeddings_FaceEnrollmentId", table: "FaceEmbeddings");
            migrationBuilder.DropCheckConstraint(name: "CK_FaceEmbeddings_CaptureIndex_Positive", table: "FaceEmbeddings");
            migrationBuilder.DropColumn(name: "Recognized", table: "FaceRecognitionEvents");
            migrationBuilder.DropColumn(name: "CurrentEnrollmentId", table: "FaceProfiles");
            migrationBuilder.DropColumn(name: "ReferenceImagePath", table: "FaceProfiles");
            migrationBuilder.DropColumn(name: "AcceptedSampleCount", table: "FaceEnrollments");
            migrationBuilder.DropColumn(name: "IsActive", table: "FaceEmbeddings");

            migrationBuilder.CreateIndex(name: "IX_FaceEmbeddings_FaceEnrollmentId_CaptureIndex", table: "FaceEmbeddings", columns: new[] { "FaceEnrollmentId", "CaptureIndex" }, unique: true);
            migrationBuilder.AddCheckConstraint(name: "CK_FaceEmbeddings_CaptureIndex_Range", table: "FaceEmbeddings", sql: "\"CaptureIndex\" >= 1 AND \"CaptureIndex\" <= 5");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate the prior representation first, while canonical data is
            // still available to reconstruct it.
            migrationBuilder.AddColumn<bool>(name: "Recognized", table: "FaceRecognitionEvents", type: "boolean", nullable: true);
            migrationBuilder.AddColumn<Guid>(name: "CurrentEnrollmentId", table: "FaceProfiles", type: "uuid", nullable: true);
            migrationBuilder.AddColumn<string>(name: "ReferenceImagePath", table: "FaceProfiles", type: "character varying(500)", maxLength: 500, nullable: true);
            migrationBuilder.AddColumn<int>(name: "AcceptedSampleCount", table: "FaceEnrollments", type: "integer", nullable: true);
            migrationBuilder.AddColumn<bool>(name: "IsActive", table: "FaceEmbeddings", type: "boolean", nullable: true);

            migrationBuilder.Sql("""
                UPDATE "FaceRecognitionEvents"
                SET "Recognized" = "RecognitionOutcome" = 'Recognized';

                UPDATE "FaceProfiles" AS profile
                SET
                    "CurrentEnrollmentId" = enrollment."Id",
                    "ReferenceImagePath" = enrollment."ReferenceImagePath"
                FROM "FaceEnrollments" AS enrollment
                WHERE enrollment."FaceProfileId" = profile."Id"
                  AND enrollment."Status" = 'Active';

                UPDATE "FaceEnrollments" AS enrollment
                SET "AcceptedSampleCount" = (
                    SELECT COUNT(*)::integer
                    FROM "FaceEmbeddings" AS embedding
                    WHERE embedding."FaceEnrollmentId" = enrollment."Id"
                );

                UPDATE "FaceEmbeddings" AS embedding
                SET "IsActive" = EXISTS (
                    SELECT 1
                    FROM "FaceEnrollments" AS enrollment
                    WHERE enrollment."Id" = embedding."FaceEnrollmentId"
                      AND enrollment."Status" = 'Active'
                );
                """);

            migrationBuilder.AlterColumn<bool>(name: "Recognized", table: "FaceRecognitionEvents", type: "boolean", nullable: false, oldClrType: typeof(bool), oldType: "boolean", oldNullable: true);
            migrationBuilder.AlterColumn<int>(name: "AcceptedSampleCount", table: "FaceEnrollments", type: "integer", nullable: false, oldClrType: typeof(int), oldType: "integer", oldNullable: true);
            migrationBuilder.AlterColumn<bool>(name: "IsActive", table: "FaceEmbeddings", type: "boolean", nullable: false, oldClrType: typeof(bool), oldType: "boolean", oldNullable: true);

            migrationBuilder.DropIndex(name: "IX_FaceEmbeddings_FaceEnrollmentId_CaptureIndex", table: "FaceEmbeddings");
            migrationBuilder.DropCheckConstraint(name: "CK_FaceEmbeddings_CaptureIndex_Range", table: "FaceEmbeddings");
            migrationBuilder.DropColumn(name: "AttendanceOutcome", table: "FaceRecognitionEvents");
            migrationBuilder.DropColumn(name: "RecognitionOutcome", table: "FaceRecognitionEvents");
            migrationBuilder.DropColumn(name: "Source", table: "FaceRecognitionEvents");
            migrationBuilder.DropColumn(name: "ReferenceImagePath", table: "FaceEnrollments");
            migrationBuilder.DropColumn(name: "KeyRotatedAtUtc", table: "AttendanceDevices");
            migrationBuilder.DropColumn(name: "KeyVersion", table: "AttendanceDevices");

            migrationBuilder.CreateIndex(name: "IX_FaceProfiles_CurrentEnrollmentId", table: "FaceProfiles", column: "CurrentEnrollmentId");
            migrationBuilder.CreateIndex(name: "IX_FaceEmbeddings_FaceEnrollmentId", table: "FaceEmbeddings", column: "FaceEnrollmentId");
            migrationBuilder.AddCheckConstraint(name: "CK_FaceEmbeddings_CaptureIndex_Positive", table: "FaceEmbeddings", sql: "\"CaptureIndex\" > 0");
            migrationBuilder.AddForeignKey(name: "FK_FaceProfiles_FaceEnrollments_CurrentEnrollmentId", table: "FaceProfiles", column: "CurrentEnrollmentId", principalTable: "FaceEnrollments", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        }
    }
}
