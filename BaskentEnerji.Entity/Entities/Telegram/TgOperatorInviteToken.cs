using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaskentEnerji.Entity.Entities.Telegram
{
    [Table("TgOperatorInviteTokens")]
    public class TgOperatorInviteToken
    {
        [Key]
        public Guid Id { get; set; }

        [MaxLength(32)]
        public string Token { get; set; } = null!;

        public Guid CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? UsedAt { get; set; }
        public long? UsedByTelegramId { get; set; }
    }
}
