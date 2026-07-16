using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaskentEnerji.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDealerTypeToTgDealer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DealerType",
                table: "TgDealers",
                type: "int",
                nullable: false,
                defaultValue: 2); // External — yeni kayıtlar için varsayılan

            // Geriye dönük veri: VaultId doluysa Branch(1), boşsa External(2)
            migrationBuilder.Sql(@"
                UPDATE TgDealers
                SET DealerType = CASE WHEN VaultId IS NOT NULL AND LTRIM(RTRIM(VaultId)) <> '' THEN 1 ELSE 2 END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DealerType",
                table: "TgDealers");
        }
    }
}
