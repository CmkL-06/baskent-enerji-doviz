using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaskentEnerji.Entity.Entities.Telegram
{
    // Harici bayilerin settlement kuru (TgDealerRate) değiştikçe önceki değeri burada saklar.
    [Table("TgDealerRateHistories")]
    public class TgDealerRateHistory
    {
        [Key]
        public int Id { get; set; }

        public int DealerId { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "";

        [Column(TypeName = "decimal(18,6)")]
        public decimal OldBuyRate { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal OldSellRate { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal NewBuyRate { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal NewSellRate { get; set; }

        public DateTime ChangedAt { get; set; }

        [ForeignKey(nameof(DealerId))]
        public TgDealer? Dealer { get; set; }
    }
}
