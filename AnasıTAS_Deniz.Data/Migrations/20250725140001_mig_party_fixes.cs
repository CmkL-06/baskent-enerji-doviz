using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnasıTAS_Deniz.Data.Migrations
{
    /// <inheritdoc />
    public partial class mig_party_fixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PartyContacts_Parties_PartyId1",
                table: "PartyContacts");

            migrationBuilder.DropIndex(
                name: "IX_PartyContacts_PartyId1",
                table: "PartyContacts");

            migrationBuilder.DropColumn(
                name: "PartyId1",
                table: "PartyContacts");

            migrationBuilder.AlterColumn<decimal>(
                name: "TemporaryLimit",
                table: "PartyCreditLimits",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "CreditLimit",
                table: "PartyAccounts",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalVolume",
                table: "Parties",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "TemporaryLimit",
                table: "PartyCreditLimits",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)",
                oldPrecision: 18,
                oldScale: 6);

            migrationBuilder.AddColumn<Guid>(
                name: "PartyId1",
                table: "PartyContacts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CreditLimit",
                table: "PartyAccounts",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)",
                oldPrecision: 18,
                oldScale: 6);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalVolume",
                table: "Parties",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)",
                oldPrecision: 18,
                oldScale: 6);

            migrationBuilder.CreateIndex(
                name: "IX_PartyContacts_PartyId1",
                table: "PartyContacts",
                column: "PartyId1");

            migrationBuilder.AddForeignKey(
                name: "FK_PartyContacts_Parties_PartyId1",
                table: "PartyContacts",
                column: "PartyId1",
                principalTable: "Parties",
                principalColumn: "Id");
        }
    }
}
