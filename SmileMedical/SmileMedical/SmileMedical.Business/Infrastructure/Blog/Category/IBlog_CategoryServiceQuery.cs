using SmileMedical.Entity.Modals.RequestModals.Blog;
using SmileMedical.Entity.Modals.ResponseModals.Blog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmileMedical.Business.Infrastructure.Blog.Category
{
    public interface IBlog_CategoryServiceQuery
    {
        List<rsp_category_name> getCategoryNames(Guid? langId, Guid? ArticleId, Guid? catId, bool? isEnabled, bool? isUnique, bool? withoutDescription);
        Task<List<rsp_category>> getCategories(rm_category data);
        rsp_category getCategory(Guid catId, string? link);
        List<rsp_category_guest> getCategories_Guest(rm_category_guest data);

    }
}
