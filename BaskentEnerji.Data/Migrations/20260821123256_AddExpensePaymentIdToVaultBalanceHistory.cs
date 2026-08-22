using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaskentEnerji.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExpensePaymentIdToVaultBalanceHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // AmountInTRY zaten elle eklenmişti (canlı ortamda 7 kayıt dolu) — kolon
            // model snapshot'ta yer alsın diye şartlı ekliyoruz.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'AmountInTRY' AND Object_ID = Object_ID(N'PartyAccountEntries'))
BEGIN
    ALTER TABLE PartyAccountEntries ADD AmountInTRY decimal(18,2) NULL;
END
");

            // Ölü tablo VaultCountDetails (plural, 0 kayıt) — kodda ve migration snapshot'ta
            // referansı yok, sadece SQL Server tarafında öksüz kalmış. FK'lar dahil temizle.
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'VaultCountDetails')
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_VaultCountDetails_Currencies_CurrencyId')
        ALTER TABLE VaultCountDetails DROP CONSTRAINT FK_VaultCountDetails_Currencies_CurrencyId;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_VaultCountDetails_VaultCounts_VaultCountId')
        ALTER TABLE VaultCountDetails DROP CONSTRAINT FK_VaultCountDetails_VaultCounts_VaultCountId;
    DROP TABLE VaultCountDetails;
END
");

            migrationBuilder.AddColumn<Guid>(
                name: "ExpensePaymentId",
                table: "VaultBalanceHistories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaultBalanceHistories_ExpensePaymentId",
                table: "VaultBalanceHistories",
                column: "ExpensePaymentId");

            migrationBuilder.AddForeignKey(
                name: "FK_VaultBalanceHistories_ExpensePayments_ExpensePaymentId",
                table: "VaultBalanceHistories",
                column: "ExpensePaymentId",
                principalTable: "ExpensePayments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Backfill: mevcut 24 onaylı gider ödemesi için VaultBalanceHistory kayıtlarını
            // Description eşleştirmesiyle bağla. Description formatı GetTurkishExpenseDescription
            // tarafından üretiliyor ve içinde PaymentNumber geçiyor.
            migrationBuilder.Sql(@"
UPDATE vh
SET vh.ExpensePaymentId = ep.Id
FROM VaultBalanceHistories vh
JOIN ExpensePayments ep ON ep.Status = 2 AND ep.IsDeleted = 0
    AND vh.Description LIKE '%' + ep.PaymentNumber + '%'
    AND vh.VaultId = ep.VaultId
    AND vh.CurrencyId = ep.CurrencyId
    AND vh.TransactionType = 3
    AND vh.Balance = -ep.Amount
    AND vh.ExpensePaymentId IS NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VaultBalanceHistories_ExpensePayments_ExpensePaymentId",
                table: "VaultBalanceHistories");

            migrationBuilder.DropIndex(
                name: "IX_VaultBalanceHistories_ExpensePaymentId",
                table: "VaultBalanceHistories");

            migrationBuilder.DropColumn(
                name: "ExpensePaymentId",
                table: "VaultBalanceHistories");
        }
    }
}
