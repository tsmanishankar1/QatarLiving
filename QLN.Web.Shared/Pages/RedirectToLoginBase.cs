using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services;
using System.Security.Claims;

namespace QLN.Web.Shared.Pages
{
    public class RedirectToLoginBase : ComponentBase
    {
        [Inject] private CookieAuthStateProvider CookieAuthenticationStateProvider { get; set; }
        [Inject] NavigationManager NavManager { get; set; }
        [Inject] IOptions<NavigationPath> navigationPath { get; set; }

        private ClaimsPrincipal user;

        protected async override Task OnInitializedAsync()
        {
            var authState = await CookieAuthenticationStateProvider.GetAuthenticationStateAsync();

            if (authState != null)
            {
                user = authState.User;
            }

            var destination = GetDestination();

            if (user.Identity.IsAuthenticated)
            {
                NavManager.NavigateTo($"{navigationPath.Value.Login}?destination={destination}", forceLoad: true);
            }
        }

        private string GetDestination()
        {
            var destination = new Uri(NavManager.Uri).AbsolutePath.Substring(1);

            if (string.IsNullOrEmpty(destination))
            {
                return "content/daily";
            }

            return destination;
        }
    }
}
