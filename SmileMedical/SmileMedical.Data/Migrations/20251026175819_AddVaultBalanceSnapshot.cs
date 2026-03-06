using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmileMedical.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVaultBalanceSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VaultBalanceSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfficeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultBalanceSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VaultBalanceSnapshots_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalTable: "Offices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VaultBalanceSnapshotDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VaultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ReservedAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultBalanceSnapshotDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VaultBalanceSnapshotDetails_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VaultBalanceSnapshotDetails_VaultBalanceSnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "VaultBalanceSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VaultBalanceSnapshotDetails_Vaults_VaultId",
                        column: x => x.VaultId,
                        principalTable: "Vaults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VaultBalanceSnapshotDetails_CurrencyId",
                table: "VaultBalanceSnapshotDetails",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_VaultBalanceSnapshotDetails_SnapshotId_VaultId_CurrencyId",
                table: "VaultBalanceSnapshotDetails",
                columns: new[] { "SnapshotId", "VaultId", "CurrencyId" });

            migrationBuilder.CreateIndex(
                name: "IX_VaultBalanceSnapshotDetails_VaultId",
                table: "VaultBalanceSnapshotDetails",
                column: "VaultId");

            migrationBuilder.CreateIndex(
                name: "IX_VaultBalanceSnapshots_OfficeId_SnapshotDate",
                table: "VaultBalanceSnapshots",
                columns: new[] { "OfficeId", "SnapshotDate" });

            migrationBuilder.CreateIndex(
                name: "IX_VaultBalanceSnapshots_UserId",
                table: "VaultBalanceSnapshots",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VaultBalanceSnapshotDetails");

            migrationBuilder.DropTable(
                name: "VaultBalanceSnapshots");
        }
    }
}
