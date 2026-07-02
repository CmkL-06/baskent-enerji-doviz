using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaskentEnerji.Entity.Entities.Telegram
{
    [Table("TgMessages")]
    public class TgMessage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MessageId { get; set; }

        public int? TransactionId { get; set; }
        public long? SenderId { get; set; }

        [MaxLength(20)]
        public string? SenderType { get; set; }

        public string? MessageText { get; set; }

        [MaxLength(500)]
        public string? FileUrl { get; set; }

        [MaxLength(20)]
        public string? FileType { get; set; }

        public DateTime? CreatedAt { get; set; }

        [ForeignKey("TransactionId")]
        public TgTransaction? Transaction { get; set; }
    }
}
