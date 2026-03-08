using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaskentEnerji.Data.Migrations
{
    /// <inheritdoc />
    public partial class mig_4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Blog_Articles_Blog_Categories_CategoryId",
                table: "Blog_Articles");

            migrationBuilder.DropIndex(
                name: "IX_Blog_Articles_CategoryId",
                table: "Blog_Articles");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Blog_Articles");

            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "Tags",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "LanguageId",
                table: "Tags",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "SeoTitle",
                table: "Tags",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "Languages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SeoLink",
                table: "Blog_Categories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SeoTitle",
                table: "Blog_Categories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsAnnouncement",
                table: "Blog_Articles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "Blog_Articles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOnSlider",
                table: "Blog_Articles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsUnique",
                table: "Blog_Articles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SeoLink",
                table: "Blog_Articles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Blog_Article_Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blog_Article_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Blog_Article_Categories_Blog_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Blog_Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Blog_Article_Categories_Blog_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Blog_Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Blog_Category_Tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blog_Category_Tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Blog_Category_Tags_Blog_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Blog_Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Blog_Category_Tags_Tags_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tags_LanguageId",
                table: "Tags",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_Blog_Article_Categories_ArticleId",
                table: "Blog_Article_Categories",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_Blog_Article_Categories_CategoryId",
                table: "Blog_Article_Categories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Blog_Category_Tags_CategoryId",
                table: "Blog_Category_Tags",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_Languages_LanguageId",
                table: "Tags",
                column: "LanguageId",
                principalTable: "Languages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tags_Languages_LanguageId",
                table: "Tags");

            migrationBuilder.DropTable(
                name: "Blog_Article_Categories");

            migrationBuilder.DropTable(
                name: "Blog_Category_Tags");

            migrationBuilder.DropIndex(
                name: "IX_Tags_LanguageId",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "Content",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "LanguageId",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "SeoTitle",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "Languages");

            migrationBuilder.DropColumn(
                name: "SeoLink",
                table: "Blog_Categories");

            migrationBuilder.DropColumn(
                name: "SeoTitle",
                table: "Blog_Categories");

            migrationBuilder.DropColumn(
                name: "IsAnnouncement",
                table: "Blog_Articles");

            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "Blog_Articles");

            migrationBuilder.DropColumn(
                name: "IsOnSlider",
                table: "Blog_Articles");

            migrationBuilder.DropColumn(
                name: "IsUnique",
                table: "Blog_Articles");

            migrationBuilder.DropColumn(
                name: "SeoLink",
                table: "Blog_Articles");

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "Blog_Articles",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Blog_Articles_CategoryId",
                table: "Blog_Articles",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Blog_Articles_Blog_Categories_CategoryId",
                table: "Blog_Articles",
                column: "CategoryId",
                principalTable: "Blog_Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
