using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneyTransferTurkey.Data.Migrations
{
    /// <inheritdoc />
    public partial class mig_component_removenamecolumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "Components");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Components",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
