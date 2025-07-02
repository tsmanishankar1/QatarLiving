using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using MudBlazor;
using QLN.ContentBO.WebUI.Handlers;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Components
{
    public class QLComponentBase : ComponentBase
    {
        [Inject] public CookieAuthStateProvider CookieAuthenticationStateProvider { get; set; } = default!;
        [Inject] public NavigationManager NavManager { get; set; } = default!;
        [Inject] public ISnackbar Snackbar { get; set; } = default!;
        [Inject] public IOptions<NavigationPath> NavigationPath { get; set; } = default!;

        protected async void AuthorizedPage()
        {
            var authState = await CookieAuthenticationStateProvider.GetAuthenticationStateAsync();
            var destination = SetDestination();

            if (authState != null && !authState.User.Identity.IsAuthenticated)
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
