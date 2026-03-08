using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneyTransferTurkey.Data.Migrations
{
    /// <inheritdoc />
    public partial class mig_updatepagelogic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pages_Languages_LanguageId",
                table: "Pages");

            migrationBuilder.DropTable(
                name: "Components");

            migrationBuilder.DropColumn(
                name: "PageId",
                table: "Pages");

            migrationBuilder.RenameColumn(
                name: "SeoLink",
                table: "Pages",
                newName: "OgTitle");

            migrationBuilder.RenameColumn(
                name: "SeoDescription",
                table: "Pages",
                newName: "OgImage");

            migrationBuilder.RenameColumn(
                name: "LayoutJson",
                table: "Pages",
                newName: "OgDescription");

            migrationBuilder.RenameColumn(
                name: "IsHome",
                table: "Pages",
                newName: "IsHomePage");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Pages",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<Guid>(
                name: "LanguageId",
                table: "Pages",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "CustomCSS",
                table: "Pages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomJS",
                table: "Pages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Pages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Keywords",
                table: "Pages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LanguageCode",
                table: "Pages",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "en");

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "Pages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Pages",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Pages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "BuilderComponents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    PropsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DesktopStyles = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TabletStyles = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MobileStyles = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomCSS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Locked = table.Column<bool>(type: "bit", nullable: false),
                    Hidden = table.Column<bool>(type: "bit", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuilderComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuilderComponents_BuilderComponents_ParentId",
                        column: x => x.ParentId,
                        principalTable: "BuilderComponents",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BuilderComponents_Pages_PageId",
                        column: x => x.PageId,
                        principalTable: "Pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pages_Slug_LanguageCode",
                table: "Pages",
                columns: new[] { "Slug", "LanguageCode" },
                unique: true,
                filter: "[Slug] != '' AND [LanguageCode] != ''");

            migrationBuilder.CreateIndex(
                name: "IX_BuilderComponents_PageId",
                table: "BuilderComponents",
                column: "PageId");

            migrationBuilder.CreateIndex(
                name: "IX_BuilderComponents_ParentId",
                table: "BuilderComponents",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pages_Languages_LanguageId",
                table: "Pages",
                column: "LanguageId",
                principalTable: "Languages",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pages_Languages_LanguageId",
                table: "Pages");

            migrationBuilder.DropTable(
                name: "BuilderComponents");

            migrationBuilder.DropIndex(
                name: "IX_Pages_Slug_LanguageCode",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "CustomCSS",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "CustomJS",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "Keywords",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "LanguageCode",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Pages");

            migrationBuilder.RenameColumn(
                name: "OgTitle",
                table: "Pages",
                newName: "SeoLink");

            migrationBuilder.RenameColumn(
                name: "OgImage",
                table: "Pages",
                newName: "SeoDescription");

            migrationBuilder.RenameColumn(
                name: "OgDescription",
                table: "Pages",
                newName: "LayoutJson");

            migrationBuilder.RenameColumn(
                name: "IsHomePage",
                table: "Pages",
                newName: "IsHome");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Pages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldDefaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "LanguageId",
                table: "Pages",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PageId",
                table: "Pages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Components",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PropsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Components", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Components_Pages_PageId",
                        column: x => x.PageId,
                        principalTable: "Pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Components_PageId",
                table: "Components",
                column: "PageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pages_Languages_LanguageId",
                table: "Pages",
                column: "LanguageId",
                principalTable: "Languages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
