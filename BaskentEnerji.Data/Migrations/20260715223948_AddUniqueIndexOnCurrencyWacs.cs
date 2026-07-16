using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaskentEnerji.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexOnCurrencyWacs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CurrencyWacs_VaultId",
                table: "CurrencyWacs");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyWacs_VaultId_CurrencyId",
                table: "CurrencyWacs",
                columns: new[] { "VaultId", "CurrencyId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CurrencyWacs_VaultId_CurrencyId",
                table: "CurrencyWacs");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyWacs_VaultId",
                table: "CurrencyWacs",
                column: "VaultId");
        }
    }
}
