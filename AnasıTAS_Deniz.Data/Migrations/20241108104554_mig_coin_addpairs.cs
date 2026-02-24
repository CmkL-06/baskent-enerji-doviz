using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnasıTAS_Deniz.Data.Migrations
{
    /// <inheritdoc />
    public partial class mig_coin_addpairs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CoinId",
                table: "Coin_Pairs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Coin_Pairs_CoinId",
                table: "Coin_Pairs",
                column: "CoinId");

            migrationBuilder.AddForeignKey(
                name: "FK_Coin_Pairs_Coins_CoinId",
                table: "Coin_Pairs",
                column: "CoinId",
                principalTable: "Coins",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Coin_Pairs_Coins_CoinId",
                table: "Coin_Pairs");

            migrationBuilder.DropIndex(
                name: "IX_Coin_Pairs_CoinId",
                table: "Coin_Pairs");

            migrationBuilder.DropColumn(
                name: "CoinId",
                table: "Coin_Pairs");
        }
    }
}
