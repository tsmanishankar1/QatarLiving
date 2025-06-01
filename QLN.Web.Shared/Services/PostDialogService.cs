using QLN.Web.Shared.Contracts;
using System.Net.Http;

namespace QLN.Web.Shared.Services
{
    public class PostDialogService : ServiceBase<PostDialogService>, IPostDialogService
    {
        private readonly HttpClient _httpClient;

        public PostDialogService(HttpClient httpClient) : base(httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<bool> PostSelectedCategoryAsync(string selectedCategoryId)
        {
            try
            {

                var response = await _httpClient.GetAsync($"node/add/post?field_page={selectedCategoryId}");


                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API Error in PostSelectedCategoryAsync: {ex.Message}");
                return false;
            }
        }
    }
}
