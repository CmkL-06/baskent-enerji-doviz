using System;
using System.Collections.Generic;

namespace BaskentEnerji.Entity.Entities.ExchangeOffice.Expense
{
    // Kullanıcı tarafından yönetilebilen gider kategorisi (Currency entity'siyle aynı desende —
    // global, DB-backed, sabit enum değil). Eskiden ExpenseCategory enum'unda sabitlenmiş 11
    // değer (Maaş, Kira, Faturalar vb.) artık bu tablonun satırları — kullanıcı yenisini ekleyebilir.
    public class ExpenseCategoryDefinition : BaseEntity
    {
        public string Name { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<ExpenseDefinition> ExpenseDefinitions { get; set; }
        public ICollection<ExpenseBudget> ExpenseBudgets { get; set; }
    }
}
