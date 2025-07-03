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

        public string CurrentUserName { get; set; } = string.Empty;
        public string CurrentUserEmail { get; set; } = string.Empty;
        public string CurrentUserAlias { get; set; } = string.Empty;
        public bool IsLoggedIn { get; set; } = false;
        public int CurrentUserId { get; set; }

        protected async void AuthorizedPage()
        {
            var authState = await CookieAuthenticationStateProvider.GetAuthenticationStateAsync();
            var destination = SetDestination();

            var user = authState.User;
            if (user.Identity != null && user.Identity.IsAuthenticated)
            {
                CurrentUserName = user.FindFirst(ClaimTypes.Name)?.Value;
                CurrentUserEmail = user.FindFirst(ClaimTypes.Email)?.Value ?? user.FindFirst("email")?.Value;
                CurrentUserAlias = user.FindFirst("alias")?.Value;
                CurrentUserId = int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : 0;
                IsLoggedIn = true;
            }
            else
            {
                NavManager.NavigateTo($"{NavigationPath.Value.Login}?destination={destination}", forceLoad: true);
            }
        }

        protected virtual string SetDestination()
        {
            var destination = new Uri(NavManager.Uri).AbsolutePath.Substring(1);

            return destination;
        }
    }
}
