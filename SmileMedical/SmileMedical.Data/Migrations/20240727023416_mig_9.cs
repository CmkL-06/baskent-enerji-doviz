using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmileMedical.Data.Migrations
{
    /// <inheritdoc />
    public partial class mig_9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LanguageId",
                table: "Pages",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "SeoDescription",
                table: "Pages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Pages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Pages_LanguageId",
                table: "Pages",
                column: "LanguageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pages_Languages_LanguageId",
                table: "Pages",
                column: "LanguageId",
                principalTable: "Languages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pages_Languages_LanguageId",
                table: "Pages");

            migrationBuilder.DropIndex(
                name: "IX_Pages_LanguageId",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "LanguageId",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "SeoDescription",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Pages");
        }
    }
}
