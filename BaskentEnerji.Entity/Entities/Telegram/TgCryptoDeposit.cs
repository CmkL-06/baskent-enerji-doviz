using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaskentEnerji.Entity.Entities.Telegram
{
    [Table("TgCryptoDeposits")]
    public class TgCryptoDeposit
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DepositId { get; set; }

        public int? TransactionId { get; set; }
        public int? DealerId { get; set; }

        [MaxLength(200)]
        public string? Txid { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? Amount { get; set; }

        [MaxLength(20)]
        public string? Network { get; set; }

        [MaxLength(200)]
        public string? ToAddress { get; set; }

        public DateTime? DepositTime { get; set; }
        public int? Confirmations { get; set; }

        [MaxLength(20)]
        public string? Status { get; set; }

        [ForeignKey("TransactionId")]
        public TgTransaction? Transaction { get; set; }
    }
}
