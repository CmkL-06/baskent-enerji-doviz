using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaskentEnerji.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramMttTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DealerReferralCode",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "TelegramOperatorId",
                table: "Users",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TgApiQueue",
                columns: table => new
                {
                    QueueId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: true),
                    MaxAttempts = table.Column<int>(type: "int", nullable: true),
                    LastAttempt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TgApiQueue", x => x.QueueId);
                });

            migrationBuilder.CreateTable(
                name: "TgBankProviders",
                columns: table => new
                {
                    ProviderId = table.Column<long>(type: "bigint", nullable: false),
                    AddedBy = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TgBankProviders", x => x.ProviderId);
                });

            migrationBuilder.CreateTable(
                name: "TgBotHeartbeats",
                columns: table => new
                {
                    BotName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    LastHeartbeat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastTransactionAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActiveSessions = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TgBotHeartbeats", x => x.BotName);
                });

            migrationBuilder.CreateTable(
                name: "TgCustomers",
                columns: table => new
                {
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LanguageCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ReferralCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastActivity = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TgCustomers", x => x.CustomerId);
                });

            migrationBuilder.CreateTable(
                name: "TgLoginLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PanelType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TgLoginLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TgOperators",
                columns: table => new
                {
                    OperatorId = table.Column<long>(type: "bigint", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    IsAdmin = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TgOperators", x => x.OperatorId);
                });

            migrationBuilder.CreateTable(
                name: "TgTransactions",
                columns: table => new
                {
                    TransactionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<long>(type: "bigint", nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    ExchangeRate = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    TryAmount = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ReferralCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AssignedOperatorId = table.Column<long>(type: "bigint", nullable: true),
                    CompletionCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Txid = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CryptoVerified = table.Column<bool>(type: "bit", nullable: true),
                    CryptoVerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsBuy = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StateData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TgTransactions", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_TgTransactions_TgCustomers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "TgCustomers",
                        principalColumn: "CustomerId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TgChatSessions",
                columns: table => new
                {
                    SessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<long>(type: "bigint", nullable: true),
                    OperatorId = table.Column<long>(type: "bigint", nullable: true),
                    TransactionId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TgChatSessions", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK_TgChatSessions_TgTransactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "TgTransactions",
                        principalColumn: "TransactionId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TgCryptoDeposits",
                columns: table => new
                {
                    DepositId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionId = table.Column<int>(type: "int", nullable: true),
                    DealerId = table.Column<int>(type: "int", nullable: true),
                    Txid = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    Network = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ToAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DepositTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Confirmations = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TgCryptoDeposits", x => x.DepositId);
                    table.ForeignKey(
                        name: "FK_TgCryptoDeposits_TgTransactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "TgTransactions",
                        principalColumn: "TransactionId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TgMessages",
                columns: table => new
                {
                    MessageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionId = table.Column<int>(type: "int", nullable: true),
                    SenderId = table.Column<long>(type: "bigint", nullable: true),
                    SenderType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MessageText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FileType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TgMessages", x => x.MessageId);
                    table.ForeignKey(
                        name: "FK_TgMessages_TgTransactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "TgTransactions",
                        principalColumn: "TransactionId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TgChatSessions_TransactionId",
                table: "TgChatSessions",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_TgCryptoDeposits_TransactionId",
                table: "TgCryptoDeposits",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_TgMessages_TransactionId",
                table: "TgMessages",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_TgTransactions_CustomerId",
                table: "TgTransactions",
                column: "CustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TgApiQueue");

            migrationBuilder.DropTable(
                name: "TgBankProviders");

            migrationBuilder.DropTable(
                name: "TgBotHeartbeats");

            migrationBuilder.DropTable(
                name: "TgChatSessions");

            migrationBuilder.DropTable(
                name: "TgCryptoDeposits");

            migrationBuilder.DropTable(
                name: "TgLoginLogs");

            migrationBuilder.DropTable(
                name: "TgMessages");

            migrationBuilder.DropTable(
                name: "TgOperators");

            migrationBuilder.DropTable(
                name: "TgTransactions");

            migrationBuilder.DropTable(
                name: "TgCustomers");

            migrationBuilder.DropColumn(
                name: "DealerReferralCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TelegramOperatorId",
                table: "Users");
        }
    }
}
