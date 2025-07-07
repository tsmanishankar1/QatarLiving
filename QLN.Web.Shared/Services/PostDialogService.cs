using Microsoft.AspNetCore.Components;
using QLN.Web.Shared.Contracts;

namespace QLN.Web.Shared.Services
{
    public class PostDialogService : ServiceBase<PostDialogService>, IPostDialogService
    {
        private readonly HttpClient _httpClient;
        private readonly NavigationManager _navigationManager;

        public PostDialogService(HttpClient httpClient, NavigationManager navigationManager) : base(httpClient)
        {
            _httpClient = httpClient;
            _navigationManager = navigationManager;

        }
        public async Task<bool> PostSelectedCategoryAsync(string selectedCategoryId)
        {
            try
            {
                var baseUrl = _httpClient.BaseAddress?.ToString()?.TrimEnd('/');

                //var response = await _httpClient.GetAsync($"node/add/post?field_page={selectedCategoryId}");

                _navigationManager.NavigateTo($"{baseUrl}/node/add/post?field_page={selectedCategoryId}", forceLoad: true);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API Error in PostSelectedCategoryAsync: {ex.Message}");
                return false;
            }
        }
    }
}
