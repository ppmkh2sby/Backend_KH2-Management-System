using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KH2.ManagementSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFaceRecognitionAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FaceAttendanceSessionId",
                table: "Presensis",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Presensis",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.CreateTable(
                name: "FaceAttendanceSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kelas = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Kegiatan = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Waktu = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Tanggal = table.Column<DateOnly>(type: "date", nullable: false),
                    OpenerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    KegiatanId = table.Column<Guid>(type: "uuid", nullable: false),
                    SesiId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    VerifiedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaceAttendanceSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaceAttendanceSessions_Kegiatans_KegiatanId",
                        column: x => x.KegiatanId,
                        principalTable: "Kegiatans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FaceAttendanceSessions_Sesis_SesiId",
                        column: x => x.SesiId,
                        principalTable: "Sesis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FaceAttendanceSessions_Users_OpenerUserId",
                        column: x => x.OpenerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FaceEnrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SantriId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CaptureCount = table.Column<int>(type: "integer", nullable: false),
                    RegisteredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EmbeddingUpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaceEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaceEnrollments_Santris_SantriId",
                        column: x => x.SantriId,
                        principalTable: "Santris",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FaceProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SantriId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderProfileId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EmbeddingUpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaceProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaceProfiles_Santris_SantriId",
                        column: x => x.SantriId,
                        principalTable: "Santris",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FaceRecognitionEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SantriId = table.Column<Guid>(type: "uuid", nullable: true),
                    Confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    CapturedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PresensiId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaceRecognitionEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaceRecognitionEvents_FaceAttendanceSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "FaceAttendanceSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FaceRecognitionEvents_Presensis_PresensiId",
                        column: x => x.PresensiId,
                        principalTable: "Presensis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FaceRecognitionEvents_Santris_SantriId",
                        column: x => x.SantriId,
                        principalTable: "Santris",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FaceEnrollmentCaptures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Pose = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsValid = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaceEnrollmentCaptures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaceEnrollmentCaptures_FaceEnrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "FaceEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UX_Presensis_FaceAttendanceSessionId_SantriId",
                table: "Presensis",
                columns: new[] { "FaceAttendanceSessionId", "SantriId" },
                unique: true,
                filter: "\"FaceAttendanceSessionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FaceAttendanceSessions_KegiatanId",
                table: "FaceAttendanceSessions",
                column: "KegiatanId");

            migrationBuilder.CreateIndex(
                name: "IX_FaceAttendanceSessions_OpenerUserId",
                table: "FaceAttendanceSessions",
                column: "OpenerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FaceAttendanceSessions_SesiId",
                table: "FaceAttendanceSessions",
                column: "SesiId");

            migrationBuilder.CreateIndex(
                name: "IX_FaceAttendanceSessions_Tanggal_Status",
                table: "FaceAttendanceSessions",
                columns: new[] { "Tanggal", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FaceEnrollmentCaptures_EnrollmentId_Sequence",
                table: "FaceEnrollmentCaptures",
                columns: new[] { "EnrollmentId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FaceEnrollments_SantriId",
                table: "FaceEnrollments",
                column: "SantriId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FaceProfiles_ProviderProfileId",
                table: "FaceProfiles",
                column: "ProviderProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FaceProfiles_SantriId",
                table: "FaceProfiles",
                column: "SantriId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FaceRecognitionEvents_PresensiId",
                table: "FaceRecognitionEvents",
                column: "PresensiId");

            migrationBuilder.CreateIndex(
                name: "IX_FaceRecognitionEvents_SantriId",
                table: "FaceRecognitionEvents",
                column: "SantriId");

            migrationBuilder.CreateIndex(
                name: "IX_FaceRecognitionEvents_SessionId_CapturedAtUtc",
                table: "FaceRecognitionEvents",
                columns: new[] { "SessionId", "CapturedAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_Presensis_FaceAttendanceSessions_FaceAttendanceSessionId",
                table: "Presensis",
                column: "FaceAttendanceSessionId",
                principalTable: "FaceAttendanceSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Presensis_FaceAttendanceSessions_FaceAttendanceSessionId",
                table: "Presensis");

            migrationBuilder.DropTable(
                name: "FaceEnrollmentCaptures");

            migrationBuilder.DropTable(
                name: "FaceProfiles");

            migrationBuilder.DropTable(
                name: "FaceRecognitionEvents");

            migrationBuilder.DropTable(
                name: "FaceEnrollments");

            migrationBuilder.DropTable(
                name: "FaceAttendanceSessions");

            migrationBuilder.DropIndex(
                name: "UX_Presensis_FaceAttendanceSessionId_SantriId",
                table: "Presensis");

            migrationBuilder.DropColumn(
                name: "FaceAttendanceSessionId",
                table: "Presensis");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Presensis");
        }
    }
}
