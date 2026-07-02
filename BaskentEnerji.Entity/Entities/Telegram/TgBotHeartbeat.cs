using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaskentEnerji.Entity.Entities.Telegram
{
    [Table("TgBotHeartbeats")]
    public class TgBotHeartbeat
    {
        [Key]
        [MaxLength(30)]
        public string BotName { get; set; } = null!;

        public DateTime? LastHeartbeat { get; set; }
        public DateTime? LastTransactionAt { get; set; }
        public int? ActiveSessions { get; set; }
    }
}
