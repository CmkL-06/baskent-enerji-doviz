using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaskentEnerji.Entity.Entities.Telegram
{
    [Table("TgExchangeRates")]
    public class TgExchangeRate
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "";

        [Column(TypeName = "decimal(18,6)")]
        public decimal BuyRate { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal SellRate { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
