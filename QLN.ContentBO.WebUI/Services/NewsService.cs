using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Services.Base;
using System.Net;
using System.Text;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Services
{
    public class NewsService : ServiceBase<NewsService>, INewsService
    {
        private readonly HttpClient _httpClient;

        public NewsService(HttpClient httpClient, ILogger<NewsService> Logger) : base(httpClient, Logger)
        {
            _httpClient = httpClient;
        }

        public async Task<HttpResponseMessage> CreateArticle(NewsArticleDTO newsArticle)
        {
            try
            {
                var newsArticleJson = new StringContent(JsonSerializer.Serialize(newsArticle), Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Post, "api/v2/news/news")
                {
                    Content = newsArticleJson
                };

                var response = await _httpClient.SendAsync(request);

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "CreateArticle");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage> GetAllArticles()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "api/v2/news/articles");

                var response = await _httpClient.SendAsync(request);

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetAllArticles");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage> GetNewsCategories()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "api/v2/news/allcategories");

                var response = await _httpClient.SendAsync(request);

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetNewsCategories");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage> GetSlots()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "api/v2/news/slots");

                var response = await _httpClient.SendAsync(request);

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetSlots");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage> GetWriterTags()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "api/v2/news/writertags");

                var response = await _httpClient.SendAsync(request);

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetWriterTags");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage> UpdateArticle(NewsArticleDTO newsArticle)
        {
            try
            {
                var newsArticleJson = new StringContent(JsonSerializer.Serialize(newsArticle), Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Put, "api/v2/news/updatenews")
                {
                    Content = newsArticleJson
                };

                var response = await _httpClient.SendAsync(request);

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "UpdateArticle");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage> GetArticlesByCategory(int categoryId)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, 
                    $"api/v2/news/categories/{categoryId}");

                var response = await _httpClient.SendAsync(request);

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetArticlesByCategory");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage> GetArticlesBySubCategory(int categoryId, int subCategoryId)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"api/v2/news/categories/{categoryId}/sub/{subCategoryId}");

                var response = await _httpClient.SendAsync(request);

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetArticlesBySubCategory");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage> GetArticleBySlug(string slug)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"api/v2/news/getbyslug/{slug}");

                var response = await _httpClient.SendAsync(request);

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetArticleBySlug");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage> GetArticleById(Guid id)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"api/v2/news/getbyid/{id}");

                var response = await _httpClient.SendAsync(request);

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetArticleById");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage> ReOrderNews(ArticleSlotAssignment slotAssignment, int UserId)
        {
            try
            {
                var payload = new
                {
                    slotAssignments = slotAssignment,
                    userId = UserId
                };
                var articleReOrderJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });

                var request = new HttpRequestMessage(HttpMethod.Post, "api/v2/news/reorderslot")
                {
                    Content = new StringContent(articleReOrderJson, Encoding.UTF8, "application/json")
                };

                var response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ReOrderNews");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage> DeleteNews(Guid id)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Delete, $"api/v2/news/news/{id}");

                var response = await _httpClient.SendAsync(request);

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "DeleteNews");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage> UpdateSubCategory(int categoryId, NewsSubCategory subCategory)
        {
            try
            {
                var subCategoryToUpdate = new StringContent(JsonSerializer.Serialize(subCategory), Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Put, $"api/v2/news/category/updatesubcategory?categoryId={categoryId}")
                {
                    Content = subCategoryToUpdate
                };

                var response = await _httpClient.SendAsync(request);

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "UpdateSubCategory");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage> SearchArticles(string searchString)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"api/v2/news/news?search={searchString}");

                var response = await _httpClient.SendAsync(request);

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SearchArticles");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
    }
}
