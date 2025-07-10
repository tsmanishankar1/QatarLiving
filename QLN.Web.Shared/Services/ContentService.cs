using Microsoft.Extensions.Options;
using QLN.Web.Shared.Services.Interface;
using System.Net;

namespace QLN.Web.Shared.Services
{
    public class ContentService : IContentService
    {
        private readonly HttpClient _httpClient;
        private readonly NavigationPath _navigationPath;

        public ContentService(
            HttpClient httpClient,
            IOptions<NavigationPath> navigationPath
            )
        {
            _httpClient = httpClient;
            _navigationPath = navigationPath.Value;
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage?> GetDailyLPAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(_navigationPath.ContentDailyBackEnd);


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
                var response = await _httpClient.GetAsync(_navigationPath.ContentDailyVideosBackEnd);
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
