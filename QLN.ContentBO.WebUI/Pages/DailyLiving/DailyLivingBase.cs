using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Pages.DailyLiving.Components.RadioAutoCompleteDialog;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Pages.DailyLiving.Components;
using QLN.ContentBO.WebUI.Pages.EventCreateForm.MessageBox;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using System.Text.Json;
using QLN.ContentBO.WebUI.Services;

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
    protected DailyLivingArticleDto ReplaceArticle { get; set; } = new();
    protected List<EventCategoryModel> Categories = [];
    protected List<FeaturedSlot> featuredEventSlots = [];
    protected List<DailyTopic> ActiveTopics = [];
    protected DailyTopic selectedTopic { get; set; } = new();
    protected FeaturedSlot ReplaceSlot { get; set; } = new();
    protected List<DailyLivingArticleDto> AvailableArticles = new();
    [Inject] public IDailyLivingService DailyService { get; set; }
    [Inject] public IDialogService DialogService { get; set; }
    [Inject] public ILogger<DailyLivingBase> Logger { get; set; }
    protected List<FeaturedSlot> FeaturedEventSlots { get; set; } = new();
    protected bool IsLoading { get; set; } = false;

    protected async Task ReplaceSlotHandler(FeaturedSlot slot)
    {
    }

    protected async Task DeleteSlotHandler(string id)
    {
        var parameters = new DialogParameters
        {
            { "Title", "Delete Confirmation" },
            { "Descrption", "Do you want to delete this Article?" },
            { "ButtonTitle", "Delete" },
            { "OnConfirmed",  EventCallback.Factory.Create(this, async () => await DeleteArticleAsync(id))}
        };
        var options = new DialogOptions { CloseButton = false, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = DialogService.Show<ConfirmationDialog>("", parameters, options);
        var result = await dialog.Result;
    }
    protected async Task UpdateHandler()
    {
        if (selectedTopic.isPublished)
        {
            await UnPublishArticle();
        }
        else
        {
            await PublishArticle();
        }

    }

    protected async Task AddItemtHandler(DailyLivingArticleDto item)
    {
        OpenRadioAutoCompleteDialog();
    }
    protected async Task ReplaceItem(DailyLivingArticleDto item)
    {
        var json = JsonSerializer.Serialize(item, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        ReplaceArticle = item;
        OpenRadioAutoCompleteDialog();
    }
    private async Task OpenRadioAutoCompleteDialog()
    {
        var parameters = new DialogParameters
    {
        { nameof(RadioAutoCompleteDialog.Title), "" },
        { nameof(RadioAutoCompleteDialog.articles), AvailableArticles },
        { nameof(RadioAutoCompleteDialog.OnAdd), EventCallback.Factory.Create<DailyLivingArticleDto>(this, activeIndex == 0 ? ReplaceArticles : AddArticles) }
    };
        var options = new DialogOptions
        {
            CloseButton = false,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            NoHeader = true
        };

        var dialog = DialogService.Show<RadioAutoCompleteDialog>("", parameters, options);
        await dialog.Result;
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








    protected async Task DeleteHandler(string id)
    {

    }
    protected async Task RenameHandler()
    {
        if (!string.IsNullOrWhiteSpace(selectedTopic?.topicName))
        {
            OpenRenameDialog(selectedTopic.topicName);
        }
    }


    protected override async Task OnInitializedAsync()
    {
        await OnTabChanged(0);
        featuredEventSlots = await GetFeaturedSlotsAsync();
        Categories = await GetEventsCategories();
        AllEventsList = await GetAllEvents();
        ActiveTopics = await GetActiveTopics();
        ActiveTopics = await GetActiveTopics();
        if (ActiveTopics?.Any() == true)
        {
            selectedTopic = ActiveTopics.First();
            AvailableArticles = await GetAvailableArticles(selectedTopic.Id);
        }
    }

    protected async Task OnTabChanged(int index)
    {
        activeIndex = index;
        if (index < 2)
        {
            switch (index)
            {
                case 0:
                    articles = await DailyService.GetTopSectionAsync();
                    break;
                case 1:
                    featuredEventSlots = await GetFeaturedSlotsAsync();
                    break;
            }
        }
        else
        {
            int topicIndex = index - 2;
            if (topicIndex >= 0 && topicIndex < ActiveTopics.Count)
            {
                selectedTopic = ActiveTopics[topicIndex];
                articles = await GetArticlesByTopicIdAsync(selectedTopic.Id);
                AvailableArticles = await GetAvailableArticles(selectedTopic.Id);
            }
        }
        StateHasChanged();
    }

    private async Task<List<DailyLivingArticleDto>> GetArticlesByTopicIdAsync(string topicId)
    {
        var topicArticles = await DailyService.GetContentByTopicIdAsync(topicId);
        if (topicArticles?.Any() == true)
        {
            return topicArticles;
        }

        return new List<DailyLivingArticleDto>();
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
            DailyLivingTab.Lifestyle => "LifeStyle",
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

    private async Task OpenRenameDialog(string currentName)
    {
        var parameters = new DialogParameters
        {
            ["TopicName"] = currentName,
            ["ShowEditField"] = true,
            ["ButtonTitle"] = "Rename",
            ["OnConfirmed"] = EventCallback.Factory.Create<string>(this, OnRenameConfirmed)
        };
        var options = new DialogOptions { CloseButton = false, MaxWidth = MaxWidth.Small, FullWidth = true };
        DialogService.Show<TopicRenameDialog>("", parameters, options);
    }

    private async Task OnRenameConfirmed(string newName)
    {
        try
        {
            selectedTopic.topicName = newName;
            var apiResponse = await DailyService.UpdateTopicAsync(selectedTopic);
            if (apiResponse.IsSuccessStatusCode)
            {
                Snackbar.Add("Topic Renamed Successfully", Severity.Success);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "GetEvents");
        }

    }
    private async Task PublishArticle()
    {
        var parameters = new DialogParameters
        {
            { "Title", "Publish Topic" },
            { "Descrption", $"Do you want to publish this Topic: {selectedTopic?.topicName}?" },
            { "ButtonTitle", "Publish" },
            { "OnConfirmed", EventCallback.Factory.Create(this, UpdateArticleAsync) }
        };
        var options = new DialogOptions { CloseButton = false, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = DialogService.Show<ConfirmationDialog>("", parameters, options);
        var result = await dialog.Result;
    }
    private async Task UnPublishArticle()
    {
        var parameters = new DialogParameters
        {
            { "Title", "UnPublish Topic" },
            { "Descrption", $"Do you want to unpublish this Topic: {selectedTopic?.topicName}?" },
            { "ButtonTitle", "UnPublish" },
            { "OnConfirmed", EventCallback.Factory.Create(this, UpdateArticleAsync) }
        };
        var options = new DialogOptions { CloseButton = false, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = DialogService.Show<ConfirmationDialog>("", parameters, options);
        var result = await dialog.Result;
    }
    private async Task UpdateArticleAsync()
    {
        try
        {
            var status = selectedTopic.isPublished;
            if (selectedTopic.isPublished)
            {
                selectedTopic.isPublished = false;
            }
            else
            {
                selectedTopic.isPublished = true;
            }
            var apiResponse = await DailyService.UpdateTopicAsync(selectedTopic);
            if (apiResponse.IsSuccessStatusCode)
            {
                if (status)
                {
                    Snackbar.Add("Topic UnPublished Successfully", Severity.Success);
                }
                else
                {
                    Snackbar.Add("Topic Published Successfully", Severity.Success);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "GetEvents");
        }
    }
    private async Task DeleteArticleAsync(string id)
    {
        try
        {
            var apiResponse = await DailyService.DeleteArticleAsync(id);
            if (apiResponse.IsSuccessStatusCode)
            {
                Snackbar.Add("Article Deleted Successfully", Severity.Success);
                await OnTabChanged(activeIndex);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "DeleteArticle");
        }
    }
    private async Task<List<DailyLivingArticleDto>> GetAvailableArticles(string topicId)
    {
        try
        {
            var topics = await DailyService.GetAvailableArticles(topicId);
            if (topics.IsSuccessStatusCode)
            {
                var response = await topics.Content.ReadFromJsonAsync<List<DailyLivingArticleDto>>();
                var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                return response ?? new List<DailyLivingArticleDto>();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "GetAvailableArticles");
        }
        return new List<DailyLivingArticleDto>();
    }
    private async Task AddArticles(DailyLivingArticleDto article)
    {
        try
        {
            article.RelatedContentId = selectedTopic.Id;
            var topics = await DailyService.AddArticle(article);
            if (topics.IsSuccessStatusCode)
            {
                Snackbar.Add("Article Added Successfully", Severity.Success);
                await OnTabChanged(activeIndex);
            }
            else
            {
                Snackbar.Add("Failed to add article", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "AddArticles");
        }
    }
    protected async Task ReplaceArticles(DailyLivingArticleDto article)
    {
        try
        {
            article.RelatedContentId = selectedTopic.Id;
            article.SlotNumber = ReplaceArticle.SlotNumber;
            var topics = await DailyService.AddArticle(article);
            if (topics.IsSuccessStatusCode)
            {
                Snackbar.Add("Article Added Successfully", Severity.Success);
                await OnTabChanged(activeIndex);
            }
            else
            {
                Snackbar.Add("Failed to add article", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "AddArticles");
        }
    }

}

