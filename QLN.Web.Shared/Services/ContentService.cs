using QLN.Web.Shared.Services.Interface;
using System.Net;

namespace QLN.Web.Shared.Services
{
    public class ContentService : IContentService
    {
        private readonly HttpClient _httpClient;

        public ContentService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage?> GetDailyLPAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/v2/dailyliving/landing");
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

        public async Task<HttpResponseMessage?> GetBannerAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/banner");
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetBannerAsync: " + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage?> GetVideosLPAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/content/qln_videos/landing");
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetVideosLPAsync: " + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
    }
}
