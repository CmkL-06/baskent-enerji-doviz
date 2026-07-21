using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaskentEnerji.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixExpenseDecimalPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // BaskentEnerjiDbContext.cs zaten HasPrecision(18,4) tanımlıyordu, ama canlı DB'de
            // bu iki kolon hâlâ ilk migration'daki (18,2) hassasiyetindeydi — model/şema
            // sürüklenmesini giderir.
            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "ExpensePayments",
                type: "decimal(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "DefaultAmount",
                table: "ExpenseDefinitions",
                type: "decimal(18,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "ExpensePayments",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "DefaultAmount",
                table: "ExpenseDefinitions",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldNullable: true);
        }
    }
}
