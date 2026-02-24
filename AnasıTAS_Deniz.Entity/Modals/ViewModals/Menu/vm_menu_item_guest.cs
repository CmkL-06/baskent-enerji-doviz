using AnasıTAS_Deniz.Entity.Entities.Blog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Entity.Modals.ViewModals.Menu
{
    public class vm_menu_item_guest
    {
        public string Name { get; set; }
        public string SeoTitle { get; set; }
        public string SeoLink { get; set; }
        public string CustomLink { get; set; }
        public int Order { get; set; }
    }
}
