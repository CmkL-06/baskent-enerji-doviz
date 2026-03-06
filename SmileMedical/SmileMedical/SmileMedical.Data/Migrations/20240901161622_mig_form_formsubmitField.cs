using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmileMedical.Data.Migrations
{
    /// <inheritdoc />
    public partial class mig_form_formsubmitField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Form_Submits",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Form_Submits",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Form_Submits");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Form_Submits");
        }
    }
}
