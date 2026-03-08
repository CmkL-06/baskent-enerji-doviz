using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneyTransferTurkey.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOfficeIdToExchangeRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add OfficeId as nullable first
            migrationBuilder.AddColumn<Guid>(
                name: "OfficeId",
                table: "ExchangeRates",
                type: "uniqueidentifier",
                nullable: true);

            // Step 2: Update existing records with the first office ID
            migrationBuilder.Sql(@"
                DECLARE @DefaultOfficeId uniqueidentifier;
                SELECT TOP 1 @DefaultOfficeId = Id FROM Offices WHERE IsActive = 1;
                
                -- If no active office found, get any office
                IF @DefaultOfficeId IS NULL
                    SELECT TOP 1 @DefaultOfficeId = Id FROM Offices;
                
                -- Update all existing exchange rates with the default office
                UPDATE ExchangeRates SET OfficeId = @DefaultOfficeId WHERE OfficeId IS NULL;
            ");

            // Step 3: Make OfficeId non-nullable
            migrationBuilder.AlterColumn<Guid>(
                name: "OfficeId",
                table: "ExchangeRates",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            // Step 4: Drop old index
            migrationBuilder.DropIndex(
                name: "IX_ExchangeRates_SourceCurrencyId_TargetCurrencyId_EffectiveFrom",
                table: "ExchangeRates");

            // Step 5: Create new indexes
            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_OfficeId_SourceCurrencyId_TargetCurrencyId_EffectiveFrom",
                table: "ExchangeRates",
                columns: new[] { "OfficeId", "SourceCurrencyId", "TargetCurrencyId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_SourceCurrencyId",
                table: "ExchangeRates",
                column: "SourceCurrencyId");

            // Step 6: Add foreign key
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