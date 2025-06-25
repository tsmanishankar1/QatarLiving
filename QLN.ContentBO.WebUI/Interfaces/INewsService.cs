using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Interfaces
{
    public interface INewsService
    {
        Task<HttpResponseMessage> GetAllArticles();
        
        Task<HttpResponseMessage> CreateArticle(NewsArticleDTO newsArticle);

        Task<HttpResponseMessage> UpdateArticle(NewsArticleDTO newsArticle);

        Task<HttpResponseMessage> GetNewsCategories();
        
        Task<HttpResponseMessage> GetWriterTags();

        Task<HttpResponseMessage> GetSlots();

    }
}
