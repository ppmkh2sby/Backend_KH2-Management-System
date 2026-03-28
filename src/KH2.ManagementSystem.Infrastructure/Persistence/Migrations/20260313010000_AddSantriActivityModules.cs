using System;
using KH2.ManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KH2.ManagementSystem.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260313010000_AddSantriActivityModules")]
    public partial class AddSantriActivityModules : Migration
    {
        private static readonly string[] KegiatanUniqueColumns = ["Kategori", "Waktu"];
        private static readonly string[] KafarahSantriTanggalColumns = ["SantriId", "Tanggal"];
        private static readonly string[] ProgressSantriJudulColumns = ["SantriId", "Judul"];
        private static readonly string[] ProgressSantriUpdatedColumns = ["SantriId", "UpdatedAtUtc"];
        private static readonly string[] ProgressLevelSantriColumns = ["Level", "SantriId"];
        private static readonly string[] LogSantriTanggalColumns = ["SantriId", "TanggalPengajuan"];
        private static readonly string[] PresensiSantriStatusColumns = ["SantriId", "Status"];
        private static readonly string[] PresensiSantriCreatedColumns = ["SantriId", "CreatedAtUtc"];
        private static readonly string[] PresensiSesiSantriColumns = ["SesiId", "SantriId"];
        private static readonly string[] PresensiSesiCreatedColumns = ["SesiId", "CreatedAtUtc"];
        private static readonly string[] PresensiKegiatanWaktuColumns = ["KegiatanId", "Waktu"];

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Kegiatans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kategori = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Waktu = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Catatan = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kegiatans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Kafarahs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SantriId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tanggal = table.Column<DateOnly>(type: "date", nullable: false),
                    JenisPelanggaran = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Kafarah = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    JumlahSetor = table.Column<int>(type: "integer", nullable: false),
                    Tanggungan = table.Column<int>(type: "integer", nullable: false),
                    Tenggat = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kafarahs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Kafarahs_Santris_SantriId",
                        column: x => x.SantriId,
                        principalTable: "Santris",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LogKeluarMasuks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SantriId = table.Column<Guid>(type: "uuid", nullable: false),
                    TanggalPengajuan = table.Column<DateOnly>(type: "date", nullable: false),
                    Jenis = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Rentang = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Petugas = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Catatan = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogKeluarMasuks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LogKeluarMasuks_Santris_SantriId",
                        column: x => x.SantriId,
                        principalTable: "Santris",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgressKeilmuans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SantriId = table.Column<Guid>(type: "uuid", nullable: false),
                    Judul = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Target = table.Column<int>(type: "integer", nullable: false),
                    Capaian = table.Column<int>(type: "integer", nullable: false),
                    Satuan = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Catatan = table.Column<string>(type: "text", nullable: true),
                    Pembimbing = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TerakhirSetorUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressKeilmuans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgressKeilmuans_Santris_SantriId",
                        column: x => x.SantriId,
                        principalTable: "Santris",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sesis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KegiatanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tanggal = table.Column<DateOnly>(type: "date", nullable: false),
                    Catatan = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sesis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sesis_Kegiatans_KegiatanId",
                        column: x => x.KegiatanId,
                        principalTable: "Kegiatans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Presensis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SantriId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nama = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    KegiatanId = table.Column<Guid>(type: "uuid", nullable: false),
                    SesiId = table.Column<Guid>(type: "uuid", nullable: true),
                    Catatan = table.Column<string>(type: "text", nullable: true),
                    Waktu = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Presensis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Presensis_Kegiatans_KegiatanId",
                        column: x => x.KegiatanId,
                        principalTable: "Kegiatans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Presensis_Santris_SantriId",
                        column: x => x.SantriId,
                        principalTable: "Santris",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Presensis_Sesis_SesiId",
                        column: x => x.SesiId,
                        principalTable: "Sesis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Kafarahs_SantriId_Tanggal",
                table: "Kafarahs",
                columns: KafarahSantriTanggalColumns);

            migrationBuilder.CreateIndex(
                name: "IX_Kegiatans_Kategori_Waktu",
                table: "Kegiatans",
                columns: KegiatanUniqueColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LogKeluarMasuks_SantriId_TanggalPengajuan",
                table: "LogKeluarMasuks",
                columns: LogSantriTanggalColumns);

            migrationBuilder.CreateIndex(
                name: "IX_LogKeluarMasuks_Status",
                table: "LogKeluarMasuks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LogKeluarMasuks_TanggalPengajuan",
                table: "LogKeluarMasuks",
                column: "TanggalPengajuan");

            migrationBuilder.CreateIndex(
                name: "IX_Presensis_CreatedAtUtc_LegacyNullSesi",
                table: "Presensis",
                column: "CreatedAtUtc",
                filter: "\"SesiId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Presensis_KegiatanId_Waktu",
                table: "Presensis",
                columns: PresensiKegiatanWaktuColumns);

            migrationBuilder.CreateIndex(
                name: "IX_Presensis_SantriId_CreatedAtUtc",
                table: "Presensis",
                columns: PresensiSantriCreatedColumns);

            migrationBuilder.CreateIndex(
                name: "IX_Presensis_SantriId_Status",
                table: "Presensis",
                columns: PresensiSantriStatusColumns);

            migrationBuilder.CreateIndex(
                name: "IX_Presensis_SesiId_CreatedAtUtc",
                table: "Presensis",
                columns: PresensiSesiCreatedColumns);

            migrationBuilder.CreateIndex(
                name: "IX_Presensis_SesiId_SantriId",
                table: "Presensis",
                columns: PresensiSesiSantriColumns);

            migrationBuilder.CreateIndex(
                name: "IX_Presensis_Status",
                table: "Presensis",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Presensis_UpdatedAtUtc",
                table: "Presensis",
                column: "UpdatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressKeilmuans_Level",
                table: "ProgressKeilmuans",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressKeilmuans_Level_SantriId",
                table: "ProgressKeilmuans",
                columns: ProgressLevelSantriColumns);

            migrationBuilder.CreateIndex(
                name: "IX_ProgressKeilmuans_SantriId_Judul",
                table: "ProgressKeilmuans",
                columns: ProgressSantriJudulColumns);

            migrationBuilder.CreateIndex(
                name: "IX_ProgressKeilmuans_SantriId_UpdatedAtUtc",
                table: "ProgressKeilmuans",
                columns: ProgressSantriUpdatedColumns);

            migrationBuilder.CreateIndex(
                name: "IX_Sesis_KegiatanId",
                table: "Sesis",
                column: "KegiatanId");

            migrationBuilder.CreateIndex(
                name: "IX_Sesis_Tanggal",
                table: "Sesis",
                column: "Tanggal");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Kafarahs");

            migrationBuilder.DropTable(
                name: "LogKeluarMasuks");

            migrationBuilder.DropTable(
                name: "Presensis");

            migrationBuilder.DropTable(
                name: "ProgressKeilmuans");

            migrationBuilder.DropTable(
                name: "Sesis");

            migrationBuilder.DropTable(
                name: "Kegiatans");
        }
    }
}
