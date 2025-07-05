using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using MudBlazor;
using Microsoft.AspNetCore.Components.Routing;
using QLN.ContentBO.WebUI.Pages.EventCreateForm.MessageBox;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Components;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Pages.EventsPage
{
    public class AllEventsListBase : QLComponentBase
    {
        [Parameter] public List<EventDTO> Events { get; set; }
        [Parameter] public List<EventCategoryModel> Categories { get; set; }
        [Parameter] public EventCallback<string> OnDelete { get; set; }
        [Parameter] public EventCallback AddEventCallback { get; set; }
        [Inject] protected NavigationManager Navigation { get; set; }
        protected string SearchText { get; set; } = string.Empty;
        protected void NavigateToEditPage(Guid id)
        {
            Navigation.NavigateTo($"/content/events/edit/{id}");
        }
    }
}