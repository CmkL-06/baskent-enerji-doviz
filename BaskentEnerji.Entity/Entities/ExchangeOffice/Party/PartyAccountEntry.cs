using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Entities.ExchangeOffice.Party
{
    public class PartyAccountEntry : BaseEntity
    {
        public Guid PartyAccountId { get; set; }
        public Guid? TransactionId { get; set; }
        public string EntryNumber { get; set; }
        public DateTime EntryDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string? ReferenceNumber { get; set; }
        public EntryType Type { get; set; }
        public decimal Amount { get; set; } // Hesabın kendi para birimi cinsinden orijinal işlem tutarı
        // Cari borç/alacak TL bazlı tek net pozisyon mantığı için TL karşılığı — farklı döviz cinsi
        // ödemeler bu alan üzerinden PartyAccount.Balance'ı (TL) doğrudan etkiler, ayrı hesap açmadan.
        public decimal? AmountInTRY { get; set; }
        public decimal PaidAmount { get; set; } = 0; // Bu kaleme kısmi/tam olarak ödenmiş toplam tutar
        public decimal RunningBalance { get; set; }
        public string Description { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentReference { get; set; }
        public bool IsReconciled { get; set; }
        public DateTime? ReconciledDate { get; set; }
        public Guid? ReconciledByUserId { get; set; }
        public Guid? PaymentLinkId { get; set; }
        public bool IsReversed { get; set; }
        public Guid? ReversalEntryId { get; set; }
        public Guid? CreatedByUserId { get; set; }

        // Orijinal döviz bilgileri
        public Guid? OriginalCurrencyId { get; set; }
        public decimal? OriginalAmount { get; set; }
        public decimal? ExchangeRate { get; set; }
        public bool IsCustomRate { get; set; } = false;

        // Navigation properties
        public PartyAccount PartyAccount { get; set; }
        public Office.Transaction Transaction { get; set; }
        public User.User ReconciledByUser { get; set; }
        public User.User CreatedByUser { get; set; }
        public Currency.Currency OriginalCurrency { get; set; }
    }

    public enum EntryType
    {
        Debit = 1,
        Credit = 2
    }

    public enum PaymentStatus
    {
        Pending = 1,
        Paid = 2,
        PartiallyPaid = 3,
        Overdue = 4,
        Cancelled = 5
    }
}