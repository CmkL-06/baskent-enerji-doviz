using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaskentEnerji.Data.Migrations
{
    /// <inheritdoc />
    public partial class mig_wac_and_dayclosure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CurrencyWacHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VaultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OldWac = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NewWac = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OldQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NewQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TransactionAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TransactionRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reason = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyWacHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurrencyWacHistories_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurrencyWacHistories_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CurrencyWacHistories_Vaults_VaultId",
                        column: x => x.VaultId,
                        principalTable: "Vaults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CurrencyWacs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VaultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Wac = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyWacs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurrencyWacs_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurrencyWacs_Vaults_VaultId",
                        column: x => x.VaultId,
                        principalTable: "Vaults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DayClosures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfficeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VaultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    VaultCountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TotalRealizedProfit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TransactionCount = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAutoGenerated = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DayClosures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DayClosures_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalTable: "Offices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DayClosures_Users_ClosedByUserId",
                        column: x => x.ClosedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DayClosures_VaultCounts_VaultCountId",
                        column: x => x.VaultCountId,
                        principalTable: "VaultCounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DayClosures_Vaults_VaultId",
                        column: x => x.VaultId,
                        principalTable: "Vaults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DayClosureDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayClosureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SystemBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PhysicalCount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Discrepancy = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscrepancyNote = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WacAtClose = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OpeningWac = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DayClosureDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DayClosureDetails_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DayClosureDetails_DayClosures_DayClosureId",
                        column: x => x.DayClosureId,
                        principalTable: "DayClosures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyWacHistories_CurrencyId",
                table: "CurrencyWacHistories",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyWacHistories_TransactionId",
                table: "CurrencyWacHistories",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyWacHistories_VaultId",
                table: "CurrencyWacHistories",
                column: "VaultId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyWacs_CurrencyId",
                table: "CurrencyWacs",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyWacs_VaultId",
                table: "CurrencyWacs",
                column: "VaultId");

            migrationBuilder.CreateIndex(
                name: "IX_DayClosureDetails_CurrencyId",
                table: "DayClosureDetails",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_DayClosureDetails_DayClosureId",
                table: "DayClosureDetails",
                column: "DayClosureId");

            migrationBuilder.CreateIndex(
                name: "IX_DayClosures_ClosedByUserId",
                table: "DayClosures",
                column: "ClosedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DayClosures_OfficeId",
                table: "DayClosures",
                column: "OfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_DayClosures_VaultCountId",
                table: "DayClosures",
                column: "VaultCountId");

            migrationBuilder.CreateIndex(
                name: "IX_DayClosures_VaultId",
                table: "DayClosures",
                column: "VaultId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CurrencyWacHistories");

            migrationBuilder.DropTable(
                name: "CurrencyWacs");

            migrationBuilder.DropTable(
                name: "DayClosureDetails");

            migrationBuilder.DropTable(
                name: "DayClosures");
        }
    }
}
