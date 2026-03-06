using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmileMedical.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameEmployerIdToUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EmployerId",
                table: "VaultCounts",
                newName: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "VaultCounts",
                newName: "EmployerId");
        }
    }
}
