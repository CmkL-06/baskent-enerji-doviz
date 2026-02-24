using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnasıTAS_Deniz.Data.Migrations
{
    /// <inheritdoc />
    public partial class mig_add_blogcategory_addfields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Blog_Article_Categories",
                table: "Blog_Article_Categories");

            migrationBuilder.DropIndex(
                name: "IX_Blog_Article_Categories_ArticleId",
                table: "Blog_Article_Categories");

            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "Blog_Categories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Blog_Article_Categories",
                table: "Blog_Article_Categories",
                columns: new[] { "ArticleId", "CategoryId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Blog_Article_Categories",
                table: "Blog_Article_Categories");

            migrationBuilder.DropColumn(
                name: "Content",
                table: "Blog_Categories");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Blog_Article_Categories",
                table: "Blog_Article_Categories",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Blog_Article_Categories_ArticleId",
                table: "Blog_Article_Categories",
                column: "ArticleId");
        }
    }
}
