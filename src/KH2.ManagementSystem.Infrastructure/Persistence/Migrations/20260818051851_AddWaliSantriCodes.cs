using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KH2.ManagementSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWaliSantriCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WaliSantriCode",
                table: "WaliSantriRelations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"WaliSantriRelations\" AS relation " +
                "SET \"WaliSantriCode\" = '354' || SUBSTRING(santri.\"Nis\" FROM 3) " +
                "FROM \"Santris\" AS santri " +
                "WHERE relation.\"SantriId\" = santri.\"Id\";");

            migrationBuilder.AlterColumn<string>(
                name: "WaliSantriCode",
                table: "WaliSantriRelations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WaliSantriRelations_WaliSantriCode",
                table: "WaliSantriRelations",
                column: "WaliSantriCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WaliSantriRelations_WaliSantriCode",
                table: "WaliSantriRelations");

            migrationBuilder.DropColumn(
                name: "WaliSantriCode",
                table: "WaliSantriRelations");
        }
    }
}
