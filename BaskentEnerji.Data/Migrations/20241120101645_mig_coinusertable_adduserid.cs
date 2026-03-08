using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaskentEnerji.Data.Migrations
{
    /// <inheritdoc />
    public partial class mig_coinusertable_adduserid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Coin_Users_Users_UserId",
                table: "Coin_Users");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Coin_User_Tables",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Coin_User_Tables_UserId",
                table: "Coin_User_Tables",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Coin_User_Tables_Users_UserId",
                table: "Coin_User_Tables",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Coin_Users_Users_UserId",
                table: "Coin_Users",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Coin_User_Tables_Users_UserId",
                table: "Coin_User_Tables");

            migrationBuilder.DropForeignKey(
                name: "FK_Coin_Users_Users_UserId",
                table: "Coin_Users");

            migrationBuilder.DropIndex(
                name: "IX_Coin_User_Tables_UserId",
                table: "Coin_User_Tables");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Coin_User_Tables");

            migrationBuilder.AddForeignKey(
                name: "FK_Coin_Users_Users_UserId",
                table: "Coin_Users",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
