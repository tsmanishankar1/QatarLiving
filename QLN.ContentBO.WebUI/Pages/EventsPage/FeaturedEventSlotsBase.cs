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

        // Replace with actual user ID retrieval logic (e.g. from auth claims)
        private string UserId => CurrentUserId.ToString();

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            AuthorizedPage(); // Ensure this is called to populate user info
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await JS.InvokeVoidAsync("initializeSortable", ".featured-table", DotNetObjectReference.Create(this));
            }
        }

        [JSInvokable]
        public async Task OnTableReordered(List<string> newOrder)
        {
            var newSignature = string.Join(",", newOrder);
            Logger.LogInformation("New slot order received: {Order}", newSignature);

            // Compare only once
            var currentOrder = FeaturedEventSlots.OrderBy(s => s.SlotNumber).Select(s => s.SlotNumber.ToString()).ToList();
            if (newSignature == string.Join(",", currentOrder))
            {
                Logger.LogInformation("Same order as before, skipping API call.");
                return;
            }

            // Find the first difference (simple diff)
            for (int i = 0; i < newOrder.Count; i++)
            {
                var newSlotId = int.Parse(newOrder[i]);
                var expectedSlotId = FeaturedEventSlots[i].SlotNumber;

                if (newSlotId != expectedSlotId)
                {
                    int fromSlot = FeaturedEventSlots.First(s => s.SlotNumber == newSlotId).SlotNumber;
                    int toSlot = i + 1;

                    Logger.LogInformation("Calling reorder API: fromSlot={From} toSlot={To} userId={UserId}", fromSlot, toSlot, UserId);
                    var response = await EventsService.ReorderFeaturedSlots(fromSlot, toSlot, UserId);

                    if (response.IsSuccessStatusCode)
                    {
                        Logger.LogInformation("Successfully reordered slot from {From} to {To}", fromSlot, toSlot);
                        Snackbar.Add($"Slot reordered from {fromSlot} to {toSlot}.", Severity.Success);
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        Logger.LogError("Reorder failed: {Status} - {Error}", response.StatusCode, error);
                         Snackbar.Add("Failed to reorder slot. Try again.", Severity.Error);
                    }

                    break; // Only process the first change
                }
            }

            // ✅ Refresh list from backend (optional but safer)
            StateHasChanged();
        }
    }
}
