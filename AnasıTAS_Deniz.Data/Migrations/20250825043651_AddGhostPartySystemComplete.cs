using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnasıTAS_Deniz.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGhostPartySystemComplete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsGhost",
                table: "VaultBalanceHistories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "GhostPartyAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfficeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    BlockedAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalDebits = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalCredits = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TransactionCount = table.Column<int>(type: "int", nullable: false),
                    LastTransactionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GhostPartyAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GhostPartyAccounts_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GhostPartyAccounts_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalTable: "Offices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GhostPartyAccounts_Parties_PartyId",
                        column: x => x.PartyId,
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GhostPartyAccountEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GhostAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfficeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntryType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    RunningBalance = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VaultId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsReconciled = table.Column<bool>(type: "bit", nullable: false),
                    ReconciledDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReconciledBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GhostPartyAccountEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GhostPartyAccountEntries_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GhostPartyAccountEntries_GhostPartyAccounts_GhostAccountId",
                        column: x => x.GhostAccountId,
                        principalTable: "GhostPartyAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GhostPartyAccountEntries_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalTable: "Offices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GhostPartyAccountEntries_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GhostPartyAccountEntries_Vaults_VaultId",
                        column: x => x.VaultId,
                        principalTable: "Vaults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GhostPartyAccountEntries_CurrencyId",
                table: "GhostPartyAccountEntries",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_GhostPartyAccountEntries_GhostAccountId",
                table: "GhostPartyAccountEntries",
                column: "GhostAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_GhostPartyAccountEntries_OfficeId",
                table: "GhostPartyAccountEntries",
                column: "OfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_GhostPartyAccountEntries_TransactionId",
                table: "GhostPartyAccountEntries",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_GhostPartyAccountEntries_VaultId",
                table: "GhostPartyAccountEntries",
                column: "VaultId");

            migrationBuilder.CreateIndex(
                name: "IX_GhostPartyAccounts_CurrencyId",
                table: "GhostPartyAccounts",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_GhostPartyAccounts_OfficeId",
                table: "GhostPartyAccounts",
                column: "OfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_GhostPartyAccounts_PartyId",
                table: "GhostPartyAccounts",
                column: "PartyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GhostPartyAccountEntries");

            migrationBuilder.DropTable(
                name: "GhostPartyAccounts");

            migrationBuilder.DropColumn(
                name: "IsGhost",
                table: "VaultBalanceHistories");
        }
    }
}
