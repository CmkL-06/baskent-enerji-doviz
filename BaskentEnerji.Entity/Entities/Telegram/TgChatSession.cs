using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaskentEnerji.Entity.Entities.Telegram
{
    [Table("TgChatSessions")]
    public class TgChatSession
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SessionId { get; set; }

        public long? CustomerId { get; set; }
        public long? OperatorId { get; set; }
        public int? TransactionId { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        [ForeignKey("TransactionId")]
        public TgTransaction? Transaction { get; set; }
    }
}
