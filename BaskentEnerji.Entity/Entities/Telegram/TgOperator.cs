using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaskentEnerji.Entity.Entities.Telegram
{
    [Table("TgOperators")]
    public class TgOperator
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long OperatorId { get; set; }

        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? Username { get; set; }

        public bool? IsActive { get; set; }
        public bool? IsAdmin { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
