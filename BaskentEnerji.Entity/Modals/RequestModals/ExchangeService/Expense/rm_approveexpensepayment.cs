using System.ComponentModel.DataAnnotations;

namespace BaskentEnerji.Entity.Modals.RequestModals.ExchangeService.Expense
{
    public class rm_approveexpensepayment
    {
        [Required]
        public bool Approve { get; set; }

        [StringLength(500)]
        public string? RejectionNote { get; set; }
    }
}
