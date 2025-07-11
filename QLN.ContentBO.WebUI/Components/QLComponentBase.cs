using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using MudBlazor;
using QLN.ContentBO.WebUI.Handlers;
using QLN.ContentBO.WebUI.Models;
using System.Security.Claims;

namespace QLN.ContentBO.WebUI.Components
{
    public class QLComponentBase : ComponentBase
    {
        [Inject] public CookieAuthStateProvider CookieAuthenticationStateProvider { get; set; } = default!;
        [Inject] public NavigationManager NavManager { get; set; } = default!;
        [Inject] public ISnackbar Snackbar { get; set; } = default!;
        [Inject] public IOptions<NavigationPath> NavigationPath { get; set; } = default!;
        [Inject] public ILogger<QLComponentBase> Logger { get; set; } = default!;

        public string CurrentUserName { get; set; } = string.Empty;
        public string CurrentUserEmail { get; set; } = string.Empty;
        public string CurrentUserAlias { get; set; } = string.Empty;
        public bool IsLoggedIn { get; set; } = false;
        public int CurrentUserId { get; set; }

        public string ArticleDetailBaseURL { get; set; } = string.Empty;
        public string EventDetailBaseURL { get; set; } = string.Empty;
        public string PostDetailBaseURL { get; set; } = string.Empty;

        protected async Task AuthorizedPage()
        {
            try
            {
                var authState = await CookieAuthenticationStateProvider.GetAuthenticationStateAsync();
                var destination = SetDestination();

                var user = authState.User;
                if (user.Identity != null && user.Identity.IsAuthenticated)
                {
                    CurrentUserName = user.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
                    CurrentUserEmail = user.FindFirst(ClaimTypes.Email)?.Value ?? user.FindFirst("email")?.Value ?? string.Empty;
                    CurrentUserAlias = user.FindFirst("alias")?.Value ?? string.Empty;
                    CurrentUserId = int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : 0;
                    IsLoggedIn = true;
                    SetContentWebURl();
                }
                else
                {
                    IsLoggedIn = false;
                    NavManager.NavigateTo($"{NavigationPath.Value.Login}?destination={NavigationPath.Value.BORedirectPrefix}{destination}", forceLoad: true);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "AuthorizedPage");
                throw;
            }
        }

        protected virtual string SetDestination()
        {
            var destination = new Uri(NavManager.Uri).AbsolutePath.Substring(1);

            return destination;
        }

        protected void SetContentWebURl()
        {
            ArticleDetailBaseURL = $"{NavigationPath.Value.ContentNewsDetail}";
            EventDetailBaseURL = $"{NavigationPath.Value.ContentEventDetail}";
            PostDetailBaseURL = $"{NavigationPath.Value.ContentPostDetail}";
        }
    }
}
