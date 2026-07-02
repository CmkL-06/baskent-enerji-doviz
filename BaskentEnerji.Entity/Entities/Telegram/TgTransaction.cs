using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaskentEnerji.Entity.Entities.Telegram
{
    [Table("TgTransactions")]
    public class TgTransaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TransactionId { get; set; }

        public long? CustomerId { get; set; }

        [MaxLength(10)]
        public string? Currency { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? Amount { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? ExchangeRate { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? TryAmount { get; set; }

        [MaxLength(30)]
        public string? Status { get; set; }

        [MaxLength(50)]
        public string? ReferralCode { get; set; }

        public long? AssignedOperatorId { get; set; }

        [MaxLength(20)]
        public string? CompletionCode { get; set; }

        [MaxLength(200)]
        public string? Txid { get; set; }

        public bool? CryptoVerified { get; set; }
        public DateTime? CryptoVerifiedAt { get; set; }
        public bool? IsBuy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? StateData { get; set; }

        [MaxLength(50)]
        public string? IdempotencyKey { get; set; }

        [ForeignKey("CustomerId")]
        public TgCustomer? Customer { get; set; }

        public ICollection<TgMessage> Messages { get; set; } = new List<TgMessage>();
        public ICollection<TgCryptoDeposit> CryptoDeposits { get; set; } = new List<TgCryptoDeposit>();
        public ICollection<TgChatSession> ChatSessions { get; set; } = new List<TgChatSession>();
    }
}
