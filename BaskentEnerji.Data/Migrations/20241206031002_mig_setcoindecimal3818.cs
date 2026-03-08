using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaskentEnerji.Data.Migrations
{
    /// <inheritdoc />
    public partial class mig_setcoindecimal3818 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "SellPrice",
                table: "Coin_Users",
                type: "decimal(38,18)",
                precision: 38,
                scale: 18,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,8)",
                oldPrecision: 38,
                oldScale: 8,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "Coin_Users",
                type: "decimal(38,18)",
                precision: 38,
                scale: 18,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,8)",
                oldPrecision: 38,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "Profit",
                table: "Coin_Users",
                type: "decimal(38,18)",
                precision: 38,
                scale: 18,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,8)",
                oldPrecision: 38,
                oldScale: 8,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "FeeRate",
                table: "Coin_Users",
                type: "decimal(38,18)",
                precision: 38,
                scale: 18,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,8)",
                oldPrecision: 38,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "EffQuantity",
                table: "Coin_Users",
                type: "decimal(38,18)",
                precision: 38,
                scale: 18,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,8)",
                oldPrecision: 38,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "BuyPrice",
                table: "Coin_Users",
                type: "decimal(38,18)",
                precision: 38,
                scale: 18,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,8)",
                oldPrecision: 38,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "SellPrice",
                table: "Coin_Profits",
                type: "decimal(38,18)",
                precision: 38,
                scale: 18,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,8)",
                oldPrecision: 38,
                oldScale: 8,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "Coin_Profits",
                type: "decimal(38,18)",
                precision: 38,
                scale: 18,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,8)",
                oldPrecision: 38,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "Profit",
                table: "Coin_Profits",
                type: "decimal(38,18)",
                precision: 38,
                scale: 18,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,8)",
                oldPrecision: 38,
                oldScale: 8,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "FeeRate",
                table: "Coin_Profits",
                type: "decimal(38,18)",
                precision: 38,
                scale: 18,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,8)",
                oldPrecision: 38,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "EffQuantity",
                table: "Coin_Profits",
                type: "decimal(38,18)",
                precision: 38,
                scale: 18,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,8)",
                oldPrecision: 38,
                oldScale: 8);

            migrationBuilder.AlterColumn<decimal>(
                name: "BuyPrice",
                table: "Coin_Profits",
                type: "decimal(38,18)",
                precision: 38,
                scale: 18,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,8)",
                oldPrecision: 38,
                oldScale: 8);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "SellPrice",
                table: "Coin_Users",
                type: "decimal(38,8)",
                precision: 38,
                scale: 8,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,18)",
                oldPrecision: 38,
                oldScale: 18,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "Coin_Users",
                type: "decimal(38,8)",
                precision: 38,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,18)",
                oldPrecision: 38,
                oldScale: 18);

            migrationBuilder.AlterColumn<decimal>(
                name: "Profit",
                table: "Coin_Users",
                type: "decimal(38,8)",
                precision: 38,
                scale: 8,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,18)",
                oldPrecision: 38,
                oldScale: 18,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "FeeRate",
                table: "Coin_Users",
                type: "decimal(38,8)",
                precision: 38,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,18)",
                oldPrecision: 38,
                oldScale: 18);

            migrationBuilder.AlterColumn<decimal>(
                name: "EffQuantity",
                table: "Coin_Users",
                type: "decimal(38,8)",
                precision: 38,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,18)",
                oldPrecision: 38,
                oldScale: 18);

            migrationBuilder.AlterColumn<decimal>(
                name: "BuyPrice",
                table: "Coin_Users",
                type: "decimal(38,8)",
                precision: 38,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,18)",
                oldPrecision: 38,
                oldScale: 18);

            migrationBuilder.AlterColumn<decimal>(
                name: "SellPrice",
                table: "Coin_Profits",
                type: "decimal(38,8)",
                precision: 38,
                scale: 8,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,18)",
                oldPrecision: 38,
                oldScale: 18,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "Coin_Profits",
                type: "decimal(38,8)",
                precision: 38,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,18)",
                oldPrecision: 38,
                oldScale: 18);

            migrationBuilder.AlterColumn<decimal>(
                name: "Profit",
                table: "Coin_Profits",
                type: "decimal(38,8)",
                precision: 38,
                scale: 8,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,18)",
                oldPrecision: 38,
                oldScale: 18,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "FeeRate",
                table: "Coin_Profits",
                type: "decimal(38,8)",
                precision: 38,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,18)",
                oldPrecision: 38,
                oldScale: 18);

            migrationBuilder.AlterColumn<decimal>(
                name: "EffQuantity",
                table: "Coin_Profits",
                type: "decimal(38,8)",
                precision: 38,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,18)",
                oldPrecision: 38,
                oldScale: 18);

            migrationBuilder.AlterColumn<decimal>(
                name: "BuyPrice",
                table: "Coin_Profits",
                type: "decimal(38,8)",
                precision: 38,
                scale: 8,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(38,18)",
                oldPrecision: 38,
                oldScale: 18);
        }
    }
}
