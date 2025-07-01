using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using MudBlazor;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services;

namespace QLN.Web.Shared.Components
{
    public class QLComponentBase : ComponentBase
    {
        [Inject] public CookieAuthStateProvider CookieAuthenticationStateProvider { get; set; } = default!;
        [Inject] public NavigationManager NavManager { get; set; } = default!;
        [Inject] public IOptions<NavigationPath> navigationPath { get; set; } = default!;
        [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] public ISnackbar Snackbar { get; set; } = default!;

        protected async void AuthorizedPage()
        {
            var authState = await CookieAuthenticationStateProvider.GetAuthenticationStateAsync();
            var destination = SetDestination();

            if (authState != null && !authState.User.Identity.IsAuthenticated)
            {
                NavManager.NavigateTo($"{navigationPath.Value.Login}?destination={destination}", forceLoad: true);
            }
        }

        protected virtual string SetDestination()
        {
            var destination = new Uri(NavManager.Uri).AbsolutePath.Substring(1);

            return destination;
        }
    }
}
