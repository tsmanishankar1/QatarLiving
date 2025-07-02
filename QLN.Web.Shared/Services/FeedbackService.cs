using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using QLN.Web.Shared.Models.FeedbackRequest;

namespace QLN.Web.Shared.Services
{
    public class FeedbackService
    {
        private readonly HttpClient _httpClient;

        private const string FeedbackEndpoint = "https://prod-27.northeurope.logic.azure.com/workflows/405f33f73c804aee99724a05edc4966c/triggers/manual/paths/invoke?api-version=2016-06-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig=iXxQgnbSdVqmUwK4iSXQLf922PFRzne8giklD21M3YE";

        public FeedbackService(HttpClient httpClient)
        {
            _httpClient = httpClient;
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
                var response = await _httpClient.PostAsJsonAsync(FeedbackEndpoint, payload);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}