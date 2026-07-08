using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaskentEnerji.Entity.Entities.Telegram
{
    [Table("TgDealers")]
    public class TgDealer
    {
        [Key]
        public int DealerId { get; set; }

        [MaxLength(50)]
        public string DealerCode { get; set; } = "";

        [MaxLength(100)]
        public string DealerName { get; set; } = "";

        public bool IsActive { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal Balance { get; set; }

        [MaxLength(100)]
        public string? VaultId { get; set; }

        [MaxLength(200)]
        public string? CryptoAddress { get; set; }

        [MaxLength(20)]
        public string? CryptoNetwork { get; set; }

        [MaxLength(50)]
        public string? ExchangeName { get; set; }

        [MaxLength(200)]
        public string? ApiKey { get; set; }

        [MaxLength(200)]
        public string? ApiSecret { get; set; }

        public DateTime CreatedAt { get; set; }

        public Guid? PartyId { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal CommissionRate { get; set; } = 1.5m;
    }
}
