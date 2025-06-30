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
                var response = await PostAsync("api/CreateArticle", newsArticleJson);
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "CreateArticle");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage> GetAllArticle(int id)
        {
            try
            {
                var response = await GetAsync($"api/GetAllArticles");
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetAllArticle");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public Task<HttpResponseMessage> GetAllArticles()
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> GetNewsCategories()
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> GetSlots()
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> GetWriterTags()
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> UpdateArticle(NewsArticleDTO newsArticle)
        {
            throw new NotImplementedException();
        }
    }
}
