using System;
using System.ComponentModel.DataAnnotations;

namespace BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Expense
{
    public class rm_expensecategory
    {
        public Guid? Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
