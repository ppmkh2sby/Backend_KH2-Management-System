using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KH2.ManagementSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GeneralizeFaceEnrollmentToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FaceEnrollments_Santris_SantriId",
                table: "FaceEnrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_FaceProfiles_Santris_SantriId",
                table: "FaceProfiles");

            // Existing rows are keyed by Santri.Id.  Move them to the owning
            // account before the foreign key is changed to Users.Id.
            migrationBuilder.Sql("""
                UPDATE \"FaceEnrollments\" AS enrollment
                SET \"SantriId\" = santri.\"UserId\"
                FROM \"Santris\" AS santri
                WHERE enrollment.\"SantriId\" = santri.\"Id\";
                """);

            migrationBuilder.Sql("""
                UPDATE \"FaceProfiles\" AS profile
                SET \"SantriId\" = santri.\"UserId\"
                FROM \"Santris\" AS santri
                WHERE profile.\"SantriId\" = santri.\"Id\";
                """);

            migrationBuilder.RenameColumn(
                name: "SantriId",
                table: "FaceProfiles",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_FaceProfiles_SantriId",
                table: "FaceProfiles",
                newName: "IX_FaceProfiles_UserId");

            migrationBuilder.RenameColumn(
                name: "SantriId",
                table: "FaceEnrollments",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_FaceEnrollments_SantriId",
                table: "FaceEnrollments",
                newName: "IX_FaceEnrollments_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_FaceEnrollments_Users_UserId",
                table: "FaceEnrollments",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FaceProfiles_Users_UserId",
                table: "FaceProfiles",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FaceEnrollments_Users_UserId",
                table: "FaceEnrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_FaceProfiles_Users_UserId",
                table: "FaceProfiles");

            migrationBuilder.Sql("""
                UPDATE \"FaceEnrollments\" AS enrollment
                SET \"UserId\" = santri.\"Id\"
                FROM \"Santris\" AS santri
                WHERE enrollment.\"UserId\" = santri.\"UserId\";
                """);

            migrationBuilder.Sql("""
                UPDATE \"FaceProfiles\" AS profile
                SET \"UserId\" = santri.\"Id\"
                FROM \"Santris\" AS santri
                WHERE profile.\"UserId\" = santri.\"UserId\";
                """);

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "FaceProfiles",
                newName: "SantriId");

            migrationBuilder.RenameIndex(
                name: "IX_FaceProfiles_UserId",
                table: "FaceProfiles",
                newName: "IX_FaceProfiles_SantriId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "FaceEnrollments",
                newName: "SantriId");

            migrationBuilder.RenameIndex(
                name: "IX_FaceEnrollments_UserId",
                table: "FaceEnrollments",
                newName: "IX_FaceEnrollments_SantriId");

            migrationBuilder.AddForeignKey(
                name: "FK_FaceEnrollments_Santris_SantriId",
                table: "FaceEnrollments",
                column: "SantriId",
                principalTable: "Santris",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FaceProfiles_Santris_SantriId",
                table: "FaceProfiles",
                column: "SantriId",
                principalTable: "Santris",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
