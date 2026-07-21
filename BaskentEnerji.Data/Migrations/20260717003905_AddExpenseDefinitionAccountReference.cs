using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaskentEnerji.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseDefinitionAccountReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountReference",
                table: "ExpenseDefinitions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DueDayOfMonth",
                table: "ExpenseDefinitions",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountReference",
                table: "ExpenseDefinitions");

            migrationBuilder.DropColumn(
                name: "DueDayOfMonth",
                table: "ExpenseDefinitions");
        }
    }
}
