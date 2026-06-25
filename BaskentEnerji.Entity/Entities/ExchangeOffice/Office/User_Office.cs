using BaskentEnerji.Entity.Entities.User;
using System;

namespace BaskentEnerji.Entity.Entities.ExchangeOffice.Office
{
    public class User_Office : BaseEntity
    {
        public Guid UserId { get; set; }
        public User.User User { get; set; }
        public Guid OfficeId { get; set; }
        public Office Office { get; set; }

        // Ofis bazlı rol — global User.Rank'tan bağımsız
        public OfficeRole Role { get; set; } = OfficeRole.Cashier;
        public bool IsActive { get; set; } = true;
    }
}
