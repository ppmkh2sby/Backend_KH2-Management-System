using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KH2.ManagementSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EstablishFaceEnrollmentGenerations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FaceEmbeddings_FaceProfiles_FaceProfileId",
                table: "FaceEmbeddings");

            migrationBuilder.DropForeignKey(
                name: "FK_FaceEnrollments_Users_UserId",
                table: "FaceEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_FaceEnrollments_UserId",
                table: "FaceEnrollments");

            migrationBuilder.DropColumn(
                name: "EnrolledAtUtc",
                table: "FaceProfiles");

            migrationBuilder.DropColumn(
                name: "ModelName",
                table: "FaceProfiles");

            migrationBuilder.DropColumn(
                name: "ModelVersion",
                table: "FaceProfiles");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "FaceEnrollments");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "FaceEnrollments",
                newName: "FaceProfileId");

            migrationBuilder.RenameColumn(
                name: "RegisteredAtUtc",
                table: "FaceEnrollments",
                newName: "SupersededAtUtc");

            migrationBuilder.RenameColumn(
                name: "EmbeddingUpdatedAtUtc",
                table: "FaceEnrollments",
                newName: "ActivatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CaptureCount",
                table: "FaceEnrollments",
                newName: "AcceptedSampleCount");

            migrationBuilder.RenameColumn(
                name: "FaceProfileId",
                table: "FaceEmbeddings",
                newName: "FaceEnrollmentId");

            migrationBuilder.RenameIndex(
                name: "IX_FaceEmbeddings_FaceProfileId",
                table: "FaceEmbeddings",
                newName: "IX_FaceEmbeddings_FaceEnrollmentId");

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentEnrollmentId",
                table: "FaceProfiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EnrolledAtUtc",
                table: "FaceEnrollments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "ModelName",
                table: "FaceEnrollments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ModelVersion",
                table: "FaceEnrollments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "LegacyFaceEnrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_LegacyFaceEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LegacyFaceEnrollments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LegacyFaceEnrollmentCaptures",
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
                    table.PrimaryKey("PK_LegacyFaceEnrollmentCaptures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LegacyFaceEnrollmentCaptures_LegacyFaceEnrollments_Enrollme~",
                        column: x => x.EnrollmentId,
                        principalTable: "LegacyFaceEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FaceProfiles_CurrentEnrollmentId",
                table: "FaceProfiles",
                column: "CurrentEnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_FaceEnrollments_FaceProfileId",
                table: "FaceEnrollments",
                column: "FaceProfileId");

            migrationBuilder.CreateIndex(
                name: "UX_FaceEnrollments_Active_FaceProfile",
                table: "FaceEnrollments",
                columns: new[] { "FaceProfileId", "Status" },
                unique: true,
                filter: "\"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_LegacyFaceEnrollmentCaptures_EnrollmentId_Sequence",
                table: "LegacyFaceEnrollmentCaptures",
                columns: new[] { "EnrollmentId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegacyFaceEnrollments_UserId",
                table: "LegacyFaceEnrollments",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FaceEmbeddings_FaceEnrollments_FaceEnrollmentId",
                table: "FaceEmbeddings",
                column: "FaceEnrollmentId",
                principalTable: "FaceEnrollments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FaceEnrollments_FaceProfiles_FaceProfileId",
                table: "FaceEnrollments",
                column: "FaceProfileId",
                principalTable: "FaceProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FaceProfiles_FaceEnrollments_CurrentEnrollmentId",
                table: "FaceProfiles",
                column: "CurrentEnrollmentId",
                principalTable: "FaceEnrollments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FaceEmbeddings_FaceEnrollments_FaceEnrollmentId",
                table: "FaceEmbeddings");

            migrationBuilder.DropForeignKey(
                name: "FK_FaceEnrollments_FaceProfiles_FaceProfileId",
                table: "FaceEnrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_FaceProfiles_FaceEnrollments_CurrentEnrollmentId",
                table: "FaceProfiles");

            migrationBuilder.DropTable(
                name: "LegacyFaceEnrollmentCaptures");

            migrationBuilder.DropTable(
                name: "LegacyFaceEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_FaceProfiles_CurrentEnrollmentId",
                table: "FaceProfiles");

            migrationBuilder.DropIndex(
                name: "IX_FaceEnrollments_FaceProfileId",
                table: "FaceEnrollments");

            migrationBuilder.DropIndex(
                name: "UX_FaceEnrollments_Active_FaceProfile",
                table: "FaceEnrollments");

            migrationBuilder.DropColumn(
                name: "CurrentEnrollmentId",
                table: "FaceProfiles");

            migrationBuilder.DropColumn(
                name: "EnrolledAtUtc",
                table: "FaceEnrollments");

            migrationBuilder.DropColumn(
                name: "ModelName",
                table: "FaceEnrollments");

            migrationBuilder.DropColumn(
                name: "ModelVersion",
                table: "FaceEnrollments");

            migrationBuilder.RenameColumn(
                name: "SupersededAtUtc",
                table: "FaceEnrollments",
                newName: "RegisteredAtUtc");

            migrationBuilder.RenameColumn(
                name: "FaceProfileId",
                table: "FaceEnrollments",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "ActivatedAtUtc",
                table: "FaceEnrollments",
                newName: "EmbeddingUpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "AcceptedSampleCount",
                table: "FaceEnrollments",
                newName: "CaptureCount");

            migrationBuilder.RenameColumn(
                name: "FaceEnrollmentId",
                table: "FaceEmbeddings",
                newName: "FaceProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_FaceEmbeddings_FaceEnrollmentId",
                table: "FaceEmbeddings",
                newName: "IX_FaceEmbeddings_FaceProfileId");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EnrolledAtUtc",
                table: "FaceProfiles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "ModelName",
                table: "FaceProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ModelVersion",
                table: "FaceProfiles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "FaceEnrollments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FaceEnrollments_UserId",
                table: "FaceEnrollments",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FaceEmbeddings_FaceProfiles_FaceProfileId",
                table: "FaceEmbeddings",
                column: "FaceProfileId",
                principalTable: "FaceProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FaceEnrollments_Users_UserId",
                table: "FaceEnrollments",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
