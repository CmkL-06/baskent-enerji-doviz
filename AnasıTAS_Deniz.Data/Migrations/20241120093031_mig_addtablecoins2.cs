using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnasıTAS_Deniz.Data.Migrations
{
    /// <inheritdoc />
    public partial class mig_addtablecoins2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Coin_Users_Coin_User_Tables_Coin_User_TableId",
                table: "Coin_Users");

            migrationBuilder.AlterColumn<Guid>(
                name: "Coin_User_TableId",
                table: "Coin_Users",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Coin_Users_Coin_User_Tables_Coin_User_TableId",
                table: "Coin_Users",
                column: "Coin_User_TableId",
                principalTable: "Coin_User_Tables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Coin_Users_Coin_User_Tables_Coin_User_TableId",
                table: "Coin_Users");

            migrationBuilder.AlterColumn<Guid>(
                name: "Coin_User_TableId",
                table: "Coin_Users",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_Coin_Users_Coin_User_Tables_Coin_User_TableId",
                table: "Coin_Users",
                column: "Coin_User_TableId",
                principalTable: "Coin_User_Tables",
                principalColumn: "Id");
        }
    }
}
