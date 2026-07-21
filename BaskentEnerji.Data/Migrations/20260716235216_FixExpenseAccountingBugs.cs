using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaskentEnerji.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixExpenseAccountingBugs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpensePayments_ExpenseDefinitions_ExpenseDefinitionId",
                table: "ExpensePayments");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpensePayments_Vaults_VaultId",
                table: "ExpensePayments");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseDefinitions_OfficeId",
                table: "ExpenseDefinitions");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "ExpenseDefinitions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseDefinitions_OfficeId_Code",
                table: "ExpenseDefinitions",
                columns: new[] { "OfficeId", "Code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpensePayments_ExpenseDefinitions_ExpenseDefinitionId",
                table: "ExpensePayments",
                column: "ExpenseDefinitionId",
                principalTable: "ExpenseDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpensePayments_Vaults_VaultId",
                table: "ExpensePayments",
                column: "VaultId",
                principalTable: "Vaults",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpensePayments_ExpenseDefinitions_ExpenseDefinitionId",
                table: "ExpensePayments");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpensePayments_Vaults_VaultId",
                table: "ExpensePayments");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseDefinitions_OfficeId_Code",
                table: "ExpenseDefinitions");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "ExpenseDefinitions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseDefinitions_OfficeId",
                table: "ExpenseDefinitions",
                column: "OfficeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpensePayments_ExpenseDefinitions_ExpenseDefinitionId",
                table: "ExpensePayments",
                column: "ExpenseDefinitionId",
                principalTable: "ExpenseDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpensePayments_Vaults_VaultId",
                table: "ExpensePayments",
                column: "VaultId",
                principalTable: "Vaults",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
