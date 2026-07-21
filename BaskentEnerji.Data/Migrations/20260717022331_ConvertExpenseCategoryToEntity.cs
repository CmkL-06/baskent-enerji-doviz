using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaskentEnerji.Data.Migrations
{
    /// <inheritdoc />
    // Elle yazıldı (scaffold edilen sürüm ÖNCE eski Category int kolonunu düşürüyordu — veri
    // kaybı riski). Sıra: yeni tablo + seed -> nullable CategoryId ekle -> eski veriden backfill
    // -> NOT NULL yap + FK/index kur -> ancak O ZAMAN eski Category kolonunu düşür.
    public partial class ConvertExpenseCategoryToEntity : Migration
    {
        // Eski ExpenseCategory enum'undaki 11 değere birebir karşılık gelen, deterministik GUID'ler.
        private const string SalaryId = "11111111-1111-1111-1111-111111111101";
        private const string RentId = "11111111-1111-1111-1111-111111111102";
        private const string UtilitiesId = "11111111-1111-1111-1111-111111111103";
        private const string OfficeId = "11111111-1111-1111-1111-111111111104";
        private const string MarketingId = "11111111-1111-1111-1111-111111111105";
        private const string TravelId = "11111111-1111-1111-1111-111111111106";
        private const string InsuranceId = "11111111-1111-1111-1111-111111111107";
        private const string TaxId = "11111111-1111-1111-1111-111111111108";
        private const string MaintenanceId = "11111111-1111-1111-1111-111111111109";
        private const string OtherId = "11111111-1111-1111-1111-111111111110";
        private const string StaffMealsId = "11111111-1111-1111-1111-111111111111";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Yeni tablo
            migrationBuilder.CreateTable(
                name: "ExpenseCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseCategories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_Name",
                table: "ExpenseCategories",
                column: "Name",
                unique: true);

            // 2) Eski enum'un 11 değerini seed et
            migrationBuilder.InsertData(
                table: "ExpenseCategories",
                columns: new[] { "Id", "Name", "IsActive", "CreatedDate" },
                values: new object[,]
                {
                    { new Guid(SalaryId), "Maaş", true, DateTime.UtcNow },
                    { new Guid(RentId), "Kira", true, DateTime.UtcNow },
                    { new Guid(UtilitiesId), "Faturalar", true, DateTime.UtcNow },
                    { new Guid(OfficeId), "Ofis Giderleri", true, DateTime.UtcNow },
                    { new Guid(MarketingId), "Pazarlama", true, DateTime.UtcNow },
                    { new Guid(TravelId), "Seyahat", true, DateTime.UtcNow },
                    { new Guid(InsuranceId), "Sigorta", true, DateTime.UtcNow },
                    { new Guid(TaxId), "Vergi", true, DateTime.UtcNow },
                    { new Guid(MaintenanceId), "Bakım", true, DateTime.UtcNow },
                    { new Guid(OtherId), "Diğer", true, DateTime.UtcNow },
                    { new Guid(StaffMealsId), "Personel Yemek", true, DateTime.UtcNow }
                });

            // 3) Nullable CategoryId ekle — eski Category kolonuna henüz DOKUNULMUYOR
            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "ExpenseDefinitions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "ExpenseBudgets",
                type: "uniqueidentifier",
                nullable: true);

            // 4) Backfill — mevcut satırlardaki eski int Category değerine göre eşle
            migrationBuilder.Sql($@"
                UPDATE ExpenseDefinitions SET CategoryId = CASE Category
                    WHEN 1 THEN '{SalaryId}'
                    WHEN 2 THEN '{RentId}'
                    WHEN 3 THEN '{UtilitiesId}'
                    WHEN 4 THEN '{OfficeId}'
                    WHEN 5 THEN '{MarketingId}'
                    WHEN 6 THEN '{TravelId}'
                    WHEN 7 THEN '{InsuranceId}'
                    WHEN 8 THEN '{TaxId}'
                    WHEN 9 THEN '{MaintenanceId}'
                    WHEN 11 THEN '{StaffMealsId}'
                    ELSE '{OtherId}'
                END;

                UPDATE ExpenseBudgets SET CategoryId = CASE Category
                    WHEN 1 THEN '{SalaryId}'
                    WHEN 2 THEN '{RentId}'
                    WHEN 3 THEN '{UtilitiesId}'
                    WHEN 4 THEN '{OfficeId}'
                    WHEN 5 THEN '{MarketingId}'
                    WHEN 6 THEN '{TravelId}'
                    WHEN 7 THEN '{InsuranceId}'
                    WHEN 8 THEN '{TaxId}'
                    WHEN 9 THEN '{MaintenanceId}'
                    WHEN 11 THEN '{StaffMealsId}'
                    ELSE '{OtherId}'
                END;
            ");

            // 5) Eski unique index'i düşür (CategoryId bazlı yenisi aşağıda kurulacak)
            migrationBuilder.DropIndex(
                name: "IX_ExpenseBudgets_OfficeId_Category_Year_Month",
                table: "ExpenseBudgets");

            // 6) Backfill tamamlandı — artık NOT NULL yapılabilir
            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "ExpenseDefinitions",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "ExpenseBudgets",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            // 7) Index + FK
            migrationBuilder.CreateIndex(
                name: "IX_ExpenseDefinitions_CategoryId",
                table: "ExpenseDefinitions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseBudgets_CategoryId",
                table: "ExpenseBudgets",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseBudgets_OfficeId_CategoryId_Year_Month",
                table: "ExpenseBudgets",
                columns: new[] { "OfficeId", "CategoryId", "Year", "Month" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseBudgets_ExpenseCategories_CategoryId",
                table: "ExpenseBudgets",
                column: "CategoryId",
                principalTable: "ExpenseCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseDefinitions_ExpenseCategories_CategoryId",
                table: "ExpenseDefinitions",
                column: "CategoryId",
                principalTable: "ExpenseCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // 8) Artık güvenli — eski enum int kolonlarını düşür
            migrationBuilder.DropColumn(
                name: "Category",
                table: "ExpenseDefinitions");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "ExpenseBudgets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "ExpenseDefinitions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "ExpenseBudgets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql($@"
                UPDATE ExpenseDefinitions SET Category = CASE CategoryId
                    WHEN '{SalaryId}' THEN 1
                    WHEN '{RentId}' THEN 2
                    WHEN '{UtilitiesId}' THEN 3
                    WHEN '{OfficeId}' THEN 4
                    WHEN '{MarketingId}' THEN 5
                    WHEN '{TravelId}' THEN 6
                    WHEN '{InsuranceId}' THEN 7
                    WHEN '{TaxId}' THEN 8
                    WHEN '{MaintenanceId}' THEN 9
                    WHEN '{StaffMealsId}' THEN 11
                    ELSE 10
                END;

                UPDATE ExpenseBudgets SET Category = CASE CategoryId
                    WHEN '{SalaryId}' THEN 1
                    WHEN '{RentId}' THEN 2
                    WHEN '{UtilitiesId}' THEN 3
                    WHEN '{OfficeId}' THEN 4
                    WHEN '{MarketingId}' THEN 5
                    WHEN '{TravelId}' THEN 6
                    WHEN '{InsuranceId}' THEN 7
                    WHEN '{TaxId}' THEN 8
                    WHEN '{MaintenanceId}' THEN 9
                    WHEN '{StaffMealsId}' THEN 11
                    ELSE 10
                END;
            ");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseBudgets_ExpenseCategories_CategoryId",
                table: "ExpenseBudgets");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseDefinitions_ExpenseCategories_CategoryId",
                table: "ExpenseDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseDefinitions_CategoryId",
                table: "ExpenseDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseBudgets_CategoryId",
                table: "ExpenseBudgets");

            migrationBuilder.DropIndex(
                name: "IX_ExpenseBudgets_OfficeId_CategoryId_Year_Month",
                table: "ExpenseBudgets");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "ExpenseDefinitions");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "ExpenseBudgets");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseBudgets_OfficeId_Category_Year_Month",
                table: "ExpenseBudgets",
                columns: new[] { "OfficeId", "Category", "Year", "Month" },
                unique: true);

            migrationBuilder.DropTable(
                name: "ExpenseCategories");
        }
    }
}
