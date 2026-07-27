using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaskentEnerji.Entity.Entities.Telegram
{
    [Table("TgApiQueue")]
    public class TgApiQueue
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int QueueId { get; set; }

        public int? TransactionId { get; set; }

        [MaxLength(20)]
        public string? Status { get; set; }

        // 'exchange' (varsayilan), 'dealer_entry' (cari hesap kaydi retry'i),
        // 'exchange_missing_rate' (kur eksik, otomatik tekrar denenmez)
        [MaxLength(20)]
        public string? OperationType { get; set; }

        public int? Attempts { get; set; }
        public int? MaxAttempts { get; set; }
        public DateTime? LastAttempt { get; set; }

        [MaxLength(500)]
        public string? LastError { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
