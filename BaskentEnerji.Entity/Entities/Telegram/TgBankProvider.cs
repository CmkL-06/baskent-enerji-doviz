using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaskentEnerji.Entity.Entities.Telegram
{
    [Table("TgBankProviders")]
    public class TgBankProvider
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long ProviderId { get; set; }

        public long? AddedBy { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
