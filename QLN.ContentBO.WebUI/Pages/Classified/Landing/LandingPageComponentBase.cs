using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Components;
using static QLN.ContentBO.WebUI.Models.ClassifiedLanding;

namespace QLN.ContentBO.WebUI.Pages.Classified.Landing
{
    public class LandingPageComponentBase : QLComponentBase
    {
        // Parameters exactly matching parent component usage
        [Parameter]
        public bool IsLoading { get; set; }

        [Parameter]
        public List<LandingPageItem> Items { get; set; } = new();

        [Parameter]
        public LandingPageItemType ItemType { get; set; }

        [Parameter]
        public EventCallback<LandingPageItem> ReplaceItem { get; set; }

        [Parameter]
        public EventCallback<string> OnDelete { get; set; }

        // Services and injections
        [Inject] public ISnackbar Snackbar { get; set; }
        [Inject] protected IJSRuntime JS { get; set; }
        [Inject] protected IEventsService EventsService { get; set; }
        [Inject] protected ILogger<LandingPageComponentBase> Logger { get; set; }

        // Private fields
        private string UserId => CurrentUserId.ToString();
        private bool shouldInitializeSortable = false;
        private List<Slot> featuredEventSlots = new();


        protected override async Task OnInitializedAsync()
        {
            await AuthorizedPage();
            await base.OnInitializedAsync();

            if (Items != null && Items.Any())
            {
                featuredEventSlots = Items.Select((item, index) => new Slot
                {
                    SlotNumber = index + 1,
                    Event = item
                }).ToList();
            }
        }

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();

            if (Items != null && Items.Any())
            {
                shouldInitializeSortable = true;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (shouldInitializeSortable)
            {
                await JS.InvokeVoidAsync("initializeSortable", ".classified-table", DotNetObjectReference.Create(this));
                shouldInitializeSortable = false;
            }
        }

        [JSInvokable]
        public async Task OnTableReordered(List<string> newOrder)
        {
            try
            {
                var newSlotOrder = newOrder.Select(int.Parse).ToList();
                var updatedItems = new List<LandingPageItem>();

                // Reconstruct items in new order
                foreach (var itemIndex in newSlotOrder)
                {
                    if (itemIndex > 0 && itemIndex <= Items.Count)
                    {
                        updatedItems.Add(Items[itemIndex - 1]);
                    }
                }

                // Update our internal slots representation
                featuredEventSlots = updatedItems.Select((item, index) => new Slot
                {
                    SlotNumber = index + 1,
                    Event = item
                }).ToList();

                var slotAssignments = featuredEventSlots.Select(slot => new
                {
                    slotNumber = slot.SlotNumber,
                    eventId = slot.Event?.Id ?? Guid.Empty
                }).ToList();

                //var response = await EventsService.ReorderFeaturedSlots(slotAssignments, UserId);

                //if (response.IsSuccessStatusCode)
                //{
                //    Snackbar.Add("Items reordered successfully.", Severity.Success);
                //    // Update parent's Items collection through OnReplace callback
                //    foreach (var item in updatedItems.Select((value, index) => new { value, index }))
                //    {
                //        await OnReplace.InvokeAsync(item.value);
                //    }
                //}
                //else
                //{
                //    Snackbar.Add("Failed to reorder items", Severity.Error);
                //    Logger.LogError("Reorder API failed: {StatusCode}", response.StatusCode);
                //}

                Snackbar.Add("Items reordered successfully.", Severity.Success);

            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during table reordering");
                Snackbar.Add("Failed to reorder items", Severity.Error);
            }
        }

        // UI Helper methods
        protected string GetTableTitle()
        {
            return ItemType switch
            {
                LandingPageItemType.FeaturedCategory => "Featured Categories",
                LandingPageItemType.SeasonalPick => "Seasonal Picks",
                LandingPageItemType.FeaturedStore => "Featured Stores",
                _ => "Items"
            };
        }

        protected string GetItemTypeName()
        {
            return ItemType switch
            {
                LandingPageItemType.FeaturedCategory => "Featured Category",
                LandingPageItemType.SeasonalPick => "Seasonal Pick",
                LandingPageItemType.FeaturedStore => "Featured Store",
                _ => "Item"
            };
        }
        protected string EmptyCardTitle => $"No {GetItemTypeName()} found";

    }
}