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
                var request = new HttpRequestMessage(HttpMethod.Post, "api/v2/news/createNewsArticle")
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
                var request = new HttpRequestMessage(HttpMethod.Get, "api/v2/news/getAllNewsArticle");

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
                var request = new HttpRequestMessage(HttpMethod.Get, "api/v2/news/getCategories");

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
                var request = new HttpRequestMessage(HttpMethod.Get, "api/v2/news/getWriterTags");

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
                var request = new HttpRequestMessage(HttpMethod.Put, "api/v2/news/updateNewsArticle")
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
    }
}
