using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KH2.ManagementSystem.Infrastructure.Persistence.Migrations
{
    public partial class ImplementTrustedFaceAttendance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Preserve the pre-existing user-operated attendance audit trail before
            // establishing the architecture's device-recognition event schema.
            migrationBuilder.DropForeignKey(name: "FK_FaceRecognitionEvents_FaceAttendanceSessions_SessionId", table: "FaceRecognitionEvents");
            migrationBuilder.DropForeignKey(name: "FK_FaceRecognitionEvents_Presensis_PresensiId", table: "FaceRecognitionEvents");
            migrationBuilder.DropForeignKey(name: "FK_FaceRecognitionEvents_Santris_SantriId", table: "FaceRecognitionEvents");
            migrationBuilder.RenameTable(name: "FaceRecognitionEvents", newName: "LegacyFaceRecognitionEvents");
            migrationBuilder.Sql("ALTER TABLE \"LegacyFaceRecognitionEvents\" RENAME CONSTRAINT \"PK_FaceRecognitionEvents\" TO \"PK_LegacyFaceRecognitionEvents\";");
            migrationBuilder.RenameIndex(name: "IX_FaceRecognitionEvents_PresensiId", table: "LegacyFaceRecognitionEvents", newName: "IX_LegacyFaceRecognitionEvents_PresensiId");
            migrationBuilder.RenameIndex(name: "IX_FaceRecognitionEvents_SantriId", table: "LegacyFaceRecognitionEvents", newName: "IX_LegacyFaceRecognitionEvents_SantriId");
            migrationBuilder.RenameIndex(name: "IX_FaceRecognitionEvents_SessionId_CapturedAtUtc", table: "LegacyFaceRecognitionEvents", newName: "IX_LegacyFaceRecognitionEvents_SessionId_CapturedAtUtc");
            migrationBuilder.AddForeignKey(name: "FK_LegacyFaceRecognitionEvents_FaceAttendanceSessions_SessionId", table: "LegacyFaceRecognitionEvents", column: "SessionId", principalTable: "FaceAttendanceSessions", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_LegacyFaceRecognitionEvents_Presensis_PresensiId", table: "LegacyFaceRecognitionEvents", column: "PresensiId", principalTable: "Presensis", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
            migrationBuilder.AddForeignKey(name: "FK_LegacyFaceRecognitionEvents_Santris_SantriId", table: "LegacyFaceRecognitionEvents", column: "SantriId", principalTable: "Santris", principalColumn: "Id", onDelete: ReferentialAction.SetNull);

            migrationBuilder.DropIndex(name: "IX_Presensis_SesiId_SantriId", table: "Presensis");
            migrationBuilder.CreateIndex(
                name: "UX_Presensis_SesiId_SantriId",
                table: "Presensis",
                columns: new[] { "SesiId", "SantriId" },
                unique: true,
                filter: "\"SesiId\" IS NOT NULL");

            migrationBuilder.CreateTable(
                name: "AttendanceDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    LocationLabel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ApiKeyHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastSeenAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_AttendanceDevices", x => x.Id));

            migrationBuilder.CreateTable(
                name: "FaceRecognitionEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    FaceProfileId = table.Column<Guid>(type: "uuid", nullable: true),
                    FaceEnrollmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    SantriId = table.Column<Guid>(type: "uuid", nullable: true),
                    SesiId = table.Column<Guid>(type: "uuid", nullable: true),
                    PresensiId = table.Column<Guid>(type: "uuid", nullable: true),
                    Recognized = table.Column<bool>(type: "boolean", nullable: false),
                    Similarity = table.Column<double>(type: "double precision", nullable: true),
                    Distance = table.Column<double>(type: "double precision", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProcessingDurationMs = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaceRecognitionEvents", x => x.Id);
                    table.ForeignKey(name: "FK_FaceRecognitionEvents_AttendanceDevices_DeviceId", column: x => x.DeviceId, principalTable: "AttendanceDevices", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(name: "FK_FaceRecognitionEvents_FaceEnrollments_FaceEnrollmentId", column: x => x.FaceEnrollmentId, principalTable: "FaceEnrollments", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(name: "FK_FaceRecognitionEvents_FaceProfiles_FaceProfileId", column: x => x.FaceProfileId, principalTable: "FaceProfiles", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(name: "FK_FaceRecognitionEvents_Presensis_PresensiId", column: x => x.PresensiId, principalTable: "Presensis", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(name: "FK_FaceRecognitionEvents_Santris_SantriId", column: x => x.SantriId, principalTable: "Santris", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(name: "FK_FaceRecognitionEvents_Sesis_SesiId", column: x => x.SesiId, principalTable: "Sesis", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(name: "IX_AttendanceDevices_IsActive", table: "AttendanceDevices", column: "IsActive");
            migrationBuilder.CreateIndex(name: "IX_AttendanceDevices_Name", table: "AttendanceDevices", column: "Name", unique: true);
            migrationBuilder.CreateIndex(name: "IX_FaceRecognitionEvents_DeviceId_CreatedAtUtc", table: "FaceRecognitionEvents", columns: new[] { "DeviceId", "CreatedAtUtc" });
            migrationBuilder.CreateIndex(name: "IX_FaceRecognitionEvents_FaceEnrollmentId", table: "FaceRecognitionEvents", column: "FaceEnrollmentId");
            migrationBuilder.CreateIndex(name: "IX_FaceRecognitionEvents_FaceProfileId", table: "FaceRecognitionEvents", column: "FaceProfileId");
            migrationBuilder.CreateIndex(name: "IX_FaceRecognitionEvents_PresensiId", table: "FaceRecognitionEvents", column: "PresensiId");
            migrationBuilder.CreateIndex(name: "IX_FaceRecognitionEvents_SantriId", table: "FaceRecognitionEvents", column: "SantriId");
            migrationBuilder.CreateIndex(name: "IX_FaceRecognitionEvents_SesiId_SantriId", table: "FaceRecognitionEvents", columns: new[] { "SesiId", "SantriId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "FaceRecognitionEvents");
            migrationBuilder.DropTable(name: "AttendanceDevices");
            migrationBuilder.DropIndex(name: "UX_Presensis_SesiId_SantriId", table: "Presensis");
            migrationBuilder.CreateIndex(name: "IX_Presensis_SesiId_SantriId", table: "Presensis", columns: new[] { "SesiId", "SantriId" });
            migrationBuilder.DropForeignKey(name: "FK_LegacyFaceRecognitionEvents_FaceAttendanceSessions_SessionId", table: "LegacyFaceRecognitionEvents");
            migrationBuilder.DropForeignKey(name: "FK_LegacyFaceRecognitionEvents_Presensis_PresensiId", table: "LegacyFaceRecognitionEvents");
            migrationBuilder.DropForeignKey(name: "FK_LegacyFaceRecognitionEvents_Santris_SantriId", table: "LegacyFaceRecognitionEvents");
            migrationBuilder.RenameIndex(name: "IX_LegacyFaceRecognitionEvents_PresensiId", table: "LegacyFaceRecognitionEvents", newName: "IX_FaceRecognitionEvents_PresensiId");
            migrationBuilder.RenameIndex(name: "IX_LegacyFaceRecognitionEvents_SantriId", table: "LegacyFaceRecognitionEvents", newName: "IX_FaceRecognitionEvents_SantriId");
            migrationBuilder.RenameIndex(name: "IX_LegacyFaceRecognitionEvents_SessionId_CapturedAtUtc", table: "LegacyFaceRecognitionEvents", newName: "IX_FaceRecognitionEvents_SessionId_CapturedAtUtc");
            migrationBuilder.RenameTable(name: "LegacyFaceRecognitionEvents", newName: "FaceRecognitionEvents");
            migrationBuilder.Sql("ALTER TABLE \"FaceRecognitionEvents\" RENAME CONSTRAINT \"PK_LegacyFaceRecognitionEvents\" TO \"PK_FaceRecognitionEvents\";");
            migrationBuilder.AddForeignKey(name: "FK_FaceRecognitionEvents_FaceAttendanceSessions_SessionId", table: "FaceRecognitionEvents", column: "SessionId", principalTable: "FaceAttendanceSessions", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            migrationBuilder.AddForeignKey(name: "FK_FaceRecognitionEvents_Presensis_PresensiId", table: "FaceRecognitionEvents", column: "PresensiId", principalTable: "Presensis", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
            migrationBuilder.AddForeignKey(name: "FK_FaceRecognitionEvents_Santris_SantriId", table: "FaceRecognitionEvents", column: "SantriId", principalTable: "Santris", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
        }
    }
}
