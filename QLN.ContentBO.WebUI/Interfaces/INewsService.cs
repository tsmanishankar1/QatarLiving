using QLN.ContentBO.WebUI.Models;
using static QLN.ContentBO.WebUI.Pages.NewsPage.NewsBase;

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

        Task<HttpResponseMessage> GetArticlesBySubCategory(int categoryId, int subCategoryId, int? status = 0, int? page = null, int? pageSize = null, string? search = null);

        Task<HttpResponseMessage> GetArticleBySlug(string slug);

        Task<HttpResponseMessage> GetArticleById(Guid Id);

        Task<HttpResponseMessage> ReOrderNews(ReorderRequest slotAssignment, string UserId);

        Task<HttpResponseMessage> DeleteNews(Guid Id);

        Task<HttpResponseMessage> UpdateSubCategory(int categoryId, NewsSubCategory subCategory);

        Task<HttpResponseMessage> SearchArticles(string searchString);
    }
}
