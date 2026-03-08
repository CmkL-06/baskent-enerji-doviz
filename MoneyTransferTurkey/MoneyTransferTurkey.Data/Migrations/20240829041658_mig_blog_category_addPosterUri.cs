using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneyTransferTurkey.Data.Migrations
{
    /// <inheritdoc />
    public partial class mig_blog_category_addPosterUri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PosterHeaderUri",
                table: "Blog_Categories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PosterUri",
                table: "Blog_Categories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PosterHeaderUri",
                table: "Blog_Articles",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PosterHeaderUri",
                table: "Blog_Categories");

            migrationBuilder.DropColumn(
                name: "PosterUri",
                table: "Blog_Categories");

            migrationBuilder.DropColumn(
                name: "PosterHeaderUri",
                table: "Blog_Articles");
        }
    }
}
