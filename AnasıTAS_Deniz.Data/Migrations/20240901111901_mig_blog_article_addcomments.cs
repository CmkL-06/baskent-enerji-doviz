using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnasıTAS_Deniz.Data.Migrations
{
    /// <inheritdoc />
    public partial class mig_blog_article_addcomments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SeoDescription",
                table: "Pages",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "Views",
                table: "Blog_Articles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "BlackList",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IpAdress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlackList", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Blog_Article_Comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IpAdress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blog_Article_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Blog_Article_Comments_Blog_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Blog_Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Blog_Article_Comments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Blog_Article_Visits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VisitorIp = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ArticleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blog_Article_Visits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Blog_Article_Visits_Blog_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Blog_Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Blog_Article_Comments_ArticleId",
                table: "Blog_Article_Comments",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_Blog_Article_Comments_UserId",
                table: "Blog_Article_Comments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Blog_Article_Visits_ArticleId",
                table: "Blog_Article_Visits",
                column: "ArticleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlackList");

            migrationBuilder.DropTable(
                name: "Blog_Article_Comments");

            migrationBuilder.DropTable(
                name: "Blog_Article_Visits");

            migrationBuilder.DropColumn(
                name: "Views",
                table: "Blog_Articles");

            migrationBuilder.AlterColumn<string>(
                name: "SeoDescription",
                table: "Pages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
