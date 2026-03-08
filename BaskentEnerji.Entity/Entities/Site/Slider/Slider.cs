using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Entity.Entities.Site.Slider
{
    public class Slider : BaseEntity
    {
        public string Name { get; set; }
        public Guid LanguageId { get; set; }
        public Language Language { get; set; }
        public bool IsHome { get; set; } = false;
        public bool IsBlog { get; set; } = false;
        public bool IsEnabled { get; set; }
       public ICollection<Slider_Item> Slider_Items { get; set; } = new List<Slider_Item>();
    }
}
