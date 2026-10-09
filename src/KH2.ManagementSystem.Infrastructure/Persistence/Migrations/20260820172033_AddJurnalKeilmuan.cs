using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KH2.ManagementSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJurnalKeilmuan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JurnalKeilmuans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Tanggal = table.Column<DateOnly>(type: "date", nullable: false),
                    SesiSambung = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Kelas = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NamaDewanGuru = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    JamMengajar = table.Column<decimal>(type: "numeric", nullable: false),
                    AdaMateri = table.Column<bool>(type: "boolean", nullable: true),
                    JenisMateri = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DibuatOlehUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DibuatOleh = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    QuranTargetSurahId = table.Column<Guid>(type: "uuid", nullable: true),
                    QuranAyatAwal = table.Column<int>(type: "integer", nullable: true),
                    QuranAyatTarget = table.Column<int>(type: "integer", nullable: true),
                    QuranRealisasiSurahId = table.Column<Guid>(type: "uuid", nullable: true),
                    QuranAyatRealisasi = table.Column<int>(type: "integer", nullable: true),
                    QuranKeterangan = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DetailsJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JurnalKeilmuans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JurnalKeilmuans_QuranSurahs_QuranRealisasiSurahId",
                        column: x => x.QuranRealisasiSurahId,
                        principalTable: "QuranSurahs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JurnalKeilmuans_QuranSurahs_QuranTargetSurahId",
                        column: x => x.QuranTargetSurahId,
                        principalTable: "QuranSurahs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JurnalKeilmuans_QuranRealisasiSurahId",
                table: "JurnalKeilmuans",
                column: "QuranRealisasiSurahId");

            migrationBuilder.CreateIndex(
                name: "IX_JurnalKeilmuans_QuranTargetSurahId",
                table: "JurnalKeilmuans",
                column: "QuranTargetSurahId");

            migrationBuilder.CreateIndex(
                name: "IX_JurnalKeilmuans_Tanggal",
                table: "JurnalKeilmuans",
                column: "Tanggal");

            migrationBuilder.CreateIndex(
                name: "IX_JurnalKeilmuans_Tanggal_JenisMateri",
                table: "JurnalKeilmuans",
                columns: new[] { "Tanggal", "JenisMateri" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JurnalKeilmuans");
        }
    }
}
