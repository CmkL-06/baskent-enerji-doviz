using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmileMedical.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVaultCountingSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastCountDate",
                table: "Vaults",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShouldCount",
                table: "Vaults",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "VaultCounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VaultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfficeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CountDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HasDiscrepancy = table.Column<bool>(type: "bit", nullable: false),
                    DiscrepancyDetails = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsSystemGenerated = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultCounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VaultCounts_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalTable: "Offices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VaultCounts_Vaults_VaultId",
                        column: x => x.VaultId,
                        principalTable: "Vaults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultCountDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VaultCountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActualAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SystemAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Discrepancy = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultCountDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VaultCountDetails_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VaultCountDetails_VaultCounts_VaultCountId",
                        column: x => x.VaultCountId,
                        principalTable: "VaultCounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VaultCountDetails_CurrencyId",
                table: "VaultCountDetails",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_VaultCountDetails_VaultCountId",
                table: "VaultCountDetails",
                column: "VaultCountId");

            migrationBuilder.CreateIndex(
                name: "IX_VaultCounts_OfficeId",
                table: "VaultCounts",
                column: "OfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_VaultCounts_VaultId",
                table: "VaultCounts",
                column: "VaultId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VaultCountDetails");

            migrationBuilder.DropTable(
                name: "VaultCounts");

            migrationBuilder.DropColumn(
                name: "LastCountDate",
                table: "Vaults");

            migrationBuilder.DropColumn(
                name: "ShouldCount",
                table: "Vaults");
        }
    }
}
