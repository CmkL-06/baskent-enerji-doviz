using Microsoft.AspNetCore.Http;
using MoneyTransferTurkey.Entity.Modals.RequestModals.Blog;
using MoneyTransferTurkey.Entity.Modals.ResponseModals.Blog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoneyTransferTurkey.Business.Infrastructure.Blog.Article
{
    public interface IBlog_ArticleServiceQuery
    {
        List<rsp_article> GetArticles(rm_article data);
        rsp_article GetArticle(Guid Id);
        List<rsp_article_guest> GetArticles_Guest(rm_article_guest data);
        rsp_article_guest GetArticle_Guest(string seoLink, HttpContext httpContext);
    }
}
