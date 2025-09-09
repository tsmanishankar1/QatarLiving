using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using MudBlazor;
using QLN.ContentBO.WebUI.Handlers;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Services;
using System.Security.Claims;

namespace QLN.ContentBO.WebUI.Components
{
    public class QLComponentBase : ComponentBase
    {
        [Inject] public CookieAuthStateProvider CookieAuthenticationStateProvider { get; set; } = default!;
        [Inject] public NavigationManager NavManager { get; set; }
        [Inject] public ISnackbar Snackbar { get; set; } = default!;
        [Inject] public IOptions<NavigationPath> NavigationPath { get; set; } = default!;
        [Inject] public ILogger<QLComponentBase> Logger { get; set; } = default!;
        [Inject] public IFileUploadService FileUploadService { get; set; } = default!;
        [Inject] public ISubscriptionService SubscriptionService { get; set; } = default!;

        public string CurrentUserName { get; set; } = string.Empty;
        public string CurrentUserEmail { get; set; } = string.Empty;
        public string CurrentUserAlias { get; set; } = string.Empty;
        public bool IsLoggedIn { get; set; } = false;
        public int CurrentUserId { get; set; }

        public string ArticleDetailBaseURL { get; set; } = string.Empty;
        public string EventDetailBaseURL { get; set; } = string.Empty;
        public string PostDetailBaseURL { get; set; } = string.Empty;

        public string ClassifiedsBlobContainerName => NavigationPath.Value.ClassifiedsBlobContainerName;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                SetContentWebURl();
                if (IsLoggedIn)
                {
                    return;
                }

                var authState = await CookieAuthenticationStateProvider.GetAuthenticationStateAsync();

                var user = authState.User;
                if (user.Identity != null && user.Identity.IsAuthenticated)
                {
                    CurrentUserName = user.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
                    CurrentUserEmail = user.FindFirst(ClaimTypes.Email)?.Value ?? user.FindFirst("email")?.Value ?? string.Empty;
                    CurrentUserAlias = user.FindFirst("alias")?.Value ?? string.Empty;
                    CurrentUserId = int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : 0;
                    IsLoggedIn = true;
                }
                else
                {
                    IsLoggedIn = false;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnInitializedAsync");
            }
            finally
            {
                IsLoggedIn = false;
            }
        }

        protected void SetContentWebURl()
        {
            ArticleDetailBaseURL = $"{NavigationPath.Value.ContentNewsDetail}";
            EventDetailBaseURL = $"{NavigationPath.Value.ContentEventDetail}";
            PostDetailBaseURL = $"{NavigationPath.Value.ContentPostDetail}";
        }


        protected async Task<List<Subscription>> GetSubscriptionProductsAsync(int? vertical = null, int? subvertical = null, int? productType = null)
        {
            try
            {
                var apiResponse = await SubscriptionService.GetAllSubscriptionProducts(vertical, subvertical, productType);

                if (apiResponse is not null)
                {
                    if (apiResponse.IsSuccessStatusCode)
                    {
                        var subscriptions = await apiResponse.Content.ReadFromJsonAsync<List<Subscription>>();

                        return subscriptions ?? [];
                    }
                }

                return [];
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetSubscriptionProductsAsync");
                return [];
            }
        }
    }
}
