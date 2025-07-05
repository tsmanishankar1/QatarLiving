using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using MudBlazor;
using Microsoft.AspNetCore.Components.Routing;
using QLN.ContentBO.WebUI.Pages.EventCreateForm.MessageBox;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Components;
using System.Text.Json;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using QLN.ContentBO.WebUI.Components.PaginationFooter;

namespace QLN.ContentBO.WebUI.Pages.EventsPage
{
    public class AllEventsListBase : QLComponentBase
    {
        [Parameter] public List<EventDTO> Events { get; set; }
        [Parameter] public List<EventCategoryModel> Categories { get; set; }
        [Parameter] public PaginatedEventResponse PaginatedData { get; set; }
        [Parameter] public EventCallback<bool> OnSortOrderChanged { get; set; }
        [Parameter] public EventCallback<string> OnDelete { get; set; }
        [Parameter] public EventCallback AddEventCallback { get; set; }
        [Inject] protected NavigationManager Navigation { get; set; }
        [Parameter] public int CurrentPage { get; set; }
        [Parameter] public int PageSize { get; set; }
        [Parameter]
        public bool IsLoading { get; set; }
        [Parameter] public EventCallback<int> OnPageChange { get; set; }
        [Parameter] public EventCallback<int> OnPageSizeChange { get; set; }

        protected string SearchText { get; set; } = string.Empty;
        protected bool SortAscending { get; set; } = true;

        protected async Task HandlePageChange(int newPage)
        {
            await OnPageChange.InvokeAsync(newPage);
        }

        protected async Task HandlePageSizeChange(int newSize)
        {
            await OnPageSizeChange.InvokeAsync(newSize);
        }

        protected Task HandleSearch(string value)
        {
            SearchText = value;
            return Task.CompletedTask;
        }

        protected async Task HandleSortToggle(bool ascending)
        {
            SortAscending = ascending;
            await OnSortOrderChanged.InvokeAsync(ascending);
        }
        protected string selectedTab = "published";

        protected List<ToggleTabs.TabOption> tabOptions = new()
    {
        new() { Label = "Published", Value = "published" },
        new() { Label = "Unpublished", Value = "unpublished" },
        new() { Label = "Expired", Value = "expired" }
    };

        [Parameter]
        public EventCallback<int?> OnStatusChanged { get; set; }  // New param to send status

        protected async Task OnTabChanged(string newTab)
        {
            selectedTab = newTab;

            int? status = newTab switch
            {
                "published" => 1,
                "unpublished" => 2,
                "expired" => 3,
                _ => null
            };

            await OnStatusChanged.InvokeAsync(status);  // Notify parent of new status
        }
        protected string GetEmptyTitle()
        {
            return selectedTab switch
            {
                "published" => "No published events found",
                "unpublished" => "No unpublished events found",
                "expired" => "No expired events found",
                _ => "No events found"
            };
        }

        protected void NavigateToEditPage(Guid id)
        {
            Navigation.NavigateTo($"/content/events/edit/{id}");
        }

    }
}