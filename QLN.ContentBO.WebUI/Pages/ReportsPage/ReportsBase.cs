using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
namespace QLN.ContentBO.WebUI.Pages.ReportsPage
{
    public class ReportsBase : ComponentBase
    {
        [Inject]
        protected NavigationManager Navigation { get; set; }
        protected string searchText;
        protected string? Type;
        protected override void OnInitialized()
        {
            var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("type", out var param))
            {
                Type = param;
            }
        }
    };    
}
