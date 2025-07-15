using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Interfaces;
using MudBlazor;

namespace QLN.ContentBO.WebUI.Pages
{
    public class FeaturedEventSlotsBase : QLComponentBase
    {
        [Parameter] public List<FeaturedSlot> FeaturedEventSlots { get; set; }
        [Parameter] public List<EventCategoryModel> Categories { get; set; }
        [Parameter] public EventCallback<FeaturedSlot> ReplaceSlot { get; set; }
        [Parameter] public EventCallback<string> OnDelete { get; set; }
        [Inject] public ISnackbar Snackbar { get; set; } 
        [Parameter]
        public bool IsLoadingEvent { get; set; }
        [Inject] protected IJSRuntime JS { get; set; }
        [Inject] protected IEventsService EventsService { get; set; }
        [Inject] protected ILogger<FeaturedEventSlotsBase> Logger { get; set; }

        private string UserId => CurrentUserId.ToString();
        [Inject] protected IDialogService DialogService { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await AuthorizedPage();
            await base.OnInitializedAsync();
        }
        private bool shouldInitializeSortable = false;

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();

            // Only trigger JS if data is loaded and has events
            if (FeaturedEventSlots != null && FeaturedEventSlots.Any(s => s.Event != null))
            {
                shouldInitializeSortable = true;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (shouldInitializeSortable)
            {
                await JS.InvokeVoidAsync("initializeSortable", ".featured-table", DotNetObjectReference.Create(this));
                shouldInitializeSortable = false;
            }
        }

        [JSInvokable]
        public async Task OnTableReordered(List<string> newOrder)
        {
            try
            {
                var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true };
                var dialog = await DialogService.ShowAsync<ReOrderConfirmDialog>("", options);
                var result = await dialog.Result;
                if (result is not null)
                {
                    if (result.Canceled)
                    {
                        // Reset the List
                        await ResetOrder();
                    }
                    if (!result.Canceled)
                    {
                        var newSlotOrder = newOrder.Select(int.Parse).ToList();

                        var eventMap = FeaturedEventSlots
                            .Where(s => s.Event != null)
                            .ToDictionary(s => s.SlotNumber, s => s.Event.Id);

                        var slotAssignments = newSlotOrder.Select((slotNumber, index) => new
                        {
                            slotNumber = index + 1,
                            eventId = eventMap.TryGetValue(slotNumber, out var eventId) && eventId != Guid.Empty
                            ? (Guid?)eventId
                            : null
                        }).ToList();

                        var response = await EventsService.ReorderFeaturedSlots(slotAssignments, UserId);
                        if (response.IsSuccessStatusCode)
                        {
                            Snackbar.Add("Slot reordered successfully", Severity.Success);
                        }

                        if (!response.IsSuccessStatusCode)
                        {
                            Snackbar.Add("Failed to reorder slots", Severity.Error);
                            Logger.LogError("Reorder API failed: {StatusCode}", response.StatusCode);
                        }
                        StateHasChanged();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnTableReordered");
            }
        }

        protected async Task ResetOrder()
        {
            try
            {
                await JS.InvokeVoidAsync("resetTableOrder");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "ResetOrder");
            }
        }
    }
}
