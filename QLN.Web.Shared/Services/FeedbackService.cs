using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using QLN.Web.Shared.Models.FeedbackRequest;
using Microsoft.Extensions.Options;

namespace QLN.Web.Shared.Services
{
    public class FeedbackService
    {
        private readonly HttpClient _httpClient;
        private readonly string _endpoint;

        public FeedbackService(HttpClient httpClient, IOptions<FeedbackApiOptions> options)
        {
            _httpClient = httpClient;
            _endpoint = options.Value.Endpoint;
        }

        public async Task<bool> SubmitFeedbackAsync(FeedbackFormModel model, string userId)
        {
            var payload = new
            {
                ntx_userid = userId,
                customerName = model.Name,
                emailaddress = model.Email,
                mobilePhone = model.Mobile.Replace("+974", "").Trim(),
                ntx_casecategory = model.Category,
                description = model.Description
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(_endpoint, payload);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}