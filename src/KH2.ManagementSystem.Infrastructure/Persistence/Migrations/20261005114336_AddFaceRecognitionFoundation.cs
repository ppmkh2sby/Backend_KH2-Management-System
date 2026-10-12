using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace KH2.ManagementSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFaceRecognitionFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FaceProfiles_Users_UserId",
                table: "FaceProfiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FaceProfiles",
                table: "FaceProfiles");

            migrationBuilder.RenameTable(
                name: "FaceProfiles",
                newName: "ProviderFaceProfiles");

            migrationBuilder.RenameIndex(
                name: "IX_FaceProfiles_UserId",
                table: "ProviderFaceProfiles",
                newName: "IX_ProviderFaceProfiles_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_FaceProfiles_ProviderProfileId",
                table: "ProviderFaceProfiles",
                newName: "IX_ProviderFaceProfiles_ProviderProfileId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProviderFaceProfiles",
                table: "ProviderFaceProfiles",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProviderFaceProfiles_Users_UserId",
                table: "ProviderFaceProfiles",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "FaceProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SantriId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ModelVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ReferenceImagePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EnrolledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastVerifiedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                name: "FaceEmbeddings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(512)", nullable: false),
                    FaceProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityScore = table.Column<float>(type: "real", nullable: true),
                    CaptureIndex = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaceEmbeddings", x => x.Id);
                    table.CheckConstraint("CK_FaceEmbeddings_CaptureIndex_Positive", "\"CaptureIndex\" > 0");
                    table.CheckConstraint("CK_FaceEmbeddings_QualityScore_Range", "\"QualityScore\" IS NULL OR (\"QualityScore\" >= 0 AND \"QualityScore\" <= 1)");
                    table.ForeignKey(
                        name: "FK_FaceEmbeddings_FaceProfiles_FaceProfileId",
                        column: x => x.FaceProfileId,
                        principalTable: "FaceProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FaceProfiles_SantriId",
                table: "FaceProfiles",
                column: "SantriId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FaceEmbeddings_FaceProfileId",
                table: "FaceEmbeddings",
                column: "FaceProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FaceEmbeddings");

            migrationBuilder.DropTable(
                name: "FaceProfiles");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.DropForeignKey(
                name: "FK_ProviderFaceProfiles_Users_UserId",
                table: "ProviderFaceProfiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProviderFaceProfiles",
                table: "ProviderFaceProfiles");

            migrationBuilder.RenameTable(
                name: "ProviderFaceProfiles",
                newName: "FaceProfiles");

            migrationBuilder.RenameIndex(
                name: "IX_ProviderFaceProfiles_UserId",
                table: "FaceProfiles",
                newName: "IX_FaceProfiles_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ProviderFaceProfiles_ProviderProfileId",
                table: "FaceProfiles",
                newName: "IX_FaceProfiles_ProviderProfileId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FaceProfiles",
                table: "FaceProfiles",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FaceProfiles_Users_UserId",
                table: "FaceProfiles",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
