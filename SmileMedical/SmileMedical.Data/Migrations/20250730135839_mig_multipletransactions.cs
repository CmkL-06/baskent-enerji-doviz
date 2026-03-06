using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmileMedical.Data.Migrations
{
    /// <inheritdoc />
    public partial class mig_multipletransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualBuyRate",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ActualSellRate",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "CustomRate",
                table: "Transactions");

            migrationBuilder.AddColumn<decimal>(
                name: "ActualBuyRate",
                table: "TransactionDetails",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualSellRate",
                table: "TransactionDetails",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CustomRate",
                table: "TransactionDetails",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualBuyRate",
                table: "TransactionDetails");

            migrationBuilder.DropColumn(
                name: "ActualSellRate",
                table: "TransactionDetails");

            migrationBuilder.DropColumn(
                name: "CustomRate",
                table: "TransactionDetails");

            migrationBuilder.AddColumn<decimal>(
                name: "ActualBuyRate",
                table: "Transactions",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualSellRate",
                table: "Transactions",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CustomRate",
                table: "Transactions",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);
        }
    }
}
