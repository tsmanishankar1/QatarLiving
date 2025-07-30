using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Services.Base;
using System.Net;
using System.Text;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Services
{
    public class ServicesService : ServiceBase<ServicesService>, IServiceService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ServicesService> _logger;

        public ServicesService(HttpClient httpClient, ILogger<ServicesService> logger)
            : base(httpClient, logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> GetServicesCategories()
        {
           try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "api/service/getallcategories");
                var response = await _httpClient.SendAsync(request);
                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetServicesCategories");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        } 
    }
}
