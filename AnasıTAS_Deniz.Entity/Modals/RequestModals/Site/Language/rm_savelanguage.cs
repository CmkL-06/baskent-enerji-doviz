using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Entity.Modals.RequestModals.Site.Language
{
    public class rm_savelanguage
    {
        public Guid? Id { get; set; } 
        public string LanguageName { get; set; }
        public string LanguageCode { get; set; }
        public string FlagUri { get; set; }
        public bool IsDefault { get; set; }
        public bool IsEnabled { get; set; }
    }
}
