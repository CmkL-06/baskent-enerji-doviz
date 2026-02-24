using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnasıTAS_Deniz.Data.Migrations
{
    /// <inheritdoc />
    public partial class mig_addoriginalcurrencytopartyentry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "PartyAccountEntries",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCustomRate",
                table: "PartyAccountEntries",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalAmount",
                table: "PartyAccountEntries",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginalCurrencyId",
                table: "PartyAccountEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartyAccountEntries_OriginalCurrencyId",
                table: "PartyAccountEntries",
                column: "OriginalCurrencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_PartyAccountEntries_Currencies_OriginalCurrencyId",
                table: "PartyAccountEntries",
                column: "OriginalCurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartyAccountEntries_Currencies_OriginalCurrencyId",
                table: "PartyAccountEntries");

            migrationBuilder.DropIndex(
                name: "IX_PartyAccountEntries_OriginalCurrencyId",
                table: "PartyAccountEntries");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "PartyAccountEntries");

            migrationBuilder.DropColumn(
                name: "IsCustomRate",
                table: "PartyAccountEntries");

            migrationBuilder.DropColumn(
                name: "OriginalAmount",
                table: "PartyAccountEntries");

            migrationBuilder.DropColumn(
                name: "OriginalCurrencyId",
                table: "PartyAccountEntries");
        }
    }
}
