using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaskentEnerji.Entity.Entities.Telegram
{
    // Bayiye özel, para birimi bazında settlement (mutabakat) kuru — Owner'ın bu bayiden
    // aldığı/bu bayiye verdiği gerçek kur. Müşteriye gösterilen kur (TgExchangeRate) ile
    // KASITLI olarak ayrı tutulur: kâr, ikisi arasındaki farktan (spread) doğar.
    [Table("TgDealerRates")]
    public class TgDealerRate
    {
        [Key]
        public int Id { get; set; }

        public int DealerId { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "";

        [Column(TypeName = "decimal(18,6)")]
        public decimal BuyRate { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal SellRate { get; set; }

        public DateTime UpdatedAt { get; set; }

        [ForeignKey(nameof(DealerId))]
        public TgDealer? Dealer { get; set; }
    }
}
