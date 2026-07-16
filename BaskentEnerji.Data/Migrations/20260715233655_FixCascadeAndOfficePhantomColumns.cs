using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaskentEnerji.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixCascadeAndOfficePhantomColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OfficeTransfers_Offices_OfficeId",
                table: "OfficeTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_OfficeTransfers_Offices_OfficeId1",
                table: "OfficeTransfers");

            migrationBuilder.DropForeignKey(
                name: "FK_TransactionDetails_Currencies_CurrencyId",
                table: "TransactionDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Users_UserId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Vaults_VaultId",
                table: "Transactions");

            // NOT: IX_OfficeTransfers_OfficeId/OfficeId1 index'leri canlı veritabanında hiç
            // oluşturulmamış (migration geçmişi/canlı sapması — FK'lar var ama index'ler yok).
            // Var olmayan bir index'i DROP etmeye çalışmak hataya düşer, bu yüzden bu iki
            // DropIndex çağrısı kasıtlı olarak kaldırıldı.
            migrationBuilder.DropColumn(
                name: "OfficeId",
                table: "OfficeTransfers");

            migrationBuilder.DropColumn(
                name: "OfficeId1",
                table: "OfficeTransfers");

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionDetails_Currencies_CurrencyId",
                table: "TransactionDetails",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Users_UserId",
                table: "Transactions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Vaults_VaultId",
                table: "Transactions",
                column: "VaultId",
                principalTable: "Vaults",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransactionDetails_Currencies_CurrencyId",
                table: "TransactionDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Users_UserId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Vaults_VaultId",
                table: "Transactions");

            migrationBuilder.AddColumn<Guid>(
                name: "OfficeId",
                table: "OfficeTransfers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OfficeId1",
                table: "OfficeTransfers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OfficeTransfers_Offices_OfficeId",
                table: "OfficeTransfers",
                column: "OfficeId",
                principalTable: "Offices",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OfficeTransfers_Offices_OfficeId1",
                table: "OfficeTransfers",
                column: "OfficeId1",
                principalTable: "Offices",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionDetails_Currencies_CurrencyId",
                table: "TransactionDetails",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Users_UserId",
                table: "Transactions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Vaults_VaultId",
                table: "Transactions",
                column: "VaultId",
                principalTable: "Vaults",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
