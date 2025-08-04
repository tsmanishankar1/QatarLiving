using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Services.Base;
using System.Net;
using System.Text;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Services
{
    public class DrupalUserService : ServiceBase<DrupalUserService>, IDrupalUserService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DrupalUserService> _logger;

        public DrupalUserService(HttpClient httpClient, ILogger<DrupalUserService> logger)
            : base(httpClient, logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }
        public async Task<List<HttpResponseMessage>> SearchDrupalUsersAsync(string searchText)
        {
            var responses = new List<HttpResponseMessage>();

            try
            {
                var endpoint = $"/auth/user/autocomplete/{searchText}";
                var response = await _httpClient.PostAsync(endpoint, null);
                responses.Add(response);
                return responses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SearchDrupalUsersAsync");
                responses.Add(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
                return responses;
            }
        }
       
    }
}
