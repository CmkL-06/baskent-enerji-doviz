using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneyTransferTurkey.Data.Migrations
{
    /// <inheritdoc />
    public partial class mig_ihtiyar7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VaultBalanceHistories_VaultId_CurrencyId",
                table: "VaultBalanceHistories");

            migrationBuilder.CreateIndex(
                name: "IX_VaultBalanceHistories_VaultId_CurrencyId",
                table: "VaultBalanceHistories",
                columns: new[] { "VaultId", "CurrencyId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VaultBalanceHistories_VaultId_CurrencyId",
                table: "VaultBalanceHistories");

            migrationBuilder.CreateIndex(
                name: "IX_VaultBalanceHistories_VaultId_CurrencyId",
                table: "VaultBalanceHistories",
                columns: new[] { "VaultId", "CurrencyId" },
                unique: true);
        }
    }
}
