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
        public NewsService(HttpClient httpClientDI, ILogger<NewsService> Logger)
           : base(httpClientDI, Logger)
        {

        }

        public async Task<HttpResponseMessage> CreateArticle(NewsArticleDTO newsArticle)
        {
            try
            {
                var newsArticleJson = new StringContent(JsonSerializer.Serialize(newsArticle), Encoding.UTF8, "application/json");
                var response = await PostAsync("api/v2/news/createNewsArticle", newsArticleJson);
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
                var response = await GetAsync($"api/v2/news/getAllNewsArticle");
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
                var response = await GetAsync($"api/v2/news/getCategories");
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
                var response = await GetAsync($"api/v2/news/slots");
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
                var response = await GetAsync($"api/v2/news/getWriterTags");
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
                var response = await PostAsync("api/v2/news/updateNewsArticle", newsArticleJson);
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "UpdateArticle");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
    }
}
