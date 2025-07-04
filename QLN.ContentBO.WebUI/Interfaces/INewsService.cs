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

        Task<HttpResponseMessage> GetArticlesByCategory(int categoryId);

        Task<HttpResponseMessage> GetArticlesBySubCategory(int categoryId, int subCategoryId);

        Task<HttpResponseMessage> GetArticleBySlug(string slug);

        Task<HttpResponseMessage> GetArticleById(Guid Id);
        
        Task<HttpResponseMessage> ReOrderNews(ArticleSlotAssignment slotAssignment);

        Task<HttpResponseMessage> DeleteNews(Guid Id);

        Task<HttpResponseMessage> UpdateSubCategory(NewsSubCategory subCategory);
    }
}
