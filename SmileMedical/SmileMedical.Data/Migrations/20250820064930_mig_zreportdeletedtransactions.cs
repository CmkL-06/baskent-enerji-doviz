using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmileMedical.Data.Migrations
{
    /// <inheritdoc />
    public partial class mig_zreportdeletedtransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "VaultBalanceHistories",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "VaultBalanceHistories");
        }
    }
}
