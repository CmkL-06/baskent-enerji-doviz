using AnasıTAS_Deniz.Entity.Entities.Site;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Entity.Entities.User
{
    public class User : BaseEntity
    {
        public string Username { get; set; }

        [JsonIgnore]
        public string Password { get; set; }
        public string Mail { get; set; }
        public bool IsEmailVerified { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public Gender Gender { get; set; }
        public Rank Rank { get; set; } = Rank.User;
        public string? LanguageCode { get; set; }
        public string? FirstIp { get; set; }
        public string? LastIp { get; set; }
        public DateTime? LastPasswordChangeDate { get; set; }

    }
}
