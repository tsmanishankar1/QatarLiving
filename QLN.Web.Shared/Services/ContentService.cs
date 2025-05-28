using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QLN.Web.Shared.Services.Interface;
using System.Net;

namespace QLN.Web.Shared.Services
{
    public class ContentService : IContentService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public ContentService(HttpClient httpClient, IOptions<ApiSettings> options)
        {
            _httpClient = httpClient;
            _baseUrl = "https://qlc-bo-dev.qatarliving.com/";
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage?> GetDailyLPAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}api/content/qln_contents_daily/landing");
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetDailyLPAsync" + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public Task<HttpResponseMessage?> GetEventBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage?> GetEventsFromDrupalAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage?> GetPostBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<T?> GetPostsFromDrupalAsync<T>(string queue_name, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
