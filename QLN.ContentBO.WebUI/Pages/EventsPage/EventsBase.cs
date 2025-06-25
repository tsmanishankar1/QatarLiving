using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using MudBlazor;
using Microsoft.AspNetCore.Components.Routing;

namespace QLN.ContentBO.WebUI.Pages.EventsPage
{
    public class EventsBase : ComponentBase
    {
        [Inject]
        protected NavigationManager Navigation { get; set; }
        protected int activeIndex = 0;
        protected string searchText;
        protected string selectedType;
        protected List<string> categories = new List<string> { "All Events", "Featured Events" };
        protected void NavigateToAddEvent()
        {
            Navigation.NavigateTo("/content/events/create");
        }
    }
}