using MoneyTransferTurkey.Entity.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Entity.Modals.ViewModals.Slider
{
    public class vm_slideritem :BaseEntity
    {
        public string? Name { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Description2 { get; set; }
        public string? CallText { get; set; }
        public string? ImageUrl { get; set; }
        public string? VideoUrl { get; set; }
        public Guid SliderId { get; set; }
        public string SliderName { get; set; }
        bool IsVideo { get; set; } = false;
        public bool IsEnabled { get; set; }
    }
}
