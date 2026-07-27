using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaskentEnerji.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexOnPartyAccountEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ReferenceNumber",
                table: "PartyAccountEntries",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartyAccountEntries_PartyAccountId_ReferenceNumber",
                table: "PartyAccountEntries",
                columns: new[] { "PartyAccountId", "ReferenceNumber" },
                unique: true,
                filter: "[ReferenceNumber] IS NOT NULL AND [IsReversed] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PartyAccountEntries_PartyAccountId_ReferenceNumber",
                table: "PartyAccountEntries");

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceNumber",
                table: "PartyAccountEntries",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
