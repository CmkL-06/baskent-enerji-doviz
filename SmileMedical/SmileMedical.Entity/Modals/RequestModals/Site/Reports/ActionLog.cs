using SmileMedical.Entity.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Entity.Modals.RequestModals.Site.Reports
{
    public class ActionLog : BaseEntity
    {
        public Guid? UserId { get; set; }
        public Entities.User.User? User { get; set; }
        public string? UserDescription { get; set; }
        public string? Description { get; set; }

        public LogType LogType { get; set; }
        public ActionType ActionType { get; set; }
        public Guid? ContentId { get; set; }
    }
}
