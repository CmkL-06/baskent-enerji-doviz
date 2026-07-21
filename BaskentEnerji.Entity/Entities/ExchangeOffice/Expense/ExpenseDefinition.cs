using System;
using System.Collections.Generic;

namespace BaskentEnerji.Entity.Entities.ExchangeOffice.Expense
{
    public class ExpenseDefinition : BaseEntity
    {
        public Guid OfficeId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public Guid CategoryId { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsRecurring { get; set; } = false;
        public RecurrencePeriod? RecurrencePeriod { get; set; }
        public decimal? DefaultAmount { get; set; }
        public Guid? DefaultCurrencyId { get; set; }

        // Abone/sözleşme no gibi dönemden döneme değişmeyen sabit referans (elektrik/su/internet
        // abone numarası, kira sözleşme no vb.) — her ödemenin kendi ReferenceNumber'ı (fatura no)
        // ile karıştırılmamalı.
        public string? AccountReference { get; set; }

        // Yalnızca RecurrencePeriod == Monthly için anlamlı: ayın hangi günü vade geldiği (örn.
        // kira ayın 5'i). Boş ise vade, son ödemeden itibaren dönemsel olarak hesaplanır.
        public int? DueDayOfMonth { get; set; }

        // Navigation properties
        public Office.Office Office { get; set; }
        public Currency.Currency DefaultCurrency { get; set; }
        public ExpenseCategoryDefinition Category { get; set; }
        public ICollection<ExpensePayment> Payments { get; set; }
    }

    public enum RecurrencePeriod
    {
        Daily = 1,
        Weekly = 2,
        Monthly = 3,
        Quarterly = 4,
        Yearly = 5
    }
}