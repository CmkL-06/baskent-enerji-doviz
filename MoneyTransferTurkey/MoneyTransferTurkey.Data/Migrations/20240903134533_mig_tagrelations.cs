using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneyTransferTurkey.Data.Migrations
{
    /// <inheritdoc />
    public partial class mig_tagrelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Blog_Category_Tags_Tags_CategoryId",
                table: "Blog_Category_Tags");

            migrationBuilder.CreateIndex(
                name: "IX_Blog_Category_Tags_TagId",
                table: "Blog_Category_Tags",
                column: "TagId");

            migrationBuilder.AddForeignKey(
                name: "FK_Blog_Category_Tags_Tags_TagId",
                table: "Blog_Category_Tags",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Blog_Category_Tags_Tags_TagId",
                table: "Blog_Category_Tags");

            migrationBuilder.DropIndex(
                name: "IX_Blog_Category_Tags_TagId",
                table: "Blog_Category_Tags");

            migrationBuilder.AddForeignKey(
                name: "FK_Blog_Category_Tags_Tags_CategoryId",
                table: "Blog_Category_Tags",
                column: "CategoryId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
