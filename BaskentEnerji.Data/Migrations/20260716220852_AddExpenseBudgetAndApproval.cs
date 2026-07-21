using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaskentEnerji.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseBudgetAndApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "ExpensePayments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedByUserId",
                table: "ExpensePayments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionNote",
                table: "ExpensePayments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExpenseBudgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfficeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    BudgetAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseBudgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseBudgets_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalTable: "Offices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExpenseBudgets_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpensePayments_ApprovedByUserId",
                table: "ExpensePayments",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseBudgets_CreatedByUserId",
                table: "ExpenseBudgets",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseBudgets_OfficeId_Category_Year_Month",
                table: "ExpenseBudgets",
                columns: new[] { "OfficeId", "Category", "Year", "Month" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpensePayments_Users_ApprovedByUserId",
                table: "ExpensePayments",
                column: "ApprovedByUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpensePayments_Users_ApprovedByUserId",
                table: "ExpensePayments");

            migrationBuilder.DropTable(
                name: "ExpenseBudgets");

            migrationBuilder.DropIndex(
                name: "IX_ExpensePayments_ApprovedByUserId",
                table: "ExpensePayments");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "ExpensePayments");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "ExpensePayments");

            migrationBuilder.DropColumn(
                name: "RejectionNote",
                table: "ExpensePayments");
        }
    }
}
