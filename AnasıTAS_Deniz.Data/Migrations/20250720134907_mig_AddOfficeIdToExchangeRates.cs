using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnasıTAS_Deniz.Data.Migrations
{
    /// <inheritdoc />
    public partial class mig_AddOfficeIdToExchangeRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ExchangeRates_SourceCurrencyId_TargetCurrencyId_EffectiveFrom",
                table: "ExchangeRates");

            migrationBuilder.AddColumn<Guid>(
                name: "OfficeId",
                table: "ExchangeRates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_OfficeId_SourceCurrencyId_TargetCurrencyId_EffectiveFrom",
                table: "ExchangeRates",
                columns: new[] { "OfficeId", "SourceCurrencyId", "TargetCurrencyId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_SourceCurrencyId",
                table: "ExchangeRates",
                column: "SourceCurrencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExchangeRates_Offices_OfficeId",
                table: "ExchangeRates",
                column: "OfficeId",
                principalTable: "Offices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExchangeRates_Offices_OfficeId",
                table: "ExchangeRates");

            migrationBuilder.DropIndex(
                name: "IX_ExchangeRates_OfficeId_SourceCurrencyId_TargetCurrencyId_EffectiveFrom",
                table: "ExchangeRates");

            migrationBuilder.DropIndex(
                name: "IX_ExchangeRates_SourceCurrencyId",
                table: "ExchangeRates");

            migrationBuilder.DropColumn(
                name: "OfficeId",
                table: "ExchangeRates");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_SourceCurrencyId_TargetCurrencyId_EffectiveFrom",
                table: "ExchangeRates",
                columns: new[] { "SourceCurrencyId", "TargetCurrencyId", "EffectiveFrom" });
        }
    }
}
