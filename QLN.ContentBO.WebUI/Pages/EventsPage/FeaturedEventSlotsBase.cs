using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Interfaces;

namespace QLN.ContentBO.WebUI.Pages
{
    public class FeaturedEventSlotsBase : ComponentBase
    {
        [Parameter] public List<FeaturedSlot> FeaturedEventSlots { get; set; }
        [Parameter] public List<EventCategoryModel> Categories { get; set; }
        [Parameter] public EventCallback<FeaturedSlot> ReplaceSlot { get; set; }
        [Parameter] public EventCallback<string> OnDelete { get; set; }

        [Inject] protected IJSRuntime JS { get; set; }
        [Inject] protected IEventsService EventsService { get; set; }
        [Inject] protected ILogger<FeaturedEventSlotsBase> Logger { get; set; }

        // Replace with actual user ID retrieval logic (e.g. from auth claims)
        private string UserId => "USER-ID-HERE";

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
    Logger.LogInformation("New slot order received: {Order}", string.Join(", ", newOrder));

    // Build reordered list based on new slot positions
    var reordered = new List<FeaturedSlot>();
    foreach (var id in newOrder)
    {
        var slot = FeaturedEventSlots.FirstOrDefault(s => s.SlotNumber.ToString() == id);
        if (slot != null)
            reordered.Add(slot);
    }

    if (reordered.Any())
    {
        for (int newIndex = 0; newIndex < reordered.Count; newIndex++)
        {
            var slot = reordered[newIndex];
            var oldIndex = FeaturedEventSlots.FindIndex(s => s.SlotNumber == slot.SlotNumber);

            int fromSlot = oldIndex + 1;
            int toSlot = newIndex + 1;

            if (fromSlot != toSlot)
            {
                Logger.LogInformation("Calling reorder API: fromSlot={From} toSlot={To} userId={UserId}", fromSlot, toSlot, UserId);

                var response = await EventsService.ReorderFeaturedSlots(fromSlot, toSlot, UserId);

                if (response.IsSuccessStatusCode)
                {
                    Logger.LogInformation("Successfully reordered slot from {From} to {To}", fromSlot, toSlot);
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Logger.LogError("Failed reorder from {From} to {To}. Status: {Status}, Error: {Error}",
                        fromSlot, toSlot, response.StatusCode, error);
                }
            }

            slot.SlotNumber = toSlot; // Update slot number to reflect new order
        }

        FeaturedEventSlots = reordered;
        StateHasChanged();
    }
}

    }
}
