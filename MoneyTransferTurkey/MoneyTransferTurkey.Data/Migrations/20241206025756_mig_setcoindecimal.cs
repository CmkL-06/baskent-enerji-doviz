using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneyTransferTurkey.Data.Migrations
{
    /// <inheritdoc />
    public partial class mig_setcoindecimal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "SellPrice",
                table: "Coin_Users",
                type: "decimal(38,8)",
                precision: 38,
                scale: 8,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,8)",
                oldPrecision: 18,
                oldScale: 8,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "Coin_Users",
                type: "decimal(38,8)",
                precision: 38,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,8)",
                oldPrecision: 18,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "Profit",
                table: "Coin_Users",
                type: "decimal(38,8)",
                precision: 38,
                scale: 8,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,8)",
                oldPrecision: 18,
                oldScale: 8,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "FeeRate",
                table: "Coin_Users",
                type: "decimal(38,8)",
                precision: 38,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,8)",
                oldPrecision: 18,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "EffQuantity",
                table: "Coin_Users",
                type: "decimal(38,8)",
                precision: 38,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,8)",
                oldPrecision: 18,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "BuyPrice",
                table: "Coin_Users",
                type: "decimal(38,8)",
                precision: 38,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,8)",
                oldPrecision: 18,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "SellPrice",
                table: "Coin_Profits",
                type: "decimal(38,8)",
                precision: 38,
                scale: 8,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,8)",
                oldPrecision: 18,
                oldScale: 8,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "Coin_Profits",
                type: "decimal(38,8)",
                precision: 38,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,8)",
                oldPrecision: 18,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "Profit",
                table: "Coin_Profits",
                type: "decimal(38,8)",
                precision: 38,
                scale: 8,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,8)",
                oldPrecision: 18,
                oldScale: 8,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "FeeRate",
                table: "Coin_Profits",
                type: "decimal(38,8)",
                precision: 38,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,8)",
                oldPrecision: 18,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "EffQuantity",
                table: "Coin_Profits",
                type: "decimal(38,8)",
                precision: 38,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,8)",
                oldPrecision: 18,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "BuyPrice",
                table: "Coin_Profits",
                type: "decimal(38,8)",
                precision: 38,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,8)",
                oldPrecision: 18,
                oldScale: 8);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "SellPrice",
                table: "Coin_Users",
                type: "decimal(18,8)",
                precision: 18,
                scale: 8,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,8)",
                oldPrecision: 38,
                oldScale: 8,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "Coin_Users",
                type: "decimal(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,8)",
                oldPrecision: 38,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "Profit",
                table: "Coin_Users",
                type: "decimal(18,8)",
                precision: 18,
                scale: 8,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,8)",
                oldPrecision: 38,
                oldScale: 8,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "FeeRate",
                table: "Coin_Users",
                type: "decimal(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,8)",
                oldPrecision: 38,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "EffQuantity",
                table: "Coin_Users",
                type: "decimal(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,8)",
                oldPrecision: 38,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "BuyPrice",
                table: "Coin_Users",
                type: "decimal(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,8)",
                oldPrecision: 38,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "SellPrice",
                table: "Coin_Profits",
                type: "decimal(18,8)",
                precision: 18,
                scale: 8,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,8)",
                oldPrecision: 38,
                oldScale: 8,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "Coin_Profits",
                type: "decimal(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,8)",
                oldPrecision: 38,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "Profit",
                table: "Coin_Profits",
                type: "decimal(18,8)",
                precision: 18,
                scale: 8,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,8)",
                oldPrecision: 38,
                oldScale: 8,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "FeeRate",
                table: "Coin_Profits",
                type: "decimal(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,8)",
                oldPrecision: 38,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "EffQuantity",
                table: "Coin_Profits",
                type: "decimal(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,8)",
                oldPrecision: 38,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "BuyPrice",
                table: "Coin_Profits",
                type: "decimal(18,8)",
                precision: 18,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,8)",
                oldPrecision: 38,
                oldScale: 8);
        }
    }
}
