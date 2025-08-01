using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Pages.DailyLiving.Components;
using QLN.ContentBO.WebUI.Pages.DailyLiving.Components.RadioAutoCompleteDialog;
using QLN.ContentBO.WebUI.Pages.EventCreateForm.MessageBox;
using System.Text.Json;

public class DailyLivingBase : QLComponentBase
{
    protected int activeIndex = 0;
    protected List<DailyLivingArticleDto> articles = new();
    protected bool IsLoadingEvent = true;
    protected bool isTabLoading = true;
    [Inject] IEventsService eventsService { get; set; }
    [Inject] protected INewsService newsService { get; set; }
    protected DailyLivingTab SelectedTab => (DailyLivingTab)activeIndex;
    public List<EventDTO> AllEventsList { get; set; } = new();
    protected EventDTO ReplacedEvent { get; set; } = new();
    protected DailyLivingArticleDto ReplaceArticle { get; set; } = new();
    protected List<EventCategoryModel> Categories = [];
    protected List<FeaturedSlot> featuredEventSlots = [];
    protected List<DailyTopic> ActiveTopics = [];
    protected List<NewsCategory> NewsCategories = [];
    protected DailyTopic selectedTopic { get; set; } = new();
    protected FeaturedSlot ReplaceSlot { get; set; } = new();
    protected List<DailyLivingArticleDto> AvailableArticles = new();
    [Inject] public IDailyLivingService DailyService { get; set; }
    [Inject] public IDialogService DialogService { get; set; }
    [Inject] public ILogger<DailyLivingBase> Logger { get; set; }
    protected List<FeaturedSlot> FeaturedEventSlots { get; set; } = new();
    protected bool IsLoading { get; set; } = false;


    protected override async Task OnInitializedAsync()
    {
        try
        {
            IsLoading = true;
            isTabLoading = true;
            NewsCategories = await GetNewsCategories();
            if (!NavigationPath.Value.IsLocal)
            {
                await AuthorizedPage();
            }
            var paginatedData = await GetEvents(1, 50, "", "desc", 1);
            var allEvents = paginatedData.Items;
            await OnTabChanged(0);
            featuredEventSlots = await GetFeaturedSlotsAsync();
            Categories = await GetEventsCategories();
            ActiveTopics = await GetActiveTopics();
            if (ActiveTopics?.Any() == true)
            {
                selectedTopic = ActiveTopics.First();
            }

            AllEventsList = [.. allEvents
                .Where(e => e.Status == EventStatus.Published)
                .OrderByDescending(e => e.PublishedDate ?? DateTime.MinValue)];
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "OnInitializedAsync");
            throw;
        }
        finally
        {
            IsLoading = false;
            IsLoadingEvent = false;
            isTabLoading = false;
            StateHasChanged();
        }
    }

    protected async Task OnTabChanged(int index)
    {
        try
        {
            IsLoading = true;
            activeIndex = index;
            if (index < 2)
            {
                switch (index)
                {
                    case 0:
                        articles = await DailyService.GetTopSectionAsync();
                        var relatedContentIds = articles
                            .Where(a => !string.IsNullOrWhiteSpace(a.RelatedContentId))
                            .Select(a => a.RelatedContentId)
                            .ToHashSet();
                        var allAvailable = await GetAvailableTopSectionArticles();
                        var skippedIds = allAvailable
                            .Where(av => relatedContentIds.Contains(av.Id))
                            .Select(av => av.Id)
                            .ToList();
                        AvailableArticles = allAvailable
                            .Where(av => !relatedContentIds.Contains(av.Id))
                            .ToList();
                        AvailableArticles = AvailableArticles
                            .OrderByDescending(e => e.PublishedDate ?? DateTime.MinValue)
                            .ToList();
                        var matchedArticleKeys = articles
                            .Where(a => a.SlotNumber == 2 && !string.IsNullOrWhiteSpace(a.RelatedContentId) && !string.IsNullOrWhiteSpace(a.Title))
                            .Select(a => new { Id = a.RelatedContentId, EventTitle = a.Title })
                            .ToList();
                        var paginatedData = await GetEvents(1, 50, "", "desc", 1);
                        AllEventsList = paginatedData.Items;
                        var removedEvents = AllEventsList
                            .Where(e => matchedArticleKeys.Any(x => x.Id == e.Id.ToString() && x.EventTitle == e.EventTitle))
                            .ToList();
                        AllEventsList.RemoveAll(e => removedEvents.Contains(e));
                        StateHasChanged();
                        break;
                    case 1:
                        featuredEventSlots = await GetFeaturedSlotsAsync();
                        var paginatedEvents = await GetEvents(1, 50, "", "desc", 1);
                        AllEventsList = paginatedEvents.Items;
                        var matchedFeaturedEvents = featuredEventSlots
                            .Where(fs => fs.Event != null)
                             .Select(fs => new { Id = fs.Event?.Id, Title = fs.Event?.EventTitle })
                             .ToList();
                        var repeatedEvents = AllEventsList
                            .Where(e => matchedFeaturedEvents.Any(x => x.Id == e.Id && x.Title == e.EventTitle))
                            .ToList();
                        AllEventsList.RemoveAll(e => repeatedEvents.Contains(e));
                        StateHasChanged();
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
                    var relatedContentIds = articles
                      .Where(a => !string.IsNullOrWhiteSpace(a.RelatedContentId))
                      .Select(a => a.RelatedContentId)
                      .ToHashSet();

                    var allAvailable = await GetAvailableArticles(selectedTopic.Id);
                    var skippedIds = allAvailable
                        .Where(av => relatedContentIds.Contains(av.Id))
                        .Select(av => av.Id)
                        .ToList();
                    AvailableArticles = allAvailable
                        .Where(av => !relatedContentIds.Contains(av.Id))
                        .ToList();
                    var paginatedEvents = await GetEvents(1, 50, "", "desc", 1);
                    var allEventsList = paginatedEvents.Items;
                    var matchedArticleKeys = articles
                            .Where(a => !string.IsNullOrWhiteSpace(a.RelatedContentId) && !string.IsNullOrWhiteSpace(a.Title))
                            .Select(a => new { Id = a.RelatedContentId, EventTitle = a.Title })
                            .ToList();
                    var removedEvents = AllEventsList
                        .Where(e => matchedArticleKeys.Any(x => x.Id == e.Id.ToString() && x.EventTitle == e.EventTitle))
                        .ToList();
                    AllEventsList.RemoveAll(e => removedEvents.Contains(e));

                }
            }
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "OnTabChanged");
            Snackbar.Add("Failed to load articles", Severity.Error);
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    protected async Task ReplaceSlotHandler(FeaturedSlot slot)
    {
    }

    protected async Task DeleteSlotHandler(string id)
    {
        var parameters = new DialogParameters
        {
            { "Title", "Delete Confirmation" },
            { "Descrption", "Do you want to delete this Article from Slot?" },
            { "ButtonTitle", "Delete" },
            { "OnConfirmed",  EventCallback.Factory.Create(this, async () => await DeleteArticleAsync(id))}
        };
        var options = new DialogOptions { CloseButton = false, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = await DialogService.ShowAsync<ConfirmationDialog>("", parameters, options);
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
        await OpenRadioAutoCompleteDialog();
    }

    protected async Task ReplaceItem(DailyLivingArticleDto item)
    {
        var json = JsonSerializer.Serialize(item, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        ReplaceArticle = item;
        await OpenRadioDialog();
    }

    protected async Task ReplaceTopSectionItem(DailyLivingArticleDto item)
    {
        var json = JsonSerializer.Serialize(item, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        ReplaceArticle = item;
        await OpenTopSectionRadioDialog();
    }

    private async Task OpenTopSectionRadioDialog()
    {
        var articlesList = AllEventsList.Select(e => new DailyLivingArticleDto
        {
            Id = e.Id.ToString(),
            Title = e.EventTitle,
            Category = e.CategoryName ?? e.CategoryId.ToString(),
            Subcategory = string.Empty,
            RelatedContentId = e.Id.ToString(),
            ContentType = 2,
            ContentURL = null,
            PublishedDate = e.PublishedDate ?? DateTime.UtcNow,
            EndDate = null,
            SlotType = e.FeaturedSlot?.Id ?? 0,
            SlotNumber = e.FeaturedSlot?.Id ?? 0,
            CreatedBy = e.CreatedBy,
            CreatedAt = e.CreatedAt,
            UpdatedBy = e.UpdatedBy ?? e.CreatedBy,
            UpdatedAt = e.UpdatedAt ?? e.CreatedAt,
            TopicId = null
        }).ToList();
        var parameters = new DialogParameters
        {
            { nameof(RadioAutoCompleteDialog.Title), "" },
            { nameof(RadioAutoCompleteDialog.articles), ReplaceArticle.SlotNumber == 2 ? articlesList : AvailableArticles },
            { nameof(RadioAutoCompleteDialog.OnAdd), EventCallback.Factory.Create<DailyLivingArticleDto>(this, ReplaceTopSectionArticles) },
            { nameof(RadioAutoCompleteDialog.IsHighlightedEvent), ReplaceArticle.SlotNumber == 2 ? true : false },
            { nameof(RadioAutoCompleteDialog.IsTopStory), ReplaceArticle.SlotNumber == 1 ? true : false },

        };
        var options = new DialogOptions
        {
            CloseButton = false,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            NoHeader = true
        };
        var dialog = await DialogService.ShowAsync<RadioAutoCompleteDialog>("", parameters, options);
        await dialog.Result;
    }

    private async Task OpenRadioAutoCompleteDialog()
    {
        var articlesList = AllEventsList.Select(e => new DailyLivingArticleDto
        {
            Id = e.Id.ToString(),
            Title = e.EventTitle,
            Category = e.CategoryName ?? e.CategoryId.ToString(),
            Subcategory = string.Empty,
            RelatedContentId = null,
            ContentType = 2,
            ContentURL = null,
            PublishedDate = e.PublishedDate ?? DateTime.UtcNow,
            EndDate = null,
            SlotType = e.FeaturedSlot?.Id ?? 0,
            SlotNumber = e.FeaturedSlot?.Id ?? 0,
            CreatedBy = e.CreatedBy,
            CreatedAt = e.CreatedAt,
            UpdatedBy = e.UpdatedBy ?? e.CreatedBy,
            UpdatedAt = e.UpdatedAt ?? e.CreatedAt,
            TopicId = null
        }).ToList();
        var parameters = new DialogParameters
        {
            { nameof(RadioAutoCompleteDialog.Title), "" },
            { nameof(RadioAutoCompleteDialog.articles), AvailableArticles },
            { nameof(RadioAutoCompleteDialog.origin), "dailyTopic" },
            { nameof(RadioAutoCompleteDialog.OnAdd), EventCallback.Factory.Create<DailyLivingArticleDto>(this, activeIndex == 0 ? ReplaceArticles : AddArticles) },
            { nameof(RadioAutoCompleteDialog.eventList), articlesList },

        };
        var options = new DialogOptions
        {
            CloseButton = false,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            NoHeader = true
        };

        var dialog = await DialogService.ShowAsync<RadioAutoCompleteDialog>("", parameters, options);
        await dialog.Result;
    }
    private async Task OpenRadioDialog()
    {
        var articlesList = AllEventsList.Select(e => new DailyLivingArticleDto
        {
            Id = e.Id.ToString(),
            Title = e.EventTitle,
            Category = e.CategoryName ?? e.CategoryId.ToString(),
            Subcategory = string.Empty,
            RelatedContentId = null,
            ContentType = 2,
            ContentURL = null,
            PublishedDate = e.PublishedDate ?? DateTime.UtcNow,
            EndDate = null,
            SlotType = e.FeaturedSlot?.Id ?? 0,
            SlotNumber = e.FeaturedSlot?.Id ?? 0,
            CreatedBy = e.CreatedBy,
            CreatedAt = e.CreatedAt,
            UpdatedBy = e.UpdatedBy ?? e.CreatedBy,
            UpdatedAt = e.UpdatedAt ?? e.CreatedAt,
            TopicId = null
        }).ToList();
        var parameters = new DialogParameters
    {
        { nameof(RadioAutoCompleteDialog.Title), "" },
        { nameof(RadioAutoCompleteDialog.articles), AvailableArticles },
        { nameof(RadioAutoCompleteDialog.origin), "dailyTopic" },
        { nameof(RadioAutoCompleteDialog.OnAdd), EventCallback.Factory.Create<DailyLivingArticleDto>(this, ReplaceArticles) },
        { nameof(RadioAutoCompleteDialog.eventList), articlesList },
    };
        var options = new DialogOptions
        {
            CloseButton = false,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            NoHeader = true
        };

        var dialog = await DialogService.ShowAsync<RadioAutoCompleteDialog>("", parameters, options);
        await dialog.Result;
    }


    protected Task OpenDReplaceDialogAsync()
    {
        var featuredEventIds = featuredEventSlots
                            .Where(slot => slot.Event != null)
                            .Select(slot => slot.Event.Id)
                            .ToHashSet();
        
        // Filter out featured events from AllEventsList
        List<EventDTO> PublishedEventsList = [.. AllEventsList
                 .Where(e => !featuredEventIds.Contains(e.Id))
                .OrderByDescending(e => e.PublishedDate ?? DateTime.MinValue)
                .Take(50)];

        var parameters = new DialogParameters
            {
                { nameof(MessageBoxBase.Title), "Featured Event" },
                { nameof(MessageBoxBase.Placeholder), "Article Title*" },
                { nameof(MessageBoxBase.events), PublishedEventsList },
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

    protected async Task RenameHandler()
    {
        if (!string.IsNullOrWhiteSpace(selectedTopic?.topicName))
        {
            await OpenRenameDialog(selectedTopic.topicName);
        }
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
        await OpenDReplaceDialogAsync();
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
                // await OnTabChanged(activeIndex);
                // featuredEventSlots = await GetFeaturedSlotsAsync();
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
    private async Task<PaginatedEventResponse> GetEvents(
              int page = 1,
              int pageSize = 12,
              string search = "",
              string sortOrder = "desc",
              int? status = null
          )
    {
        try
        {
            IsLoading = true;
            var apiResponse = await eventsService.GetEventsByPagination(
                page: page,
                perPage: pageSize,
                search: search ?? "",
                categoryId: null,
                sortOrder: sortOrder,
                fromDate: null,
                toDate: null,
                filterType: "",
                status: status,
                location: null,
                freeOnly: false,
                featuredFirst: false
            );

            if (apiResponse.IsSuccessStatusCode)
            {
                var result = await apiResponse.Content.ReadFromJsonAsync<PaginatedEventResponse>();
                if (result != null)
                {
                    result.Items = result.Items
                        .Where(e => e.IsActive)
                        .ToList();
                }
                return result ?? new PaginatedEventResponse();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching paginated events");
        }
        finally
        {
            IsLoading = false;
        }

        return new PaginatedEventResponse();
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
            var apiResponse = await DailyService.UpdateTopicRenameAsync(selectedTopic);
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
                var json = await topics.Content.ReadAsStringAsync();
                var articles = JsonSerializer.Deserialize<List<DailyLivingArticleDto>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<DailyLivingArticleDto>();
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;
                for (int i = 0; i < articles.Count; i++)
                {
                    var articleElement = root[i];
                    if (articleElement.TryGetProperty("categories", out var categoriesElement) &&
                        categoriesElement.ValueKind == JsonValueKind.Array &&
                        categoriesElement.GetArrayLength() > 0)
                    {
                        var firstCategory = categoriesElement[0];
                        if (firstCategory.TryGetProperty("categoryId", out var categoryId))
                            articles[i].Category = categoryId.GetInt32().ToString();

                        if (firstCategory.TryGetProperty("subcategoryId", out var subcategoryId))
                            articles[i].Subcategory = subcategoryId.GetInt32().ToString();
                    }
                }
                return articles;
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
            article.TopicId = selectedTopic.Id;
            var topics = await DailyService.AddArticle(article);
            if (topics.IsSuccessStatusCode)
            {
                string message = article.ContentType switch
                {
                    1 => "Article added successfully",
                    2 => "Event added successfully",
                    3 => "Video added successfully",
                    _ => "Content added successfully"
                };

                Snackbar.Add(message, Severity.Success);
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
    protected async Task ReplaceTopSectionArticles(DailyLivingArticleDto article)
    {
        try
        {
            article.SlotNumber = ReplaceArticle.SlotNumber;
            article.SlotType = ReplaceArticle.SlotNumber;
            article.TopicId = selectedTopic.Id;
            article.ContentType = article.SlotNumber == 2 ? 2 : 1;
            var topics = await DailyService.ReplaceTopSectionArticle(article);
            if (topics.IsSuccessStatusCode)
            {
                Snackbar.Add(
                article.SlotNumber == 2 ? "Highlighted Event Replaced Successfully" : article.SlotNumber == 1 ? "Top Story Replaced Successfully" : "Article Replaced Successfully",
                 Severity.Success
                );
                await OnTabChanged(activeIndex);
            }
            else
            {
                Snackbar.Add("Failed to add article", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "ReplaceArticles");
        }
    }
    protected async Task ReplaceArticles(DailyLivingArticleDto article)
    {
        try
        {
            article.SlotNumber = ReplaceArticle.SlotNumber;
            article.TopicId = selectedTopic.Id;
            var topics = await DailyService.ReplaceArticle(article);
            if (topics.IsSuccessStatusCode)
            {
                string message = article.ContentType switch
                {
                    1 => "Article replaced successfully",
                    2 => "Event replaced successfully",
                    3 => "Video replaced successfully",
                    _ => "Content replaced successfully"
                };
                Snackbar.Add(message, Severity.Success);
                await OnTabChanged(activeIndex);
            }
            else
            {
                Snackbar.Add("Failed to add article", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "ReplaceArticles");
        }
    }
    protected async Task DeleteEventOnClick(string id)
    {
        var parameters = new DialogParameters
        {
            { "Title", "Delete Confirmation" },
            { "Descrption", "Do you want to delete this Event?" },
            { "ButtonTitle", "Delete" },
            { "OnConfirmed",  EventCallback.Factory.Create(this, async () => await DeleteFeatureEvent(id))}
        };
        var options = new DialogOptions { CloseButton = false, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = DialogService.Show<ConfirmationDialog>("", parameters, options);
        var result = await dialog.Result;
    }
    private async Task<List<DailyLivingArticleDto>> GetAvailableTopSectionArticles()
    {
        try
        {
            var response = await DailyService.GetAvailableTopSectionArticles();
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var articles = JsonSerializer.Deserialize<List<DailyLivingArticleDto>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<DailyLivingArticleDto>();
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;
                for (int i = 0; i < articles.Count; i++)
                {
                    var articleElement = root[i];
                    if (articleElement.TryGetProperty("categories", out var categoriesElement) &&
                        categoriesElement.ValueKind == JsonValueKind.Array &&
                        categoriesElement.GetArrayLength() > 0)
                    {
                        var firstCategory = categoriesElement[0];
                        if (firstCategory.TryGetProperty("categoryId", out var categoryId))
                            articles[i].Category = categoryId.GetInt32().ToString();

                        if (firstCategory.TryGetProperty("subcategoryId", out var subcategoryId))
                            articles[i].Subcategory = subcategoryId.GetInt32().ToString();
                    }
                }
                return articles;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "GetAvailableArticles");
        }
        finally
        {
            IsLoading = false;
        }
        return new List<DailyLivingArticleDto>();
    }
    private async Task<List<NewsCategory>> GetNewsCategories()
    {
        try
        {
            var apiResponse = await newsService.GetNewsCategories();
            if (apiResponse.IsSuccessStatusCode)
            {
                return await apiResponse.Content.ReadFromJsonAsync<List<NewsCategory>>() ?? [];
            }

            return [];
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "GetNewsCategories");
            return [];
        }
    }

}

