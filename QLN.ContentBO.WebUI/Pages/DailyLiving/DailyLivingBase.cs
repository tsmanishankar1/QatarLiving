using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Pages.DailyLiving.Components.RadioAutoCompleteDialog;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Pages.EventCreateForm.MessageBox;
using System.Text.Json;

public class DailyLivingBase : QLComponentBase
{
    protected int activeIndex = 0;
    protected List<DailyLivingArticleDto> articles = new();
    protected bool isLoading = false;
    protected bool IsLoadingEvent = true;
    [Inject] IEventsService eventsService { get; set; }
    protected DailyLivingTab SelectedTab => (DailyLivingTab)activeIndex;
    public List<EventDTO> AllEventsList { get; set; } = new();
    protected EventDTO ReplacedEvent { get; set; } = new();
    protected List<EventCategoryModel> Categories = [];
    protected List<FeaturedSlot> featuredEventSlots = [];
    protected List<DailyTopic> ActiveTopics = [];
    protected FeaturedSlot ReplaceSlot { get; set; } = new();

    [Inject] public IDailyLivingService DailyService { get; set; }
    [Inject] public IDialogService DialogService { get; set; }
    [Inject] public ILogger<DailyLivingBase> Logger { get; set; }
    protected List<FeaturedSlot> FeaturedEventSlots { get; set; } = new();
    protected bool IsLoading { get; set; } = false;

    protected async Task ReplaceSlotHandler(FeaturedSlot slot)
{
    // your logic
}

    protected async Task DeleteSlotHandler(string id)
    {
        // your logic
    }
protected async Task UpdateHandler(FeaturedSlot slot)
{
    // your logic
}

    protected async Task AddItemtHandler(DailyLivingArticleDto item)
    {
       await OpenDialogAsync();
    }
    protected async Task DeleteHandler(string id)
    {
        // your logic
    }
protected async Task RenameHandler(FeaturedSlot slot)
{
    // your logic
}


    protected override async Task OnInitializedAsync()
    {
        await LoadArticlesAsync();
        featuredEventSlots = await GetFeaturedSlotsAsync();
        Categories = await GetEventsCategories();
        AllEventsList = await GetAllEvents();
        ActiveTopics = await GetActiveTopics();
    }

    protected async Task OnTabChanged(int newIndex)
    {
        if (newIndex == activeIndex)
            return;

        activeIndex = newIndex;
        await LoadArticlesAsync();
    }


    private async Task LoadArticlesAsync()
{
    isLoading = true;
    StateHasChanged();

    try
    {
        articles = new();

        switch (SelectedTab)
        {
            case DailyLivingTab.TopSection:
                articles = await DailyService.GetTopSectionAsync();
                break;

            case DailyLivingTab.FeaturedEvents:
                    featuredEventSlots = await GetFeaturedSlotsAsync();
                break;

            case DailyLivingTab.EverythingQatar:
            case DailyLivingTab.Lifestyle:
            case DailyLivingTab.SportsNews:
            case DailyLivingTab.QLExclusive:
            case DailyLivingTab.AdviceHelp:
                {
                    var topicName = GetTopicNameFromTab(SelectedTab);
                    var topic = ActiveTopics.FirstOrDefault(t => t.topicName.Equals(topicName, StringComparison.OrdinalIgnoreCase));

                    if (topic is not null)
                    {
                        var topicArticles = await DailyService.GetContentByTopicIdAsync(topic.Id);
                        if (topicArticles?.Any() == true)
                        {
                            articles = topicArticles;
                        }
                    }
                    break;
                }

            default:
                articles = new();
                break;
        }
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Failed to load articles for tab {Tab}", SelectedTab);
        articles = new();
    }

    isLoading = false;
    StateHasChanged();
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
    private string GetTopicNameFromTab(DailyLivingTab tab)
{
    return tab switch
    {
        DailyLivingTab.EverythingQatar => "Everything Qatar",
        DailyLivingTab.Lifestyle => "Lifestyle",
        DailyLivingTab.SportsNews => "Sports News",
        DailyLivingTab.QLExclusive => "QL Exclusive",
        DailyLivingTab.AdviceHelp => "Advice & Help",
        _ => string.Empty
    };
}
    protected async Task ReplaceEventSlot(FeaturedSlot selectedEvent)
    {
        ReplaceSlot = selectedEvent;
        OpenDReplaceDialogAsync();
        await Task.CompletedTask;
    }
    protected Task OpenDReplaceDialogAsync()
    {
        var parameters = new DialogParameters
            {
                { nameof(MessageBoxBase.Title), "Featured Event" },
                { nameof(MessageBoxBase.Placeholder), "Article Title*" },
                { nameof(MessageBoxBase.events), AllEventsList },
                { nameof(MessageBoxBase.OnAdd), EventCallback.Factory.Create<FeaturedSlot>(this, HandleEventSelected) }
            };
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Small,
            FullWidth = true,
            CloseOnEscapeKey = true
        };
        return DialogService.ShowAsync<MessageBox>("", parameters, options);
    }
    protected async Task HandleEventSelected(FeaturedSlot selectedEvent)
    {
        if (ReplaceSlot?.SlotNumber > 0 && ReplaceSlot?.SlotNumber != null)
        {
            var targetSlot = featuredEventSlots.FirstOrDefault(s => s.SlotNumber == ReplaceSlot?.SlotNumber);
            if (targetSlot != null)
            {
                targetSlot.Event = selectedEvent.Event;
                ReplacedEvent = selectedEvent.Event;
                ReplacedEvent.FeaturedSlot.Id = ReplaceSlot?.SlotNumber ?? 0;
                ReplacedEvent.IsFeatured = true;
                ReplaceFeaturedEvent();
            }
        }
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
    protected async Task ReplaceFeaturedEvent()
    {
        try
        {
            var payload = new
            {
                eventId = ReplacedEvent.Id,
                isFeatured = true,
                slot = new
                {
                    id = ReplacedEvent.FeaturedSlot.Id,
                    name = ReplacedEvent.FeaturedSlot.Name
                }
            };

            var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            Logger.LogInformation("New slot replacement payload: {Payload}", payloadJson);

            var apiResponse = await eventsService.UpdateFeaturedEvents(payload);
            if (apiResponse.IsSuccessStatusCode)
            {
                Snackbar.Add("Event Replaced successfully", Severity.Success);
                StateHasChanged();
            }
            else
            {
                var error = await apiResponse.Content.ReadAsStringAsync();
                Logger.LogError("Failed to replace event. StatusCode: {StatusCode}, Response: {Response}", apiResponse.StatusCode, error);
                Snackbar.Add("Failed to replace event", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "ReplaceFeaturedEvent");
            Snackbar.Add("Something went wrong while replacing the event.", Severity.Error);
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
    private async Task<List<EventDTO>> GetAllEvents()
    {
        try
        {
            var apiResponse = await eventsService.GetAllEvents();
            if (apiResponse.IsSuccessStatusCode)
            {
                var response = await apiResponse.Content.ReadFromJsonAsync<List<EventDTO>>();
                var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                return response ?? new List<EventDTO>();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "GetEvents");
        }
        return new List<EventDTO>();
    }
    private async Task<List<DailyTopic>> GetActiveTopics()
    {
        try
        {
            var topics = await DailyService.GetActiveTopicsAsync();
            return topics ?? []; 
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in GetActiveTopics");
            return [];
        }   
    }
}

