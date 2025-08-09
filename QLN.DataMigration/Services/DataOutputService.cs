namespace QLN.DataMigration.Services
{
    using Dapr.Client;
    using Dapr.Client.Autogen.Grpc.v1;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using QLN.Common.DTO_s;
    using QLN.Common.Infrastructure.Constants;
    using QLN.Common.Infrastructure.DTO_s;
    using QLN.Common.Infrastructure.IService.IContentService;
    using QLN.Common.Infrastructure.IService.V2IContent;
    using QLN.Common.Infrastructure.Utilities;
    using QLN.DataMigration.Models;
    using System.Text;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using static QLN.Common.Infrastructure.Constants.ConstantValues;

    public class DataOutputService : IDataOutputService
    {
        private readonly ILogger<DataOutputService> _logger;
        private readonly IV2EventService _eventService;
        private readonly IV2NewsService _newsService;
        private readonly IV2CommunityPostService _communityPostService;
        private readonly DaprClient _daprClient;

        public DataOutputService(
            ILogger<DataOutputService> logger,
            IV2EventService eventService,
            IV2NewsService newsService,
            IV2CommunityPostService communityPostService,
            DaprClient daprClient
            )
        {
            _logger = logger;
            _eventService = eventService;
            _newsService = newsService;
            _communityPostService = communityPostService;
            _daprClient = daprClient;
        }

        public async Task SaveCategoriesAsync(ItemsCategories itemsCategories, CancellationToken cancellationToken)
        {
            foreach (var item in itemsCategories.Models)
            {
                await _daprClient.SaveStateAsync(ConstantValues.StateStoreNames.CommonStore, item.Id.ToString(), item, cancellationToken: cancellationToken);
                _logger.LogInformation($"Saving {item.Name} with ID {item.Id} to state");
            }

            _logger.LogInformation("Completed saving all state");
        }

        public async Task SaveMigrationItemsAsync(List<MigrationItem> migrationItems, CancellationToken cancellationToken)
        {
            foreach (var item in migrationItems)
            {
                var newGuid = Guid.NewGuid().ToString();

                await _daprClient.SaveStateAsync(ConstantValues.StateStoreNames.CommonStore, newGuid, item, cancellationToken: cancellationToken);

                _logger.LogInformation($"Saving {item.Title} with ID {newGuid} to state");
            }
            _logger.LogInformation("Completed saving all items to state");
        }

        public async Task SaveContentNewsAsync(List<ArticleItem> items, int categoryId, int subcategoryId, CancellationToken cancellationToken)
        {
            foreach (var dto in items)
            {
                try
                {

                    var articleCategories = new List<V2ArticleCategory>() { new V2ArticleCategory
                    {
                        CategoryId = categoryId,
                        SubcategoryId = subcategoryId,
                        SlotId = (int)Common.DTO_s.Slot.UnPublished
                    } };

                    var article = new V2NewsArticleDTO
                    {
                        Id = ProcessingHelpers.StringToGuid(dto.Nid),
                        Title = dto.Title,
                        Content = dto.Description,
                        WriterTag = dto.UserName,
                        Slug = dto.Slug,
                        IsActive = dto.Status == "1",
                        Categories = articleCategories,
                        PublishedDate = DateTime.TryParse(dto.DateCreated, out var publishedDate) ? publishedDate : DateTime.UtcNow,
                        CreatedBy = dto.UserName,
                        UpdatedBy = dto.UserName,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        authorName = dto.UserName,
                        CoverImageUrl = dto.ImageUrl,
                        UserId = dto.UserName
                    };

                    // modify this to send to the Content Service directly
                    await _newsService.CreateNewsArticleAsync(article.UserId, article, cancellationToken);
                    //await _daprClient.SaveStateAsync(V2Content.ContentStoreName, article.Id.ToString(), article, cancellationToken: cancellationToken);

                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to create article {dto.Nid} - {ex.Message}");
                    throw new Exception("Unexpected error during article creation", ex);
                }

            }
        }

        public async Task SaveContentEventsAsync(List<ContentEvent> items, int destinationCategoryId, CancellationToken cancellationToken)
        {
            foreach (var dto in items)
            {
                try
                {
                    var id = ProcessingHelpers.StringToGuid(dto.Nid);

                    var hasStartDate = DateTime.TryParse(dto.EventStart, out var startDate);
                    var hasEndDate = DateTime.TryParse(dto.EventEnd, out var endDate);

                    var hasStartTime = startDate.Hour != 0; // check if start time is set to midnight
                    var hasEndTime = endDate.Hour != 0; // check if end time is set to midnight (not a perfect solution but unlikely event would end here ?)

                    var entity = new V2Events
                    {
                        Id = id,
                        Slug = dto.Slug,
                        CategoryId = destinationCategoryId,
                        CategoryName = dto.EventCategory,
                        EventTitle = dto.Title,
                        EventType = V2EventType.FreeAcess,
                        Venue = dto.EventVenue,
                        Longitude = dto.EventLat,
                        Latitude = dto.EventLong,
                        EventDescription = dto.Description,
                        CoverImage = dto.ImageUrl,
                        IsFeatured = false,
                        PublishedDate = dto.CreatedAt,
                        IsActive = true,
                        CreatedBy = dto.UserName,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = dto.UserName,
                        Status = Common.DTO_s.EventStatus.Published
                    };

                    var eventSchedule = new EventSchedule();

                    if(hasStartDate)
                    {
                        eventSchedule.StartDate = new DateOnly(startDate.Year, startDate.Month, startDate.Day);
                        eventSchedule.TimeSlotType = V2EventTimeType.GeneralTime;
                        eventSchedule.GeneralTextTime = $"{dto.EventStart}";
                    }

                    if(hasEndDate && startDate != endDate)
                    {
                        eventSchedule.EndDate = new DateOnly(endDate.Year, endDate.Month, endDate.Day);
                        eventSchedule.TimeSlotType = V2EventTimeType.GeneralTime;
                        eventSchedule.GeneralTextTime = string.IsNullOrWhiteSpace(eventSchedule.GeneralTextTime) ? $"{dto.EventEnd}" : $" - {dto.EventEnd}";
                    } 
                    else
                    {
                        eventSchedule.EndDate = new DateOnly(startDate.Year, startDate.Month, startDate.Day);
                    }

                    if (eventSchedule.TimeSlotType == V2EventTimeType.GeneralTime)
                    {
                        entity.EventSchedule = eventSchedule;
                    }

                    var timeSlots = new List<TimeSlot>();

                    if(hasStartTime)
                    {
                        timeSlots.Add(new TimeSlot
                        {
                            DayOfWeek = startDate.DayOfWeek,
                            TextTime = startDate.ToShortTimeString(),
                        });
                    }

                    if (hasEndTime && startDate != endDate)
                    {
                        timeSlots.Add(new TimeSlot
                        {
                            DayOfWeek = endDate.DayOfWeek,
                            TextTime = endDate.ToShortTimeString(),
                        });
                    };

                    if(timeSlots.Count > 0)
                    {
                       entity.EventSchedule.TimeSlotType = V2EventTimeType.PerDayTime;
                       entity.EventSchedule.TimeSlots = timeSlots;
                    }

                    // modify this to send to the Content Service directly
                    await _eventService.CreateEvent(dto.UserName, entity, cancellationToken);
                    //await _daprClient.SaveStateAsync(
                    //    ConstantValues.V2Content.ContentStoreName,
                    //    id.ToString(),
                    //    entity,
                    //    cancellationToken: cancellationToken
                    //);

                    //var keys = await _daprClient.GetStateAsync<List<string>>(
                    //    ConstantValues.V2Content.ContentStoreName,
                    //    ConstantValues.V2Content.EventIndexKey,
                    //    cancellationToken: cancellationToken
                    //) ?? new List<string>();

                    //if (!keys.Contains(id.ToString()))
                    //{
                    //    keys.Add(id.ToString());
                    //    await _daprClient.SaveStateAsync(
                    //        ConstantValues.V2Content.ContentStoreName,
                    //        ConstantValues.V2Content.EventIndexKey,
                    //        keys,
                    //        cancellationToken: cancellationToken
                    //    );
                    //}

                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to create article {dto.Nid} - {ex.Message}");
                    throw new Exception("Unexpected error during article creation", ex);
                }

            }
        }

        public async Task SaveContentCommunityPostsAsync(List<CommunityPost> items, CancellationToken cancellationToken)
        {
            foreach (var dto in items)
            {
                try
                {
                    var id = ProcessingHelpers.StringToGuid(dto.Nid);

                    var entity = new V2CommunityPostDto
                    {
                        Id = id,
                        Slug = dto.Slug,
                        CategoryId = dto.CategoryId,
                        Category = dto.Category,
                        Title = dto.Title,
                        UpdatedBy = dto.UserName,
                        UpdatedDate = DateTime.UtcNow,
                        Description = dto.Description,
                        ImageUrl = dto.ImageUrl,
                        IsActive = true,
                        UserName = dto.UserName,
                        DateCreated = DateTime.TryParse(dto.DateCreated, out var dateCreated) ? dateCreated : DateTime.UtcNow
                    };


                    // modify this to send to the Content Service directly
                    await _communityPostService.CreateCommunityPostAsync(dto.UserName, entity, cancellationToken);
                    //await _daprClient.SaveStateAsync(
                    //    ConstantValues.V2Content.ContentStoreName,
                    //    id.ToString(),
                    //    entity,
                    //    cancellationToken: cancellationToken
                    //);

                    //var keys = await _daprClient.GetStateAsync<List<string>>(
                    //    ConstantValues.V2Content.ContentStoreName,
                    //    "community-index",
                    //    cancellationToken: cancellationToken
                    //) ?? new List<string>();

                    //if (!keys.Contains(id.ToString()))
                    //{
                    //    keys.Add(id.ToString());
                    //    await _daprClient.SaveStateAsync(
                    //        ConstantValues.V2Content.ContentStoreName,
                    //        "community-index",
                    //        keys,
                    //        cancellationToken: cancellationToken
                    //    );
                    //}

                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to create article {dto.Nid} - {ex.Message}");
                    throw new Exception("Unexpected error during article creation", ex);
                }

            }
        }

        public async Task SaveEventCategoriesAsync(List<Common.Infrastructure.DTO_s.EventCategory> items, CancellationToken cancellationToken)
        {
            foreach (var dto in items)
            {
                try {

                    if(int.TryParse(dto.Id, out var id))
                    {
                        var entity = new EventsCategory
                        {
                            Id = id,
                            CategoryName = dto.Name
                        };

                        await _eventService.CreateCategory(entity, cancellationToken);

                        _logger.LogInformation($"Created category {dto.Id} - {dto.Name}");

                    } else
                    {
                        _logger.LogError($"Failed to create category {dto.Id} - {dto.Name}");
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to create category {dto.Id} - {ex.Message}");
                    throw new Exception("Unexpected error during article creation", ex);
                }
            }
        }
    }
}
