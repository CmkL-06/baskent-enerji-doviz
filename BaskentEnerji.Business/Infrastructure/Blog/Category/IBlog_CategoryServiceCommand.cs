using BaskentEnerji.Entity.Modals.RequestModals.Blog;
using BaskentEnerji.Entity.Modals.RequestModals.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaskentEnerji.Business.Infrastructure.Blog.Category
{
    public interface IBlog_CategoryServiceCommand
    {
        Task SaveCategory(rm_savecategory data);
        Task DeleteCategory(Guid id);
        Task SetParentcategory(rm_sourcetarget data);
    }
}
