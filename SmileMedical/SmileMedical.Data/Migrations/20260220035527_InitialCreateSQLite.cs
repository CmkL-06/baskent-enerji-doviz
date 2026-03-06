using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmileMedical.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateSQLite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "mtturkey_exchange");

            migrationBuilder.CreateTable(
                name: "BlackList",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IpAdress = table.Column<string>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlackList", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Coins",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Icon = table.Column<string>(type: "TEXT", nullable: true),
                    Cover = table.Column<string>(type: "TEXT", nullable: true),
                    ShortDescription = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    FixedPrice = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Exchange = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coins", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Currencies",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    CurrencyName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CurrencySymbol = table.Column<string>(type: "TEXT", nullable: false),
                    DecimalPlaces = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExchangeSettings",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsAutoUpdateEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdateIntervalMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    StartHour = table.Column<int>(type: "INTEGER", nullable: false),
                    EndHour = table.Column<int>(type: "INTEGER", nullable: false),
                    WorkDays = table.Column<string>(type: "TEXT", nullable: true),
                    TryBasedMarginPercent = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    CrossFiatMarginPercent = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    CryptoMarginPercent = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    CurrencySpecificMargins = table.Column<string>(type: "TEXT", nullable: true),
                    UseTcmb = table.Column<bool>(type: "INTEGER", nullable: false),
                    UseDovizCom = table.Column<bool>(type: "INTEGER", nullable: false),
                    UseBinance = table.Column<bool>(type: "INTEGER", nullable: false),
                    RateSelectionStrategy = table.Column<string>(type: "TEXT", nullable: true),
                    MaxPriceChangePercent = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    RequireApprovalAboveThreshold = table.Column<bool>(type: "INTEGER", nullable: false),
                    NotificationEmails = table.Column<string>(type: "TEXT", nullable: true),
                    SendMobileNotifications = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastAutoUpdate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalDataSources",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceName = table.Column<string>(type: "TEXT", nullable: true),
                    SourceType = table.Column<string>(type: "TEXT", nullable: true),
                    SourceKey = table.Column<string>(type: "TEXT", nullable: true),
                    BaseUrl = table.Column<string>(type: "TEXT", nullable: true),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Configuration = table.Column<string>(type: "TEXT", nullable: true),
                    LastSuccessfulFetch = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastError = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalDataSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalRateCaches",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: true),
                    CurrencyCode = table.Column<string>(type: "TEXT", nullable: true),
                    TargetCurrencyCode = table.Column<string>(type: "TEXT", nullable: true),
                    BuyRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    SellRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    SpreadPercent = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    FetchedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsValid = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    SourceUrl = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalRateCaches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Forms",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    FormStructure = table.Column<string>(type: "NVARCHAR(MAX)", nullable: false),
                    CustomName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    SubTitle = table.Column<string>(type: "TEXT", nullable: false),
                    Footer = table.Column<string>(type: "TEXT", nullable: false),
                    SubmitMessage = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Forms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GlobalColors",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ColorsJson = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalColors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Languages",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    LanguageName = table.Column<string>(type: "TEXT", nullable: false),
                    LanguageCode = table.Column<string>(type: "TEXT", nullable: false),
                    FlagUri = table.Column<string>(type: "TEXT", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MediaFiles",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    OriginalName = table.Column<string>(type: "TEXT", nullable: false),
                    FileUrl = table.Column<string>(type: "TEXT", nullable: false),
                    FileType = table.Column<string>(type: "TEXT", nullable: false),
                    MimeType = table.Column<string>(type: "TEXT", nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    Width = table.Column<int>(type: "INTEGER", nullable: true),
                    Height = table.Column<int>(type: "INTEGER", nullable: true),
                    AltText = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    FolderPath = table.Column<string>(type: "TEXT", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    UploadedBy = table.Column<int>(type: "INTEGER", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Offices",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OfficeName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OfficeDescription = table.Column<string>(type: "TEXT", nullable: true),
                    OfficeImageUri = table.Column<string>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    Phone = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Site_Settings",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SiteName = table.Column<string>(type: "TEXT", nullable: true),
                    SiteUrl = table.Column<string>(type: "TEXT", nullable: true),
                    LogoUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Phone = table.Column<string>(type: "TEXT", nullable: true),
                    FacebookUrl = table.Column<string>(type: "TEXT", nullable: true),
                    TwitterUrl = table.Column<string>(type: "TEXT", nullable: true),
                    InstagramUrl = table.Column<string>(type: "TEXT", nullable: true),
                    LinkedinUrl = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Site_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Themes",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Colors = table.Column<string>(type: "TEXT", nullable: true),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDark = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Themes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Coin_Pairs",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceCoinId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Pair = table.Column<string>(type: "TEXT", nullable: false),
                    CoinId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coin_Pairs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Coin_Pairs_Coins_CoinId",
                        column: x => x.CoinId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Coins",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Form_Submits",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FormId = table.Column<Guid>(type: "TEXT", nullable: false),
                    data = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    IpAdress = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Form_Submits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Form_Submits_Forms_FormId",
                        column: x => x.FormId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Forms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Blog_Categories",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentCategoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    SeoTitle = table.Column<string>(type: "TEXT", nullable: false),
                    SeoLink = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Content = table.Column<string>(type: "TEXT", nullable: true),
                    ImageUri = table.Column<string>(type: "TEXT", nullable: true),
                    LanguageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsUnique = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDisplayPage = table.Column<bool>(type: "INTEGER", nullable: false),
                    PageId = table.Column<string>(type: "TEXT", nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    PosterHeaderUri = table.Column<string>(type: "TEXT", nullable: true),
                    PosterUri = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blog_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Blog_Categories_Blog_Categories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Blog_Categories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Blog_Categories_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pages",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, defaultValue: ""),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, defaultValue: ""),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Keywords = table.Column<string>(type: "TEXT", nullable: true),
                    LanguageCode = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false, defaultValue: "en"),
                    IsHomePage = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CustomCSS = table.Column<string>(type: "TEXT", nullable: true),
                    CustomJS = table.Column<string>(type: "TEXT", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LanguageId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OgImage = table.Column<string>(type: "TEXT", nullable: true),
                    OgTitle = table.Column<string>(type: "TEXT", nullable: true),
                    OgDescription = table.Column<string>(type: "TEXT", nullable: true),
                    FloatingContactJson = table.Column<string>(type: "TEXT", nullable: true),
                    BackgroundColor = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pages_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Languages",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Sliders",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    LanguageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsHome = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsBlog = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sliders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sliders_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    SeoTitle = table.Column<string>(type: "TEXT", nullable: false),
                    SeoLink = table.Column<string>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    LanguageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tags_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExchangeRateHistories",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OfficeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SourceCurrencyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TargetCurrencyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OldBuyRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    OldSellRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: true),
                    NewBuyRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    NewSellRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    ChangePercent = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    UpdateSource = table.Column<string>(type: "TEXT", nullable: true),
                    DataSources = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    IsApproved = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRateHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExchangeRateHistories_Currencies_SourceCurrencyId",
                        column: x => x.SourceCurrencyId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Currencies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExchangeRateHistories_Currencies_TargetCurrencyId",
                        column: x => x.TargetCurrencyId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Currencies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExchangeRateHistories_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Offices",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExchangeRates",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OfficeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SourceCurrencyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TargetCurrencyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BuyRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    SellRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExchangeRates_Currencies_SourceCurrencyId",
                        column: x => x.SourceCurrencyId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExchangeRates_Currencies_TargetCurrencyId",
                        column: x => x.TargetCurrencyId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExchangeRates_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Offices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseDefinitions",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OfficeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsRecurring = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecurrencePeriod = table.Column<int>(type: "INTEGER", nullable: true),
                    DefaultAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    DefaultCurrencyId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseDefinitions_Currencies_DefaultCurrencyId",
                        column: x => x.DefaultCurrencyId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Currencies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExpenseDefinitions_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Offices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PendingRateApprovals",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OfficeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SourceCurrencyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TargetCurrencyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurrentBuyRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    CurrentSellRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    ProposedBuyRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    ProposedSellRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    ChangePercent = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    SourceData = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ApprovalNotes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingRateApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingRateApprovals_Currencies_SourceCurrencyId",
                        column: x => x.SourceCurrencyId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Currencies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PendingRateApprovals_Currencies_TargetCurrencyId",
                        column: x => x.TargetCurrencyId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Currencies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PendingRateApprovals_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Offices",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: false),
                    Mail = table.Column<string>(type: "TEXT", nullable: false),
                    IsEmailVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    Firstname = table.Column<string>(type: "TEXT", nullable: false),
                    Lastname = table.Column<string>(type: "TEXT", nullable: false),
                    Gender = table.Column<int>(type: "INTEGER", nullable: false),
                    Rank = table.Column<int>(type: "INTEGER", nullable: false),
                    LanguageCode = table.Column<string>(type: "TEXT", nullable: true),
                    FirstIp = table.Column<string>(type: "TEXT", nullable: true),
                    LastIp = table.Column<string>(type: "TEXT", nullable: true),
                    LastPasswordChangeDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    OfficeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Offices",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "VaultBalanceSnapshots",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OfficeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultBalanceSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VaultBalanceSnapshots_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Offices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vaults",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    OfficeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    ClosingBalance = table.Column<decimal>(type: "TEXT", nullable: false),
                    ClosedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ShouldCount = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastCountDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vaults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vaults_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Offices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Menus",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    LanguageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomUrl = table.Column<string>(type: "TEXT", nullable: true),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsOnNavbar = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsOnAside = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsOnFooter = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsForMobile = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsCategoryMenu = table.Column<bool>(type: "INTEGER", nullable: false),
                    Blog_CategoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ParentMenuId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Menus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Menus_Blog_Categories_Blog_CategoryId",
                        column: x => x.Blog_CategoryId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Blog_Categories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Menus_Languages_LanguageId",
                        column: x => x.LanguageId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Languages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Menus_Menus_ParentMenuId",
                        column: x => x.ParentMenuId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Menus",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Analytics",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PageId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PageSlug = table.Column<string>(type: "TEXT", nullable: false),
                    SessionId = table.Column<string>(type: "TEXT", nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", nullable: false),
                    UserAgent = table.Column<string>(type: "TEXT", nullable: false),
                    Referrer = table.Column<string>(type: "TEXT", nullable: false),
                    Device = table.Column<string>(type: "TEXT", nullable: false),
                    Browser = table.Column<string>(type: "TEXT", nullable: false),
                    Country = table.Column<string>(type: "TEXT", nullable: false),
                    City = table.Column<string>(type: "TEXT", nullable: false),
                    VisitDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Duration = table.Column<int>(type: "INTEGER", nullable: false),
                    IsBounce = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Analytics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Analytics_Pages_PageId",
                        column: x => x.PageId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Pages",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BuilderComponents",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    PropsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DesktopStyles = table.Column<string>(type: "TEXT", nullable: true),
                    TabletStyles = table.Column<string>(type: "TEXT", nullable: true),
                    MobileStyles = table.Column<string>(type: "TEXT", nullable: true),
                    CustomCSS = table.Column<string>(type: "TEXT", nullable: true),
                    Locked = table.Column<bool>(type: "INTEGER", nullable: false),
                    Hidden = table.Column<bool>(type: "INTEGER", nullable: false),
                    ParentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PageId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuilderComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuilderComponents_BuilderComponents_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "BuilderComponents",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BuilderComponents_Pages_PageId",
                        column: x => x.PageId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeoSettings",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PageId = table.Column<Guid>(type: "TEXT", nullable: true),
                    MetaTitle = table.Column<string>(type: "TEXT", nullable: false),
                    MetaDescription = table.Column<string>(type: "TEXT", nullable: false),
                    MetaKeywords = table.Column<string>(type: "TEXT", nullable: false),
                    MetaAuthor = table.Column<string>(type: "TEXT", nullable: false),
                    OgTitle = table.Column<string>(type: "TEXT", nullable: false),
                    OgDescription = table.Column<string>(type: "TEXT", nullable: false),
                    OgImage = table.Column<string>(type: "TEXT", nullable: false),
                    OgType = table.Column<string>(type: "TEXT", nullable: false),
                    TwitterCard = table.Column<string>(type: "TEXT", nullable: false),
                    TwitterSite = table.Column<string>(type: "TEXT", nullable: false),
                    TwitterCreator = table.Column<string>(type: "TEXT", nullable: false),
                    SchemaMarkup = table.Column<string>(type: "TEXT", nullable: false),
                    CanonicalUrl = table.Column<string>(type: "TEXT", nullable: false),
                    NoIndex = table.Column<bool>(type: "INTEGER", nullable: false),
                    NoFollow = table.Column<bool>(type: "INTEGER", nullable: false),
                    IncludeInSitemap = table.Column<bool>(type: "INTEGER", nullable: false),
                    SitemapPriority = table.Column<string>(type: "TEXT", nullable: false),
                    SitemapChangeFrequency = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeoSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeoSettings_Pages_PageId",
                        column: x => x.PageId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Pages",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Slider_Items",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Description2 = table.Column<string>(type: "TEXT", nullable: true),
                    CallText = table.Column<string>(type: "TEXT", nullable: true),
                    ImageUrl = table.Column<string>(type: "TEXT", nullable: true),
                    VideoUrl = table.Column<string>(type: "TEXT", nullable: true),
                    SliderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Slider_Items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Slider_Items_Sliders_SliderId",
                        column: x => x.SliderId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Sliders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Blog_Category_Tags",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TagId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blog_Category_Tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Blog_Category_Tags_Blog_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Blog_Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Blog_Category_Tags_Tags_TagId",
                        column: x => x.TagId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Blog_Articles",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    SeoTitle = table.Column<string>(type: "TEXT", nullable: false),
                    SeoLink = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    AuthorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsUnique = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsOnSlider = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsAnnouncement = table.Column<bool>(type: "INTEGER", nullable: false),
                    Views = table.Column<int>(type: "INTEGER", nullable: false),
                    PosterHeaderUri = table.Column<string>(type: "TEXT", nullable: true),
                    PosterUri = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blog_Articles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Blog_Articles_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Coin_Profits",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CoinId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PairId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 38, scale: 18, nullable: false),
                    EffQuantity = table.Column<decimal>(type: "TEXT", precision: 38, scale: 18, nullable: false),
                    BuyPrice = table.Column<decimal>(type: "TEXT", precision: 38, scale: 18, nullable: false),
                    SellPrice = table.Column<decimal>(type: "TEXT", precision: 38, scale: 18, nullable: true),
                    FeeRate = table.Column<decimal>(type: "TEXT", precision: 38, scale: 18, nullable: false),
                    Profit = table.Column<decimal>(type: "TEXT", precision: 38, scale: 18, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coin_Profits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Coin_Profits_Coin_Pairs_PairId",
                        column: x => x.PairId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Coin_Pairs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Coin_Profits_Coins_CoinId",
                        column: x => x.CoinId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Coins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Coin_Profits_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Coin_User_Favorites",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CoinId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PairName = table.Column<string>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coin_User_Favorites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Coin_User_Favorites_Coins_CoinId",
                        column: x => x.CoinId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Coins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Coin_User_Favorites_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Coin_User_Tables",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coin_User_Tables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Coin_User_Tables_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Logs",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UserDescription = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    LogType = table.Column<int>(type: "INTEGER", nullable: false),
                    ActionType = table.Column<int>(type: "INTEGER", nullable: false),
                    ContentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Logs_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Parties",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OfficeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PartyCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ContactPerson = table.Column<string>(type: "TEXT", nullable: true),
                    TaxNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    RegistrationNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    City = table.Column<string>(type: "TEXT", nullable: true),
                    Country = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasCreditLimit = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultPaymentTermDays = table.Column<int>(type: "INTEGER", nullable: false),
                    LastTransactionDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TotalVolume = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Parties_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Offices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Parties_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Parties_Users_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "User_Offices",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OfficeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User_Offices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_User_Offices_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Offices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_User_Offices_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CurrencySales",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VaultId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceCurrencyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TargetCurrencyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SaleRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    OriginalSaleRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    Revenue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    RevenuePercent = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencySales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurrencySales_Currencies_SourceCurrencyId",
                        column: x => x.SourceCurrencyId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CurrencySales_Currencies_TargetCurrencyId",
                        column: x => x.TargetCurrencyId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurrencySales_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CurrencySales_Vaults_VaultId",
                        column: x => x.VaultId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Vaults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DailySummaries",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OfficeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VaultId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SummaryDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    TotalIn = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    TotalOut = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    ClosingBalance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    ProfitLoss = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    TransactionCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailySummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailySummaries_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DailySummaries_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Offices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DailySummaries_Vaults_VaultId",
                        column: x => x.VaultId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Vaults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExpensePayments",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExpenseDefinitionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VaultId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PaymentNumber = table.Column<string>(type: "TEXT", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false),
                    PaymentMethod = table.Column<int>(type: "INTEGER", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Receipt = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedReason = table.Column<string>(type: "TEXT", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpensePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpensePayments_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExpensePayments_ExpenseDefinitions_ExpenseDefinitionId",
                        column: x => x.ExpenseDefinitionId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "ExpenseDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExpensePayments_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExpensePayments_Users_DeletedByUserId",
                        column: x => x.DeletedByUserId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExpensePayments_Vaults_VaultId",
                        column: x => x.VaultId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Vaults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultBalanceHistories",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VaultId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Balance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    TransactionType = table.Column<int>(type: "INTEGER", nullable: false),
                    IsGhost = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsParty = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultBalanceHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VaultBalanceHistories_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VaultBalanceHistories_Vaults_VaultId",
                        column: x => x.VaultId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Vaults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultBalances",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VaultId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Balance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    ReservedAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultBalances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VaultBalances_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VaultBalances_Vaults_VaultId",
                        column: x => x.VaultId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Vaults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VaultBalanceSnapshotDetails",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VaultId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Balance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    ReservedAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultBalanceSnapshotDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VaultBalanceSnapshotDetails_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VaultBalanceSnapshotDetails_VaultBalanceSnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "VaultBalanceSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VaultBalanceSnapshotDetails_Vaults_VaultId",
                        column: x => x.VaultId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Vaults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VaultCounts",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VaultId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OfficeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CountDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HasDiscrepancy = table.Column<bool>(type: "INTEGER", nullable: false),
                    DiscrepancyDetails = table.Column<string>(type: "TEXT", nullable: false),
                    IsSystemGenerated = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultCounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VaultCounts_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Offices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VaultCounts_Vaults_VaultId",
                        column: x => x.VaultId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Vaults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Menu_Items",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    SeoTitle = table.Column<string>(type: "TEXT", nullable: false),
                    SeoLink = table.Column<string>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    MenuId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsSingleCategoryItem = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsMainCategoryItem = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsCustomLink = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsPage = table.Column<bool>(type: "INTEGER", nullable: false),
                    MainCategoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Blog_CategoryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Menu_Items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Menu_Items_Blog_Categories_Blog_CategoryId",
                        column: x => x.Blog_CategoryId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Blog_Categories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Menu_Items_Blog_Categories_MainCategoryId",
                        column: x => x.MainCategoryId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Blog_Categories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Menu_Items_Menus_MenuId",
                        column: x => x.MenuId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Menus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Blog_Article_Categories",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    ArticleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blog_Article_Categories", x => new { x.ArticleId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_Blog_Article_Categories_Blog_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Blog_Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Blog_Article_Categories_Blog_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Blog_Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Blog_Article_Comments",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    LastName = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    IpAdress = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ArticleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blog_Article_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Blog_Article_Comments_Blog_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Blog_Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Blog_Article_Comments_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Blog_Article_Tags",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TagId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ArticleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blog_Article_Tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Blog_Article_Tags_Blog_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Blog_Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Blog_Article_Tags_Tags_TagId",
                        column: x => x.TagId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Blog_Article_Visits",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VisitorIp = table.Column<string>(type: "TEXT", nullable: false),
                    ArticleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blog_Article_Visits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Blog_Article_Visits_Blog_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Blog_Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Coin_Users",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Coin_User_TableId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CoinId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PairId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 38, scale: 18, nullable: false),
                    EffQuantity = table.Column<decimal>(type: "TEXT", precision: 38, scale: 18, nullable: false),
                    BuyPrice = table.Column<decimal>(type: "TEXT", precision: 38, scale: 18, nullable: false),
                    SellPrice = table.Column<decimal>(type: "TEXT", precision: 38, scale: 18, nullable: true),
                    FeeRate = table.Column<decimal>(type: "TEXT", precision: 38, scale: 18, nullable: false),
                    Profit = table.Column<decimal>(type: "TEXT", precision: 38, scale: 18, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coin_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Coin_Users_Coin_Pairs_PairId",
                        column: x => x.PairId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Coin_Pairs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Coin_Users_Coin_User_Tables_Coin_User_TableId",
                        column: x => x.Coin_User_TableId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Coin_User_Tables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Coin_Users_Coins_CoinId",
                        column: x => x.CoinId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Coins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Coin_Users_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GhostPartyAccounts",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PartyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OfficeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    BlockedAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalDebits = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalCredits = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TransactionCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastTransactionDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GhostPartyAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GhostPartyAccounts_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GhostPartyAccounts_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Offices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GhostPartyAccounts_Parties_PartyId",
                        column: x => x.PartyId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PartyAccounts",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PartyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Balance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    BlockedAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    CreditLimit = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    PaymentTermDays = table.Column<int>(type: "INTEGER", nullable: false),
                    LastActivityDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TotalDebits = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    TotalCredits = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    TransactionCount = table.Column<int>(type: "INTEGER", nullable: false),
                    LastTransactionDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartyAccounts_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartyAccounts_Parties_PartyId",
                        column: x => x.PartyId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartyContacts",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PartyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContactName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Position = table.Column<string>(type: "TEXT", nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Mobile = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsPrimary = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartyContacts_Parties_PartyId",
                        column: x => x.PartyId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartyCreditLimits",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PartyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreditLimit = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    UtilizedAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    TemporaryLimit = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    TemporaryLimitExpiry = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PaymentTermDays = table.Column<int>(type: "INTEGER", nullable: false),
                    InterestRate = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastReviewDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyCreditLimits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartyCreditLimits_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartyCreditLimits_Parties_PartyId",
                        column: x => x.PartyId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartyCreditLimits_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PartyStatements",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PartyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StatementNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    StatementDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    ClosingBalance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    TotalDebits = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    TotalCredits = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    CurrentAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    Amount30Days = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    Amount60Days = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    Amount90Days = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    AmountOver90Days = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SentDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SentTo = table.Column<string>(type: "TEXT", nullable: false),
                    GeneratedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyStatements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartyStatements_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartyStatements_Parties_PartyId",
                        column: x => x.PartyId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartyStatements_Users_GeneratedByUserId",
                        column: x => x.GeneratedByUserId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TransactionNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    VaultId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustomerId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PartyId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    Profit = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    IsCustomRate = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedReason = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    deletedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_Parties_PartyId",
                        column: x => x.PartyId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transactions_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Transactions_Vaults_VaultId",
                        column: x => x.VaultId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Vaults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VaultCountDetails",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VaultCountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActualAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    SystemAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    Discrepancy = table.Column<decimal>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaultCountDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VaultCountDetails_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VaultCountDetails_VaultCounts_VaultCountId",
                        column: x => x.VaultCountId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "VaultCounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GhostPartyAccountEntries",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    GhostAccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OfficeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntryType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    RunningBalance = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TransactionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    VaultId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PaymentMethod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsReconciled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReconciledDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReconciledBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "TEXT", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GhostPartyAccountEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GhostPartyAccountEntries_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GhostPartyAccountEntries_GhostPartyAccounts_GhostAccountId",
                        column: x => x.GhostAccountId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "GhostPartyAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GhostPartyAccountEntries_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Offices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GhostPartyAccountEntries_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GhostPartyAccountEntries_Vaults_VaultId",
                        column: x => x.VaultId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Vaults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PartyAccountEntries",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PartyAccountId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TransactionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    EntryNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    EntryDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    RunningBalance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    PaymentStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PaymentReference = table.Column<string>(type: "TEXT", nullable: false),
                    IsReconciled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReconciledDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ReconciledByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PaymentLinkId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IsReversed = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReversalEntryId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OriginalCurrencyId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OriginalAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    ExchangeRate = table.Column<decimal>(type: "TEXT", nullable: true),
                    IsCustomRate = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyAccountEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartyAccountEntries_Currencies_OriginalCurrencyId",
                        column: x => x.OriginalCurrencyId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Currencies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PartyAccountEntries_PartyAccounts_PartyAccountId",
                        column: x => x.PartyAccountId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "PartyAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartyAccountEntries_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PartyAccountEntries_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartyAccountEntries_Users_ReconciledByUserId",
                        column: x => x.ReconciledByUserId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransactionDetails",
                schema: "mtturkey_exchange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TransactionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Side = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    Rate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    Commission = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    NetAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    ActualBuyRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: true),
                    ActualSellRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: true),
                    CustomRate = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionDetails_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransactionDetails_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalSchema: "mtturkey_exchange",
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Analytics_PageId",
                schema: "mtturkey_exchange",
                table: "Analytics",
                column: "PageId");

            migrationBuilder.CreateIndex(
                name: "IX_Blog_Article_Categories_CategoryId",
                schema: "mtturkey_exchange",
                table: "Blog_Article_Categories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Blog_Article_Comments_ArticleId",
                schema: "mtturkey_exchange",
                table: "Blog_Article_Comments",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_Blog_Article_Comments_UserId",
                schema: "mtturkey_exchange",
                table: "Blog_Article_Comments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Blog_Article_Tags_ArticleId",
                schema: "mtturkey_exchange",
                table: "Blog_Article_Tags",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_Blog_Article_Tags_TagId",
                schema: "mtturkey_exchange",
                table: "Blog_Article_Tags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_Blog_Article_Visits_ArticleId",
                schema: "mtturkey_exchange",
                table: "Blog_Article_Visits",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_Blog_Articles_AuthorId",
                schema: "mtturkey_exchange",
                table: "Blog_Articles",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Blog_Categories_LanguageId",
                schema: "mtturkey_exchange",
                table: "Blog_Categories",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_Blog_Categories_ParentCategoryId",
                schema: "mtturkey_exchange",
                table: "Blog_Categories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Blog_Category_Tags_CategoryId",
                schema: "mtturkey_exchange",
                table: "Blog_Category_Tags",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Blog_Category_Tags_TagId",
                schema: "mtturkey_exchange",
                table: "Blog_Category_Tags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_BuilderComponents_PageId",
                schema: "mtturkey_exchange",
                table: "BuilderComponents",
                column: "PageId");

            migrationBuilder.CreateIndex(
                name: "IX_BuilderComponents_ParentId",
                schema: "mtturkey_exchange",
                table: "BuilderComponents",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Coin_Pairs_CoinId",
                schema: "mtturkey_exchange",
                table: "Coin_Pairs",
                column: "CoinId");

            migrationBuilder.CreateIndex(
                name: "IX_Coin_Profits_CoinId",
                schema: "mtturkey_exchange",
                table: "Coin_Profits",
                column: "CoinId");

            migrationBuilder.CreateIndex(
                name: "IX_Coin_Profits_PairId",
                schema: "mtturkey_exchange",
                table: "Coin_Profits",
                column: "PairId");

            migrationBuilder.CreateIndex(
                name: "IX_Coin_Profits_UserId",
                schema: "mtturkey_exchange",
                table: "Coin_Profits",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Coin_User_Favorites_CoinId",
                schema: "mtturkey_exchange",
                table: "Coin_User_Favorites",
                column: "CoinId");

            migrationBuilder.CreateIndex(
                name: "IX_Coin_User_Favorites_UserId",
                schema: "mtturkey_exchange",
                table: "Coin_User_Favorites",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Coin_User_Tables_UserId",
                schema: "mtturkey_exchange",
                table: "Coin_User_Tables",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Coin_Users_Coin_User_TableId",
                schema: "mtturkey_exchange",
                table: "Coin_Users",
                column: "Coin_User_TableId");

            migrationBuilder.CreateIndex(
                name: "IX_Coin_Users_CoinId",
                schema: "mtturkey_exchange",
                table: "Coin_Users",
                column: "CoinId");

            migrationBuilder.CreateIndex(
                name: "IX_Coin_Users_PairId",
                schema: "mtturkey_exchange",
                table: "Coin_Users",
                column: "PairId");

            migrationBuilder.CreateIndex(
                name: "IX_Coin_Users_UserId",
                schema: "mtturkey_exchange",
                table: "Coin_Users",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_CurrencyCode",
                schema: "mtturkey_exchange",
                table: "Currencies",
                column: "CurrencyCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurrencySales_SourceCurrencyId",
                schema: "mtturkey_exchange",
                table: "CurrencySales",
                column: "SourceCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencySales_TargetCurrencyId",
                schema: "mtturkey_exchange",
                table: "CurrencySales",
                column: "TargetCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencySales_UserId",
                schema: "mtturkey_exchange",
                table: "CurrencySales",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencySales_VaultId",
                schema: "mtturkey_exchange",
                table: "CurrencySales",
                column: "VaultId");

            migrationBuilder.CreateIndex(
                name: "IX_DailySummaries_CurrencyId",
                schema: "mtturkey_exchange",
                table: "DailySummaries",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_DailySummaries_OfficeId",
                schema: "mtturkey_exchange",
                table: "DailySummaries",
                column: "OfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_DailySummaries_VaultId_CurrencyId_SummaryDate",
                schema: "mtturkey_exchange",
                table: "DailySummaries",
                columns: new[] { "VaultId", "CurrencyId", "SummaryDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRateHistories_OfficeId",
                schema: "mtturkey_exchange",
                table: "ExchangeRateHistories",
                column: "OfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRateHistories_SourceCurrencyId",
                schema: "mtturkey_exchange",
                table: "ExchangeRateHistories",
                column: "SourceCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRateHistories_TargetCurrencyId",
                schema: "mtturkey_exchange",
                table: "ExchangeRateHistories",
                column: "TargetCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_OfficeId_SourceCurrencyId_TargetCurrencyId_EffectiveFrom",
                schema: "mtturkey_exchange",
                table: "ExchangeRates",
                columns: new[] { "OfficeId", "SourceCurrencyId", "TargetCurrencyId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_SourceCurrencyId",
                schema: "mtturkey_exchange",
                table: "ExchangeRates",
                column: "SourceCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_TargetCurrencyId",
                schema: "mtturkey_exchange",
                table: "ExchangeRates",
                column: "TargetCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseDefinitions_DefaultCurrencyId",
                schema: "mtturkey_exchange",
                table: "ExpenseDefinitions",
                column: "DefaultCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseDefinitions_OfficeId",
                schema: "mtturkey_exchange",
                table: "ExpenseDefinitions",
                column: "OfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpensePayments_CreatedByUserId",
                schema: "mtturkey_exchange",
                table: "ExpensePayments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpensePayments_CurrencyId",
                schema: "mtturkey_exchange",
                table: "ExpensePayments",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpensePayments_DeletedByUserId",
                schema: "mtturkey_exchange",
                table: "ExpensePayments",
                column: "DeletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpensePayments_ExpenseDefinitionId",
                schema: "mtturkey_exchange",
                table: "ExpensePayments",
                column: "ExpenseDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpensePayments_VaultId",
                schema: "mtturkey_exchange",
                table: "ExpensePayments",
                column: "VaultId");

            migrationBuilder.CreateIndex(
                name: "IX_Form_Submits_FormId",
                schema: "mtturkey_exchange",
                table: "Form_Submits",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_GhostPartyAccountEntries_CurrencyId",
                schema: "mtturkey_exchange",
                table: "GhostPartyAccountEntries",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_GhostPartyAccountEntries_GhostAccountId",
                schema: "mtturkey_exchange",
                table: "GhostPartyAccountEntries",
                column: "GhostAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_GhostPartyAccountEntries_OfficeId",
                schema: "mtturkey_exchange",
                table: "GhostPartyAccountEntries",
                column: "OfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_GhostPartyAccountEntries_TransactionId",
                schema: "mtturkey_exchange",
                table: "GhostPartyAccountEntries",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_GhostPartyAccountEntries_VaultId",
                schema: "mtturkey_exchange",
                table: "GhostPartyAccountEntries",
                column: "VaultId");

            migrationBuilder.CreateIndex(
                name: "IX_GhostPartyAccounts_CurrencyId",
                schema: "mtturkey_exchange",
                table: "GhostPartyAccounts",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_GhostPartyAccounts_OfficeId",
                schema: "mtturkey_exchange",
                table: "GhostPartyAccounts",
                column: "OfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_GhostPartyAccounts_PartyId",
                schema: "mtturkey_exchange",
                table: "GhostPartyAccounts",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_Logs_UserId",
                schema: "mtturkey_exchange",
                table: "Logs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Menu_Items_Blog_CategoryId",
                schema: "mtturkey_exchange",
                table: "Menu_Items",
                column: "Blog_CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Menu_Items_MainCategoryId",
                schema: "mtturkey_exchange",
                table: "Menu_Items",
                column: "MainCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Menu_Items_MenuId",
                schema: "mtturkey_exchange",
                table: "Menu_Items",
                column: "MenuId");

            migrationBuilder.CreateIndex(
                name: "IX_Menus_Blog_CategoryId",
                schema: "mtturkey_exchange",
                table: "Menus",
                column: "Blog_CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Menus_LanguageId",
                schema: "mtturkey_exchange",
                table: "Menus",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_Menus_ParentMenuId",
                schema: "mtturkey_exchange",
                table: "Menus",
                column: "ParentMenuId");

            migrationBuilder.CreateIndex(
                name: "IX_Offices_OfficeName",
                schema: "mtturkey_exchange",
                table: "Offices",
                column: "OfficeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pages_LanguageId",
                schema: "mtturkey_exchange",
                table: "Pages",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_Pages_Slug_LanguageCode",
                schema: "mtturkey_exchange",
                table: "Pages",
                columns: new[] { "Slug", "LanguageCode" },
                unique: true,
                filter: "[Slug] != '' AND [LanguageCode] != ''");

            migrationBuilder.CreateIndex(
                name: "IX_Parties_CreatedByUserId",
                schema: "mtturkey_exchange",
                table: "Parties",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Parties_Email",
                schema: "mtturkey_exchange",
                table: "Parties",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Parties_ModifiedByUserId",
                schema: "mtturkey_exchange",
                table: "Parties",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Parties_OfficeId_PartyCode",
                schema: "mtturkey_exchange",
                table: "Parties",
                columns: new[] { "OfficeId", "PartyCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Parties_TaxNumber",
                schema: "mtturkey_exchange",
                table: "Parties",
                column: "TaxNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PartyAccountEntries_CreatedByUserId",
                schema: "mtturkey_exchange",
                table: "PartyAccountEntries",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyAccountEntries_DueDate",
                schema: "mtturkey_exchange",
                table: "PartyAccountEntries",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_PartyAccountEntries_EntryNumber",
                schema: "mtturkey_exchange",
                table: "PartyAccountEntries",
                column: "EntryNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartyAccountEntries_OriginalCurrencyId",
                schema: "mtturkey_exchange",
                table: "PartyAccountEntries",
                column: "OriginalCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyAccountEntries_PartyAccountId_EntryDate",
                schema: "mtturkey_exchange",
                table: "PartyAccountEntries",
                columns: new[] { "PartyAccountId", "EntryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PartyAccountEntries_PaymentLinkId",
                schema: "mtturkey_exchange",
                table: "PartyAccountEntries",
                column: "PaymentLinkId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyAccountEntries_PaymentStatus",
                schema: "mtturkey_exchange",
                table: "PartyAccountEntries",
                column: "PaymentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_PartyAccountEntries_ReconciledByUserId",
                schema: "mtturkey_exchange",
                table: "PartyAccountEntries",
                column: "ReconciledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyAccountEntries_ReversalEntryId",
                schema: "mtturkey_exchange",
                table: "PartyAccountEntries",
                column: "ReversalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyAccountEntries_TransactionId",
                schema: "mtturkey_exchange",
                table: "PartyAccountEntries",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyAccounts_AccountNumber",
                schema: "mtturkey_exchange",
                table: "PartyAccounts",
                column: "AccountNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartyAccounts_CurrencyId",
                schema: "mtturkey_exchange",
                table: "PartyAccounts",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyAccounts_PartyId_CurrencyId",
                schema: "mtturkey_exchange",
                table: "PartyAccounts",
                columns: new[] { "PartyId", "CurrencyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartyContacts_PartyId_Email",
                schema: "mtturkey_exchange",
                table: "PartyContacts",
                columns: new[] { "PartyId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_PartyCreditLimits_ApprovedByUserId",
                schema: "mtturkey_exchange",
                table: "PartyCreditLimits",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyCreditLimits_CurrencyId",
                schema: "mtturkey_exchange",
                table: "PartyCreditLimits",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyCreditLimits_PartyId_CurrencyId_EffectiveFrom",
                schema: "mtturkey_exchange",
                table: "PartyCreditLimits",
                columns: new[] { "PartyId", "CurrencyId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_PartyStatements_CurrencyId",
                schema: "mtturkey_exchange",
                table: "PartyStatements",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyStatements_GeneratedByUserId",
                schema: "mtturkey_exchange",
                table: "PartyStatements",
                column: "GeneratedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyStatements_PartyId_CurrencyId_StatementDate",
                schema: "mtturkey_exchange",
                table: "PartyStatements",
                columns: new[] { "PartyId", "CurrencyId", "StatementDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PartyStatements_StatementNumber",
                schema: "mtturkey_exchange",
                table: "PartyStatements",
                column: "StatementNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PendingRateApprovals_OfficeId",
                schema: "mtturkey_exchange",
                table: "PendingRateApprovals",
                column: "OfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingRateApprovals_SourceCurrencyId",
                schema: "mtturkey_exchange",
                table: "PendingRateApprovals",
                column: "SourceCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingRateApprovals_TargetCurrencyId",
                schema: "mtturkey_exchange",
                table: "PendingRateApprovals",
                column: "TargetCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_SeoSettings_PageId",
                schema: "mtturkey_exchange",
                table: "SeoSettings",
                column: "PageId");

            migrationBuilder.CreateIndex(
                name: "IX_Slider_Items_SliderId",
                schema: "mtturkey_exchange",
                table: "Slider_Items",
                column: "SliderId");

            migrationBuilder.CreateIndex(
                name: "IX_Sliders_LanguageId",
                schema: "mtturkey_exchange",
                table: "Sliders",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_LanguageId",
                schema: "mtturkey_exchange",
                table: "Tags",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionDetails_CurrencyId",
                schema: "mtturkey_exchange",
                table: "TransactionDetails",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionDetails_TransactionId",
                schema: "mtturkey_exchange",
                table: "TransactionDetails",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_PartyId",
                schema: "mtturkey_exchange",
                table: "Transactions",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TransactionDate",
                schema: "mtturkey_exchange",
                table: "Transactions",
                column: "TransactionDate");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TransactionNumber",
                schema: "mtturkey_exchange",
                table: "Transactions",
                column: "TransactionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserId",
                schema: "mtturkey_exchange",
                table: "Transactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_VaultId",
                schema: "mtturkey_exchange",
                table: "Transactions",
                column: "VaultId");

            migrationBuilder.CreateIndex(
                name: "IX_User_Offices_OfficeId",
                schema: "mtturkey_exchange",
                table: "User_Offices",
                column: "OfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_User_Offices_UserId",
                schema: "mtturkey_exchange",
                table: "User_Offices",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_OfficeId",
                schema: "mtturkey_exchange",
                table: "Users",
                column: "OfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_VaultBalanceHistories_CurrencyId",
                schema: "mtturkey_exchange",
                table: "VaultBalanceHistories",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_VaultBalanceHistories_VaultId_CurrencyId",
                schema: "mtturkey_exchange",
                table: "VaultBalanceHistories",
                columns: new[] { "VaultId", "CurrencyId" });

            migrationBuilder.CreateIndex(
                name: "IX_VaultBalances_CurrencyId",
                schema: "mtturkey_exchange",
                table: "VaultBalances",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_VaultBalances_VaultId_CurrencyId",
                schema: "mtturkey_exchange",
                table: "VaultBalances",
                columns: new[] { "VaultId", "CurrencyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaultBalanceSnapshotDetails_CurrencyId",
                schema: "mtturkey_exchange",
                table: "VaultBalanceSnapshotDetails",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_VaultBalanceSnapshotDetails_SnapshotId_VaultId_CurrencyId",
                schema: "mtturkey_exchange",
                table: "VaultBalanceSnapshotDetails",
                columns: new[] { "SnapshotId", "VaultId", "CurrencyId" });

            migrationBuilder.CreateIndex(
                name: "IX_VaultBalanceSnapshotDetails_VaultId",
                schema: "mtturkey_exchange",
                table: "VaultBalanceSnapshotDetails",
                column: "VaultId");

            migrationBuilder.CreateIndex(
                name: "IX_VaultBalanceSnapshots_OfficeId_SnapshotDate",
                schema: "mtturkey_exchange",
                table: "VaultBalanceSnapshots",
                columns: new[] { "OfficeId", "SnapshotDate" });

            migrationBuilder.CreateIndex(
                name: "IX_VaultBalanceSnapshots_UserId",
                schema: "mtturkey_exchange",
                table: "VaultBalanceSnapshots",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VaultCountDetails_CurrencyId",
                schema: "mtturkey_exchange",
                table: "VaultCountDetails",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_VaultCountDetails_VaultCountId",
                schema: "mtturkey_exchange",
                table: "VaultCountDetails",
                column: "VaultCountId");

            migrationBuilder.CreateIndex(
                name: "IX_VaultCounts_OfficeId",
                schema: "mtturkey_exchange",
                table: "VaultCounts",
                column: "OfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_VaultCounts_VaultId",
                schema: "mtturkey_exchange",
                table: "VaultCounts",
                column: "VaultId");

            migrationBuilder.CreateIndex(
                name: "IX_Vaults_OfficeId_Name",
                schema: "mtturkey_exchange",
                table: "Vaults",
                columns: new[] { "OfficeId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Analytics",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "BlackList",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "Blog_Article_Categories",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "Blog_Article_Comments",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "Blog_Article_Tags",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "Blog_Article_Visits",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "Blog_Category_Tags",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "BuilderComponents",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "Coin_Profits",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "Coin_User_Favorites",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "Coin_Users",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "CurrencySales",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "DailySummaries",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "ExchangeRateHistories",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "ExchangeRates",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "ExchangeSettings",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "ExpensePayments",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "ExternalDataSources",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "ExternalRateCaches",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "Form_Submits",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "GhostPartyAccountEntries",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "GlobalColors",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "Logs",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "MediaFiles",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "Menu_Items",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "PartyAccountEntries",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "PartyContacts",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "PartyCreditLimits",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "PartyStatements",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "PendingRateApprovals",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "SeoSettings",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "Site_Settings",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "Slider_Items",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "Themes",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "TransactionDetails",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "User_Offices",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "VaultBalanceHistories",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "VaultBalances",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "VaultBalanceSnapshotDetails",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "VaultCountDetails",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "Blog_Articles",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "Tags",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "Coin_Pairs",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "Coin_User_Tables",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "ExpenseDefinitions",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "Forms",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "GhostPartyAccounts",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "Menus",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "PartyAccounts",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "Pages",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "Sliders",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "Transactions",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "VaultBalanceSnapshots",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "VaultCounts",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "Coins",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "Blog_Categories",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "Currencies",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "Parties",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "Vaults",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "Languages",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "mtturkey_exchange");

            migrationBuilder.DropTable(
                name: "Offices",
                schema: "mtturkey_exchange");
        }
    }
}
