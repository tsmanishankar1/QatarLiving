using QLN.Web.Shared.Services.Interface;
using System.Net;

namespace QLN.Web.Shared.Services
{
    public class ClassifiedsServices : IClassifiedsServices
    {
        private readonly HttpClient _httpClient;

        public ClassifiedsServices(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage?> GetClassifiedsLPAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/landing/classifieds");
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetDailyLPAsync" + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
    }
}
