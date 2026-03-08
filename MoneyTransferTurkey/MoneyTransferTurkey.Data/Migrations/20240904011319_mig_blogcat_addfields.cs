using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneyTransferTurkey.Data.Migrations
{
    /// <inheritdoc />
    public partial class mig_blogcat_addfields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDisplayPage",
                table: "Blog_Categories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PageId",
                table: "Blog_Categories",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDisplayPage",
                table: "Blog_Categories");

            migrationBuilder.DropColumn(
                name: "PageId",
                table: "Blog_Categories");
        }
    }
}
