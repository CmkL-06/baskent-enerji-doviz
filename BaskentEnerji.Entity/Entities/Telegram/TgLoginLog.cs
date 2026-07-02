using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaskentEnerji.Entity.Entities.Telegram
{
    [Table("TgLoginLogs")]
    public class TgLoginLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(100)]
        public string? Username { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [MaxLength(20)]
        public string? PanelType { get; set; }

        public bool Success { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
