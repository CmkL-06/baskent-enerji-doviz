using BaskentEnerji.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace BaskentEnerji.Business.Tests
{
    /// <summary>
    /// Testler için gerçek SQL Server (SQLEXPRESS) üzerinde ayrı, canlı veriden tamamen izole bir
    /// veritabanı kullanır. WacService.GetOrCreateWacAsync raw SQL (WITH (UPDLOCK)) kullandığından
    /// EF Core InMemory provider bunu desteklemiyor — bu yüzden gerçek SQL Server gerekiyor.
    ///
    /// DbContext.Database.EnsureCreated() TÜM modeli (tüm tablolar) oluşturmaya çalışır ve bu depoda
    /// önceden var olan, testlerle ilgisiz bir hata yüzünden başarısız olur: ExchangeRateHistories
    /// tablosunun Currencies'e olan iki FK'ı (Source/Target) SQL Server'da "multiple cascade paths"
    /// hatası veriyor. Bu, canlı veritabanının kademeli migration'larla inşa edildiği için hiç ortaya
    /// çıkmamış, ama sıfırdan tek seferde oluşturulmaya çalışılınca (tam olarak burada olduğu gibi)
    /// yüzeye çıkan gerçek bir konfigürasyon sorunu — ayrı bir konu olarak not edildi, bu testlerin
    /// kapsamı dışında bırakıldı.
    ///
    /// Bu yüzden burada sadece WacService'in (ve ileride diğer servislerin) ihtiyaç duyduğu tablolar
    /// ham SQL ile, üretim şemasıyla birebir aynı sütun/tip eşleşmesiyle oluşturuluyor.
    /// </summary>
    public static class TestDbContextFactory
    {
        private const string ConnectionString =
            @"Server=.\SQLEXPRESS;Database=BaskentEnerjiTests;Trusted_Connection=True;TrustServerCertificate=True;";

        public static BaskentEnerjiDbContext Create()
        {
            var options = new DbContextOptionsBuilder<BaskentEnerjiDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;
            return new BaskentEnerjiDbContext(options);
        }

        public static void EnsureCreated()
        {
            using var ctx = Create();
            // NOT: ctx.Database.EnsureCreated() KASITLI OLARAK kullanılmıyor — BaskentEnerjiDbContext'in
            // TÜM modelini (tüm tablolar) oluşturmaya çalışır ve yukarıdaki class-level yorumda açıklanan
            // önceden var olan cascade-cycle hatasına takılır. Bunun yerine sadece aşağıdaki hedefli
            // tabloları oluşturuyoruz; veritabanının kendisinin var olduğu varsayılıyor (bkz. test ortamı
            // kurulumu — "CREATE DATABASE BaskentEnerjiTests" elle çalıştırıldı).

            // Üretim şemasıyla birebir aynı — sqlcmd ile INFORMATION_SCHEMA.COLUMNS'tan doğrulandı.
            ctx.Database.ExecuteSqlRaw(@"
IF OBJECT_ID('dbo.Offices') IS NULL
CREATE TABLE dbo.Offices (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    OfficeName nvarchar(max) NOT NULL,
    OfficeDescription nvarchar(max) NULL,
    OfficeImageUri nvarchar(max) NULL,
    Address nvarchar(max) NULL,
    Phone nvarchar(max) NULL,
    IsActive bit NOT NULL DEFAULT 1,
    OfficeType int NOT NULL DEFAULT 0,
    ParentOfficeId uniqueidentifier NULL,
    DailyTransactionLimit decimal(18,2) NULL,
    MonthlyTransactionLimit decimal(18,2) NULL,
    CommissionRate decimal(18,2) NULL,
    RateInheritanceMode int NOT NULL DEFAULT 0,
    TransferApprovalThreshold decimal(18,2) NULL,
    CreatedDate datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
IF OBJECT_ID('dbo.Currencies') IS NULL
CREATE TABLE dbo.Currencies (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    CurrencyCode nvarchar(max) NOT NULL,
    CurrencyName nvarchar(max) NOT NULL DEFAULT '',
    CurrencySymbol nvarchar(max) NOT NULL DEFAULT '',
    DecimalPlaces int NOT NULL DEFAULT 2,
    CreatedDate datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
IF OBJECT_ID('dbo.Vaults') IS NULL
CREATE TABLE dbo.Vaults (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    OfficeId uniqueidentifier NOT NULL,
    Name nvarchar(max) NOT NULL,
    Description nvarchar(max) NOT NULL DEFAULT '',
    Type int NOT NULL DEFAULT 1,
    IsActive bit NOT NULL DEFAULT 1,
    ClosingBalance decimal(18,2) NOT NULL DEFAULT 0,
    ClosedDate datetime2 NULL,
    ShouldCount bit NOT NULL DEFAULT 0,
    LastCountDate datetime2 NULL,
    CreatedDate datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
IF OBJECT_ID('dbo.CurrencyWacs') IS NULL
CREATE TABLE dbo.CurrencyWacs (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    VaultId uniqueidentifier NOT NULL,
    CurrencyId uniqueidentifier NOT NULL,
    Wac decimal(18,2) NOT NULL,
    Quantity decimal(18,2) NOT NULL,
    LastUpdated datetime2 NOT NULL,
    CreatedDate datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
-- Denetim raporu düzeltmesi: üretim şemasıyla birebir aynı unique index (bkz.
-- AddUniqueIndexOnCurrencyWacs migration'ı) — WacService.GetOrCreateWacAsync'teki ilk-alış race
-- condition'ının retry mantığını test edebilmek için testlerde de bu kısıt gerekli.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_CurrencyWacs_VaultId_CurrencyId' AND object_id = OBJECT_ID('dbo.CurrencyWacs'))
CREATE UNIQUE INDEX IX_CurrencyWacs_VaultId_CurrencyId ON dbo.CurrencyWacs(VaultId, CurrencyId);
IF OBJECT_ID('dbo.CurrencyWacHistories') IS NULL
CREATE TABLE dbo.CurrencyWacHistories (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    VaultId uniqueidentifier NOT NULL,
    CurrencyId uniqueidentifier NOT NULL,
    OldWac decimal(18,2) NOT NULL,
    NewWac decimal(18,2) NOT NULL,
    OldQuantity decimal(18,2) NOT NULL,
    NewQuantity decimal(18,2) NOT NULL,
    TransactionAmount decimal(18,2) NOT NULL,
    TransactionRate decimal(18,2) NOT NULL,
    TransactionId uniqueidentifier NULL,
    Reason int NOT NULL,
    CreatedDate datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
IF OBJECT_ID('dbo.Users') IS NULL
CREATE TABLE dbo.Users (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    Username nvarchar(max) NOT NULL,
    Password nvarchar(max) NOT NULL DEFAULT '',
    Mail nvarchar(max) NOT NULL DEFAULT '',
    IsEmailVerified bit NOT NULL DEFAULT 0,
    Firstname nvarchar(max) NOT NULL DEFAULT '',
    Lastname nvarchar(max) NOT NULL DEFAULT '',
    Gender int NOT NULL DEFAULT 0,
    Rank int NOT NULL DEFAULT 0,
    LanguageCode nvarchar(max) NULL,
    FirstIp nvarchar(max) NULL,
    LastIp nvarchar(max) NULL,
    OfficeId uniqueidentifier NULL,
    LastPasswordChangeDate datetime2 NULL,
    DealerReferralCode nvarchar(max) NULL,
    TelegramOperatorId bigint NULL,
    LastActivityDate datetime2 NULL,
    LastLoginDate datetime2 NULL,
    CreatedDate datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
IF OBJECT_ID('dbo.VaultBalances') IS NULL
CREATE TABLE dbo.VaultBalances (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    VaultId uniqueidentifier NOT NULL,
    CurrencyId uniqueidentifier NOT NULL,
    Balance decimal(18,6) NOT NULL DEFAULT 0,
    ReservedAmount decimal(18,6) NOT NULL DEFAULT 0,
    LastUpdated datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedDate datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
IF OBJECT_ID('dbo.Transactions') IS NULL
CREATE TABLE dbo.Transactions (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    TransactionNumber nvarchar(max) NULL,
    VaultId uniqueidentifier NOT NULL,
    CustomerId uniqueidentifier NULL,
    UserId uniqueidentifier NOT NULL,
    Type int NOT NULL DEFAULT 0,
    TransactionDate datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    Status int NOT NULL DEFAULT 0,
    Notes nvarchar(max) NULL,
    IsDeleted bit NOT NULL DEFAULT 0,
    Profit decimal(18,6) NOT NULL DEFAULT 0,
    IsCustomRate bit NOT NULL DEFAULT 0,
    PartyId uniqueidentifier NULL,
    DeletedReason nvarchar(max) NULL,
    deletedByUserId uniqueidentifier NULL,
    CreatedDate datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
IF OBJECT_ID('dbo.DayClosures') IS NULL
CREATE TABLE dbo.DayClosures (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    OfficeId uniqueidentifier NOT NULL,
    VaultId uniqueidentifier NOT NULL,
    BusinessDate datetime2 NOT NULL,
    ClosedByUserId uniqueidentifier NOT NULL,
    ClosedAt datetime2 NOT NULL,
    Status int NOT NULL,
    VaultCountId uniqueidentifier NULL,
    TotalRealizedProfit decimal(18,2) NOT NULL DEFAULT 0,
    TransactionCount int NOT NULL DEFAULT 0,
    Notes nvarchar(max) NOT NULL DEFAULT '',
    IsAutoGenerated bit NOT NULL DEFAULT 0,
    ApprovedAt datetime2 NULL,
    ApprovedByUserId uniqueidentifier NULL,
    RejectionNote nvarchar(max) NOT NULL DEFAULT '',
    CreatedDate datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
IF OBJECT_ID('dbo.DayClosureDetails') IS NULL
CREATE TABLE dbo.DayClosureDetails (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    DayClosureId uniqueidentifier NOT NULL,
    CurrencyId uniqueidentifier NOT NULL,
    SystemBalance decimal(18,2) NOT NULL,
    PhysicalCount decimal(18,2) NOT NULL,
    Discrepancy decimal(18,2) NOT NULL,
    DiscrepancyNote nvarchar(max) NOT NULL DEFAULT '',
    WacAtClose decimal(18,2) NOT NULL,
    OpeningBalance decimal(18,2) NOT NULL,
    OpeningWac decimal(18,2) NOT NULL,
    CreatedDate datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
IF OBJECT_ID('dbo.VaultCounts') IS NULL
CREATE TABLE dbo.VaultCounts (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    VaultId uniqueidentifier NOT NULL,
    OfficeId uniqueidentifier NOT NULL,
    UserId uniqueidentifier NOT NULL,
    CountDate datetime2 NOT NULL,
    HasDiscrepancy bit NOT NULL DEFAULT 0,
    DiscrepancyDetails nvarchar(max) NOT NULL DEFAULT '',
    IsSystemGenerated bit NOT NULL DEFAULT 0,
    CreatedDate datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
-- NOT: Tablo adi kasitli olarak TEKIL (VaultCountDetail) -- EF'in su anki modeli (model snapshot'ta
-- b.ToTable(VaultCountDetail) olarak dogrulandi) bu entity icin DbSet tanimlamiyor, bu yuzden
-- varsayilan (tekil, sinif adi) isimlendirmeyi kullaniyor. Uretim veritabaninda AYRICA VaultCountDetails
-- (cogul) adinda, 0 satirlik, kullanilmayan/artik kalmis bir tablo daha var -- muhtemelen eski bir sema
-- sürümünden kalma; gerçek kod hep tekil olana yazıyor. Bu, testler sırasında keşfedilen ayrı bir
-- temizlik konusu, bu PR'ın kapsamı dışında bırakıldı.
IF OBJECT_ID('dbo.VaultCountDetail') IS NULL
CREATE TABLE dbo.VaultCountDetail (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    VaultCountId uniqueidentifier NOT NULL,
    CurrencyId uniqueidentifier NOT NULL,
    ActualAmount decimal(18,2) NOT NULL,
    SystemAmount decimal(18,2) NOT NULL,
    Discrepancy decimal(18,2) NOT NULL,
    CreatedDate datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
IF OBJECT_ID('dbo.ExchangeRates') IS NULL
CREATE TABLE dbo.ExchangeRates (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    SourceCurrencyId uniqueidentifier NOT NULL,
    TargetCurrencyId uniqueidentifier NOT NULL,
    BuyRate decimal(18,6) NOT NULL,
    SellRate decimal(18,6) NOT NULL,
    UpdatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    EffectiveFrom datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    EffectiveTo datetime2 NULL,
    IsActive bit NOT NULL DEFAULT 1,
    OfficeId uniqueidentifier NULL,
    CreatedDate datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
IF OBJECT_ID('dbo.VaultBalanceHistories') IS NULL
CREATE TABLE dbo.VaultBalanceHistories (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    VaultId uniqueidentifier NOT NULL,
    CurrencyId uniqueidentifier NOT NULL,
    Balance decimal(18,6) NOT NULL,
    Description nvarchar(max) NULL,
    TransactionType int NOT NULL,
    UserId uniqueidentifier NULL,
    IsDeleted bit NOT NULL DEFAULT 0,
    IsGhost bit NOT NULL DEFAULT 0,
    IsParty bit NOT NULL DEFAULT 0,
    TransferReferenceId uniqueidentifier NULL,
    DeletedByUserId uniqueidentifier NULL,
    DeletedReason nvarchar(max) NULL,
    CreatedDate datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
IF OBJECT_ID('dbo.TransactionDetails') IS NULL
CREATE TABLE dbo.TransactionDetails (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    TransactionId uniqueidentifier NOT NULL,
    CurrencyId uniqueidentifier NOT NULL,
    Side int NOT NULL,
    Amount decimal(18,6) NOT NULL,
    Rate decimal(18,6) NOT NULL,
    Commission decimal(18,6) NOT NULL DEFAULT 0,
    NetAmount decimal(18,6) NOT NULL DEFAULT 0,
    ActualBuyRate decimal(18,6) NULL,
    ActualSellRate decimal(18,6) NULL,
    CustomRate decimal(18,6) NULL,
    CreatedDate datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
IF OBJECT_ID('dbo.Parties') IS NULL
CREATE TABLE dbo.Parties (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    OfficeId uniqueidentifier NOT NULL,
    PartyCode nvarchar(max) NOT NULL DEFAULT '',
    Name nvarchar(max) NOT NULL DEFAULT '',
    Type int NOT NULL DEFAULT 0,
    Status int NOT NULL DEFAULT 0,
    ContactPerson nvarchar(max) NULL,
    TaxNumber nvarchar(max) NULL,
    RegistrationNumber nvarchar(max) NULL,
    Phone nvarchar(max) NULL,
    Email nvarchar(max) NULL,
    Address nvarchar(max) NULL,
    City nvarchar(max) NULL,
    Country nvarchar(max) NULL,
    Notes nvarchar(max) NULL,
    IsActive bit NOT NULL DEFAULT 1,
    HasCreditLimit bit NOT NULL DEFAULT 0,
    DefaultPaymentTermDays int NOT NULL DEFAULT 0,
    LastTransactionDate datetime2 NULL,
    TotalVolume decimal(18,6) NOT NULL DEFAULT 0,
    CreatedByUserId uniqueidentifier NULL,
    ModifiedByUserId uniqueidentifier NULL,
    ModifiedDate datetime2 NULL,
    CreatedDate datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
IF OBJECT_ID('dbo.OfficeTransfers') IS NULL
CREATE TABLE dbo.OfficeTransfers (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    SourceVaultId uniqueidentifier NOT NULL,
    TargetVaultId uniqueidentifier NOT NULL,
    CurrencyId uniqueidentifier NOT NULL,
    Amount decimal(18,6) NOT NULL,
    Status int NOT NULL DEFAULT 0,
    RequestedByUserId uniqueidentifier NOT NULL,
    ApprovedByUserId uniqueidentifier NULL,
    Notes nvarchar(max) NULL,
    RejectionReason nvarchar(max) NULL,
    ProcessedAt datetime2 NULL,
    CreatedDate datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    -- OfficeId/OfficeId1: Office.OutgoingTransfers/IncomingTransfers navigasyon koleksiyonları hiçbir
    -- yerde Fluent API ile yapılandırılmadığı için EF Core convention'ı bunları OfficeTransfer'a
    -- örtük (shadow) FK olarak bağlıyor. Canlı veritabanında da gerçekten var (sqlcmd ile doğrulandı) —
    -- hiçbir kod bunları hiç set etmiyor, bu yüzden bu iki sütun kalıcı olarak NULL kalan ölü şema.
    OfficeId uniqueidentifier NULL,
    OfficeId1 uniqueidentifier NULL
);
IF OBJECT_ID('dbo.User_Offices') IS NULL
CREATE TABLE dbo.User_Offices (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    UserId uniqueidentifier NOT NULL,
    OfficeId uniqueidentifier NOT NULL,
    Role int NOT NULL DEFAULT 2,
    IsActive bit NOT NULL DEFAULT 1,
    CreatedDate datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
IF OBJECT_ID('dbo.ExpenseCategories') IS NULL
CREATE TABLE dbo.ExpenseCategories (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    Name nvarchar(100) NOT NULL,
    IsActive bit NOT NULL DEFAULT 1,
    CreatedDate datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ExpenseCategories_Name' AND object_id = OBJECT_ID('dbo.ExpenseCategories'))
CREATE UNIQUE INDEX IX_ExpenseCategories_Name ON dbo.ExpenseCategories(Name);
IF OBJECT_ID('dbo.ExpenseDefinitions') IS NULL
CREATE TABLE dbo.ExpenseDefinitions (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    OfficeId uniqueidentifier NOT NULL,
    Code nvarchar(450) NOT NULL DEFAULT '',
    Name nvarchar(max) NOT NULL DEFAULT '',
    CategoryId uniqueidentifier NOT NULL,
    Description nvarchar(max) NULL,
    IsActive bit NOT NULL DEFAULT 1,
    IsRecurring bit NOT NULL DEFAULT 0,
    RecurrencePeriod int NULL,
    DefaultAmount decimal(18,4) NULL,
    DefaultCurrencyId uniqueidentifier NULL,
    CreatedDate datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
-- Eski (Category int) şemadan yükseltme — enum'dan entity'ye geçiş. Statik SQL, henüz var
-- olmayan CategoryId kolonuna IF bloğu içinde bile derleme zamanında bağlanmaya çalışır (SQL
-- Server ad-hoc batch davranışı) — bu yüzden EXEC() ile çalışma zamanına erteleniyor.
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ExpenseDefinitions') AND name = 'Category')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ExpenseDefinitions') AND name = 'CategoryId')
        EXEC('ALTER TABLE dbo.ExpenseDefinitions ADD CategoryId uniqueidentifier NULL');
    EXEC('UPDATE dbo.ExpenseDefinitions SET CategoryId = ''11111111-1111-1111-1111-111111111110'' WHERE CategoryId IS NULL');
    EXEC('ALTER TABLE dbo.ExpenseDefinitions ALTER COLUMN CategoryId uniqueidentifier NOT NULL');
    EXEC('ALTER TABLE dbo.ExpenseDefinitions DROP COLUMN Category');
END
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ExpenseDefinitions') AND name = 'AccountReference')
ALTER TABLE dbo.ExpenseDefinitions ADD AccountReference nvarchar(100) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ExpenseDefinitions') AND name = 'DueDayOfMonth')
ALTER TABLE dbo.ExpenseDefinitions ADD DueDayOfMonth int NULL;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ExpenseDefinitions') AND name = 'Code' AND max_length = -1)
BEGIN
    DECLARE @codeDefaultName nvarchar(200) = (
        SELECT dc.name FROM sys.default_constraints dc
        JOIN sys.columns c ON c.default_object_id = dc.object_id
        WHERE dc.parent_object_id = OBJECT_ID('dbo.ExpenseDefinitions') AND c.name = 'Code'
    );
    IF @codeDefaultName IS NOT NULL
        EXEC('ALTER TABLE dbo.ExpenseDefinitions DROP CONSTRAINT ' + @codeDefaultName);
    ALTER TABLE dbo.ExpenseDefinitions ALTER COLUMN Code nvarchar(450) NOT NULL;
END
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ExpenseDefinitions_OfficeId_Code' AND object_id = OBJECT_ID('dbo.ExpenseDefinitions'))
CREATE UNIQUE INDEX IX_ExpenseDefinitions_OfficeId_Code ON dbo.ExpenseDefinitions(OfficeId, Code);
IF OBJECT_ID('dbo.ExpensePayments') IS NULL
CREATE TABLE dbo.ExpensePayments (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    ExpenseDefinitionId uniqueidentifier NOT NULL,
    VaultId uniqueidentifier NOT NULL,
    CurrencyId uniqueidentifier NOT NULL,
    PaymentNumber nvarchar(max) NOT NULL DEFAULT '',
    PaymentDate datetime2 NOT NULL,
    Amount decimal(18,4) NOT NULL,
    PaymentMethod int NOT NULL,
    ReferenceNumber nvarchar(max) NULL,
    Description nvarchar(max) NULL,
    Receipt nvarchar(max) NULL,
    Status int NOT NULL,
    IsDeleted bit NOT NULL DEFAULT 0,
    DeletedReason nvarchar(max) NULL,
    DeletedByUserId uniqueidentifier NULL,
    DeletedDate datetime2 NULL,
    CreatedByUserId uniqueidentifier NOT NULL,
    ApprovedByUserId uniqueidentifier NULL,
    ApprovedAt datetime2 NULL,
    RejectionNote nvarchar(max) NULL,
    CreatedDate datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
IF OBJECT_ID('dbo.ExpenseBudgets') IS NULL
CREATE TABLE dbo.ExpenseBudgets (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    OfficeId uniqueidentifier NOT NULL,
    CategoryId uniqueidentifier NOT NULL,
    Year int NOT NULL,
    Month int NOT NULL,
    BudgetAmount decimal(18,4) NOT NULL,
    CreatedByUserId uniqueidentifier NOT NULL,
    CreatedDate datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
-- Eski (Category int) şemadan yükseltme — enum'dan entity'ye geçiş (EXEC ile çalışma zamanına ertelenir)
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ExpenseBudgets') AND name = 'Category')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ExpenseBudgets') AND name = 'CategoryId')
        EXEC('ALTER TABLE dbo.ExpenseBudgets ADD CategoryId uniqueidentifier NULL');
    EXEC('UPDATE dbo.ExpenseBudgets SET CategoryId = ''11111111-1111-1111-1111-111111111110'' WHERE CategoryId IS NULL');
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ExpenseBudgets_OfficeId_Category_Year_Month' AND object_id = OBJECT_ID('dbo.ExpenseBudgets'))
        EXEC('DROP INDEX IX_ExpenseBudgets_OfficeId_Category_Year_Month ON dbo.ExpenseBudgets');
    EXEC('ALTER TABLE dbo.ExpenseBudgets ALTER COLUMN CategoryId uniqueidentifier NOT NULL');
    EXEC('ALTER TABLE dbo.ExpenseBudgets DROP COLUMN Category');
END
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ExpenseBudgets_OfficeId_CategoryId_Year_Month' AND object_id = OBJECT_ID('dbo.ExpenseBudgets'))
EXEC('CREATE UNIQUE INDEX IX_ExpenseBudgets_OfficeId_CategoryId_Year_Month ON dbo.ExpenseBudgets(OfficeId, CategoryId, Year, Month)');
IF OBJECT_ID('dbo.PartyAccounts') IS NULL
CREATE TABLE dbo.PartyAccounts (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    PartyId uniqueidentifier NOT NULL,
    CurrencyId uniqueidentifier NOT NULL,
    AccountNumber nvarchar(max) NOT NULL DEFAULT '',
    Balance decimal(18,6) NOT NULL DEFAULT 0,
    BlockedAmount decimal(18,6) NOT NULL DEFAULT 0,
    CreditLimit decimal(18,6) NOT NULL DEFAULT 0,
    PaymentTermDays int NOT NULL DEFAULT 0,
    LastActivityDate datetime2 NULL,
    TotalDebits decimal(18,6) NOT NULL DEFAULT 0,
    TotalCredits decimal(18,6) NOT NULL DEFAULT 0,
    TransactionCount int NOT NULL DEFAULT 0,
    LastTransactionDate datetime2 NULL,
    Status int NOT NULL DEFAULT 0,
    IsActive bit NOT NULL DEFAULT 1,
    CreatedDate datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
");
        }
    }
}
