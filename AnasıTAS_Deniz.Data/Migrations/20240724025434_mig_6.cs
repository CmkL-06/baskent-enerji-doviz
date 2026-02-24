using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnasıTAS_Deniz.Data.Migrations
{
    /// <inheritdoc />
    public partial class mig_6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SeoLink",
                table: "Tags",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Menus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsOnNavbar = table.Column<bool>(type: "bit", nullable: false),
                    IsOnAside = table.Column<bool>(type: "bit", nullable: false),
                    IsOnFooter = table.Column<bool>(type: "bit", nullable: false),
                    IsForMobile = table.Column<bool>(type: "bit", nullable: false),
                    IsCategoryMenu = table.Column<bool>(type: "bit", nullable: false),
                    Blog_CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParentMenuId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Menus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Menus_Blog_Categories_Blog_CategoryId",
                        column: x => x.Blog_CategoryId,
                        principalTable: "Blog_Categories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Menus_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Menus_Menus_ParentMenuId",
                        column: x => x.ParentMenuId,
                        principalTable: "Menus",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Menu_Items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SeoTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SeoLink = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MenuId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsSingleCategoryItem = table.Column<bool>(type: "bit", nullable: false),
                    IsMainCategoryItem = table.Column<bool>(type: "bit", nullable: false),
                    IsCustomLink = table.Column<bool>(type: "bit", nullable: false),
                    MainCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Blog_CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Menu_Items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Menu_Items_Blog_Categories_Blog_CategoryId",
                        column: x => x.Blog_CategoryId,
                        principalTable: "Blog_Categories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Menu_Items_Blog_Categories_MainCategoryId",
                        column: x => x.MainCategoryId,
                        principalTable: "Blog_Categories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Menu_Items_Menus_MenuId",
                        column: x => x.MenuId,
                        principalTable: "Menus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Menu_Items_Blog_CategoryId",
                table: "Menu_Items",
                column: "Blog_CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Menu_Items_MainCategoryId",
                table: "Menu_Items",
                column: "MainCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Menu_Items_MenuId",
                table: "Menu_Items",
                column: "MenuId");

            migrationBuilder.CreateIndex(
                name: "IX_Menus_Blog_CategoryId",
                table: "Menus",
                column: "Blog_CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Menus_LanguageId",
                table: "Menus",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_Menus_ParentMenuId",
                table: "Menus",
                column: "ParentMenuId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Menu_Items");

            migrationBuilder.DropTable(
                name: "Menus");

            migrationBuilder.DropColumn(
                name: "SeoLink",
                table: "Tags");
        }
    }
}
