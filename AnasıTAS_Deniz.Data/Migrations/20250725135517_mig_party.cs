using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnasıTAS_Deniz.Data.Migrations
{
    /// <inheritdoc />
    public partial class mig_party : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PartyId",
                table: "Transactions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Parties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfficeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartyCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TaxNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RegistrationNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    HasCreditLimit = table.Column<bool>(type: "bit", nullable: false),
                    DefaultPaymentTermDays = table.Column<int>(type: "int", nullable: false),
                    LastTransactionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalVolume = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Parties_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalTable: "Offices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Parties_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Parties_Users_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PartyAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    BlockedAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CreditLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentTermDays = table.Column<int>(type: "int", nullable: false),
                    LastActivityDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalDebits = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalCredits = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TransactionCount = table.Column<int>(type: "int", nullable: false),
                    LastTransactionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartyAccounts_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartyAccounts_Parties_PartyId",
                        column: x => x.PartyId,
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartyContacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Position = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Mobile = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PartyId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartyContacts_Parties_PartyId",
                        column: x => x.PartyId,
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartyContacts_Parties_PartyId1",
                        column: x => x.PartyId1,
                        principalTable: "Parties",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PartyCreditLimits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreditLimit = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    UtilizedAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TemporaryLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TemporaryLimitExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentTermDays = table.Column<int>(type: "int", nullable: false),
                    InterestRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastReviewDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyCreditLimits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartyCreditLimits_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartyCreditLimits_Parties_PartyId",
                        column: x => x.PartyId,
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartyCreditLimits_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PartyStatements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StatementNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StatementDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ClosingBalance = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalDebits = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TotalCredits = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    CurrentAmount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Amount30Days = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Amount60Days = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Amount90Days = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    AmountOver90Days = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentTo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyStatements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartyStatements_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartyStatements_Parties_PartyId",
                        column: x => x.PartyId,
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartyStatements_Users_GeneratedByUserId",
                        column: x => x.GeneratedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PartyAccountEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartyAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EntryNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    RunningBalance = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PaymentStatus = table.Column<int>(type: "int", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentReference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsReconciled = table.Column<bool>(type: "bit", nullable: false),
                    ReconciledDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReconciledByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PaymentLinkId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsReversed = table.Column<bool>(type: "bit", nullable: false),
                    ReversalEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyAccountEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartyAccountEntries_PartyAccounts_PartyAccountId",
                        column: x => x.PartyAccountId,
                        principalTable: "PartyAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartyAccountEntries_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PartyAccountEntries_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartyAccountEntries_Users_ReconciledByUserId",
                        column: x => x.ReconciledByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_PartyId",
                table: "Transactions",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_Parties_CreatedByUserId",
                table: "Parties",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Parties_Email",
                table: "Parties",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Parties_ModifiedByUserId",
                table: "Parties",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Parties_OfficeId_PartyCode",
                table: "Parties",
                columns: new[] { "OfficeId", "PartyCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Parties_TaxNumber",
                table: "Parties",
                column: "TaxNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PartyAccountEntries_CreatedByUserId",
                table: "PartyAccountEntries",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyAccountEntries_DueDate",
                table: "PartyAccountEntries",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_PartyAccountEntries_EntryNumber",
                table: "PartyAccountEntries",
                column: "EntryNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartyAccountEntries_PartyAccountId_EntryDate",
                table: "PartyAccountEntries",
                columns: new[] { "PartyAccountId", "EntryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PartyAccountEntries_PaymentLinkId",
                table: "PartyAccountEntries",
                column: "PaymentLinkId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyAccountEntries_PaymentStatus",
                table: "PartyAccountEntries",
                column: "PaymentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_PartyAccountEntries_ReconciledByUserId",
                table: "PartyAccountEntries",
                column: "ReconciledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyAccountEntries_ReversalEntryId",
                table: "PartyAccountEntries",
                column: "ReversalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyAccountEntries_TransactionId",
                table: "PartyAccountEntries",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyAccounts_AccountNumber",
                table: "PartyAccounts",
                column: "AccountNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartyAccounts_CurrencyId",
                table: "PartyAccounts",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyAccounts_PartyId_CurrencyId",
                table: "PartyAccounts",
                columns: new[] { "PartyId", "CurrencyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartyContacts_PartyId_Email",
                table: "PartyContacts",
                columns: new[] { "PartyId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_PartyContacts_PartyId1",
                table: "PartyContacts",
                column: "PartyId1");

            migrationBuilder.CreateIndex(
                name: "IX_PartyCreditLimits_ApprovedByUserId",
                table: "PartyCreditLimits",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyCreditLimits_CurrencyId",
                table: "PartyCreditLimits",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyCreditLimits_PartyId_CurrencyId_EffectiveFrom",
                table: "PartyCreditLimits",
                columns: new[] { "PartyId", "CurrencyId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_PartyStatements_CurrencyId",
                table: "PartyStatements",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyStatements_GeneratedByUserId",
                table: "PartyStatements",
                column: "GeneratedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyStatements_PartyId_CurrencyId_StatementDate",
                table: "PartyStatements",
                columns: new[] { "PartyId", "CurrencyId", "StatementDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PartyStatements_StatementNumber",
                table: "PartyStatements",
                column: "StatementNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Parties_PartyId",
                table: "Transactions",
                column: "PartyId",
                principalTable: "Parties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Parties_PartyId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "PartyAccountEntries");

            migrationBuilder.DropTable(
                name: "PartyContacts");

            migrationBuilder.DropTable(
                name: "PartyCreditLimits");

            migrationBuilder.DropTable(
                name: "PartyStatements");

            migrationBuilder.DropTable(
                name: "PartyAccounts");

            migrationBuilder.DropTable(
                name: "Parties");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_PartyId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PartyId",
                table: "Transactions");
        }
    }
}
