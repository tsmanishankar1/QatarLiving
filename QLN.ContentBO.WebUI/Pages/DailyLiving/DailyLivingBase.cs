using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudExRichTextEditor;
using QLN.Common.Infrastructure.DTO_s;
using MudBlazor;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Pages.EventsPage;
using QLN.ContentBO.WebUI.Pages.DailyLiving.Components.RadioAutoCompleteDialog;
namespace QLN.ContentBO.WebUI.Pages
{
    public class DailyLivingBase : QLComponentBase
    {
        protected int activeIndex = 0;
        [Inject]
        public IDialogService DialogService { get; set; }
        [Inject] ILogger<EventCreateFormBase> Logger { get; set; }
        [Inject] IEventsService eventsService { get; set; }
        protected bool IsLoadingEvent = true;
        protected List<EventCategoryModel> Categories = [];
        protected List<FeaturedSlot> featuredEventSlots = [];
        protected FeaturedSlot ReplaceSlot { get; set; } = new();
        protected override async Task OnInitializedAsync()
        {
            featuredEventSlots = await GetFeaturedSlotsAsync();
            Categories = await GetEventsCategories();
        }
        protected Task OpenDialogAsync()
        {
            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                CloseOnEscapeKey = true
            };
            return DialogService.ShowAsync<RadioAutoCompleteDialog>(string.Empty, options);
        }
        protected async Task ReplaceEventSlot(FeaturedSlot selectedEvent)
        {
            ReplaceSlot = selectedEvent;
            OpenDialogAsync();
            await Task.CompletedTask;
        }
        private async Task<List<EventCategoryModel>> GetEventsCategories()
        {
            try
            {
                var apiResponse = await eventsService.GetEventCategories();
                if (apiResponse.IsSuccessStatusCode)
                {
                    return await apiResponse.Content.ReadFromJsonAsync<List<EventCategoryModel>>() ?? [];
                }
                return [];
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetEventsCategories");
                return [];
            }
        }
        private async Task<List<FeaturedSlot>> GetFeaturedSlotsAsync()
        {
            IsLoadingEvent = true;
            var slots = Enumerable.Range(1, 6)
                .Select(i => new FeaturedSlot
                {
                    SlotNumber = i,
                    Event = new EventDTO
                    {
                        EventTitle = "Feature an Event"
                    }
                }).ToList();

            try
            {
                var apiResponse = await eventsService.GetFeaturedEvents();
                if (apiResponse.IsSuccessStatusCode)
                {
                    var events = await apiResponse.Content.ReadFromJsonAsync<List<EventDTO>>() ?? new();
                    var rawContent = await apiResponse.Content.ReadAsStringAsync();
                    foreach (var ev in events)
                    {
                        if (ev.FeaturedSlot != null && ev.FeaturedSlot.Id >= 1 && ev.FeaturedSlot.Id <= 6)
                        {
                            var index = ev.FeaturedSlot.Id - 1;
                            slots[index] = new FeaturedSlot
                            {
                                SlotNumber = ev.FeaturedSlot.Id,
                                Event = ev
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while fetching featured slots");
            }
            finally
            {
                IsLoadingEvent = false;
            }
            return slots;
        }
        protected async Task DeleteFeatureEvent(string eventId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(eventId))
                {
                    Snackbar.Add("Invalid event ID.", Severity.Warning);
                    return;
                }
                var slot = featuredEventSlots.FirstOrDefault(s => s.Event?.Id.ToString() == eventId);
                if (slot == null)
                {
                    Snackbar.Add("No featured event found with the given ID.", Severity.Warning);
                    return;
                }
                var apiResponse = await eventsService.DeleteEvent(eventId);
                if (apiResponse.IsSuccessStatusCode)
                {
                    slot.Event = new EventDTO
                    {
                        EventTitle = "Feature an Event"
                    };
                    Snackbar.Add("Featured event deleted successfully", Severity.Success);
                    featuredEventSlots = await GetFeaturedSlotsAsync();
                }
                else
                {
                    Snackbar.Add("Failed to delete featured event.", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "DeleteFeatureEvent");
                Snackbar.Add("Something went wrong while deleting the featured event.", Severity.Error);
            }
        }

    }
}