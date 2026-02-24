using System;

namespace AnasıTAS_Deniz.Entity.Modals.ViewModals.ExchangeOFfice.Office
{
    public class vm_useroffice
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid OfficeId { get; set; }
        public User.vm_user User { get; set; }
        public vm_office Office { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}