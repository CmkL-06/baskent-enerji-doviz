using MoneyTransferTurkey.Entity.Entities.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Entity.Entities.ExchangeOffice.Office
{
    public class User_Office :BaseEntity
    {
        public Guid UserId { get; set; }
        public User.User User { get; set; }
        public Guid OfficeId { get; set; }
        public Office Office { get; set; }
    }
}
