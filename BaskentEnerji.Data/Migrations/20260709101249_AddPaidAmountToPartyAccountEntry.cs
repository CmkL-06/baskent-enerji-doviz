using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaskentEnerji.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaidAmountToPartyAccountEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "PartyAccountEntries",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "PartyAccountEntries");
        }
    }
}
