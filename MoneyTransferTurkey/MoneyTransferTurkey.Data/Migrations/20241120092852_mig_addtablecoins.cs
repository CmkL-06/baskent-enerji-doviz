using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneyTransferTurkey.Data.Migrations
{
    /// <inheritdoc />
    public partial class mig_addtablecoins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "Coin_User_TableId",
                table: "Coin_Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "Coin_Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Coin_User_Tables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coin_User_Tables", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Coin_Users_Coin_User_TableId",
                table: "Coin_Users",
                column: "Coin_User_TableId");

            migrationBuilder.AddForeignKey(
                name: "FK_Coin_Users_Coin_User_Tables_Coin_User_TableId",
                table: "Coin_Users",
                column: "Coin_User_TableId",
                principalTable: "Coin_User_Tables",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Coin_Users_Coin_User_Tables_Coin_User_TableId",
                table: "Coin_Users");

            migrationBuilder.DropTable(
                name: "Coin_User_Tables");

            migrationBuilder.DropIndex(
                name: "IX_Coin_Users_Coin_User_TableId",
                table: "Coin_Users");

            migrationBuilder.DropColumn(
                name: "Coin_User_TableId",
                table: "Coin_Users");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "Coin_Users");
        }
    }
}
