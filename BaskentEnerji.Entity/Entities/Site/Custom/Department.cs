using BaskentEnerji.Entity.Entities;

namespace BaskentEnerji.Entity.Entities.Site.Custom
{
    public class Department : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
