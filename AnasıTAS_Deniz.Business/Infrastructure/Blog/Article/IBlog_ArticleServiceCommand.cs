using Microsoft.AspNetCore.Http;
using AnasıTAS_Deniz.Entity.Modals.RequestModals.Blog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnasıTAS_Deniz.Business.Infrastructure.Blog.Article
{
    public interface IBlog_ArticleServiceCommand
    {
        Task SaveArticle(rm_savearticle data);
        Task AddCategory(List<rm_article_addcategory> data);
        Task DeleteArticle(Guid articleId);
        Task AddComment(rm_article_comment data, HttpContext httpContext);
        Task RemoveComment(Guid ArticleId );
    }
}
