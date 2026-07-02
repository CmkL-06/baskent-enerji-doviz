using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BaskentEnerji.Entity.Entities.Telegram
{
    [Table("TgCustomers")]
    public class TgCustomer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long CustomerId { get; set; }

        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

        [MaxLength(100)]
        public string? Username { get; set; }

        [MaxLength(10)]
        public string? LanguageCode { get; set; }

        [MaxLength(50)]
        public string? ReferralCode { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? LastActivity { get; set; }

        public ICollection<TgTransaction> Transactions { get; set; } = new List<TgTransaction>();
    }
}
