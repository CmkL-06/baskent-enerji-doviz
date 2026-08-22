using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Entities.ExchangeOffice.Office
{
    public class VaultBalanceHistory : BaseEntity
    {
        public Guid VaultId { get; set; }
        public Guid CurrencyId { get; set; }
        public Guid? UserId { get; set; }
        public decimal Balance { get; set; }
       
       
        public string? Description { get; set; }

        public Guid? TransferReferenceId { get; set; }

        public Vault Vault { get; set; }
        public Currency.Currency Currency { get; set; }
        public bool IsDeleted { get; set; }
        public string? DeletedReason { get; set; }
        public Guid? DeletedByUserId { get; set; }
        public TransactionType TransactionType { get; set; }
        public bool IsGhost { get; set; } = false;
        public bool IsParty { get; set; } = false;

        // TransactionType.Adjustment iki farklı anlamda kullanılıyor: "elle düzeltildi" (tam bakiye
        // override — Balance alanı MUTLAK yeni bakiyeyi tutar) ile "kasa sayımı"/"gün kapanışı sayım
        // farkı" (Balance alanı sadece FARKI/delta'yı tutar). Bu ayrım olmadan bakiye replay/void
        // mantığı (VaultService.VoidVaultBalanceHistoryAsync) her iki türü de aynı kabul edip mutlak
        // değer gibi atıyor, delta kayıtlarında bakiyeyi yanlışlıkla o küçük delta değerine sıfırlıyor.
        public bool IsAbsoluteBalance { get; set; } = false;

        // Kasa çıkışı bir ExpensePayment onayından tetiklenmişse doğrudan FK ile bağlanır —
        // eskiden yalnızca Description metniyle eşleşiyordu; reverse-lookup / rapor / iptal
        // senaryolarında kırılgandı.
        public Guid? ExpensePaymentId { get; set; }
        public BaskentEnerji.Entity.Entities.ExchangeOffice.Expense.ExpensePayment? ExpensePayment { get; set; }
    }
}
