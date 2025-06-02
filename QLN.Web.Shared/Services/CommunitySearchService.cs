using Microsoft.AspNetCore.Components;
using QLN.Web.Shared.Contracts;

namespace QLN.Web.Shared.Services
{
    public class CommunitySearchService : ServiceBase<PostDialogService>, ISearchService
    {
        private readonly HttpClient _httpClient;
        private readonly NavigationManager _navigationManager;

        public CommunitySearchService(HttpClient httpClient, NavigationManager navigationManager) : base(httpClient)
        {
            _httpClient = httpClient;
            _navigationManager = navigationManager;

        }
        public async Task<bool> PerformSearchAsync(string searchText)
        {
            try
            {
                var baseUrl = _httpClient.BaseAddress?.ToString()?.TrimEnd('/');


                _navigationManager.NavigateTo($"{baseUrl}/custom-search/google?name={searchText}/#gsc.tab=0&gsc.q=test&gsc.sort=", forceLoad: true);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}

