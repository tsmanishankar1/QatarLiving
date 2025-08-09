namespace QLN.DataMigration.Services
{
    using Dapr.Client;
    using Dapr.Client.Autogen.Grpc.v1;
    using Microsoft.Extensions.Logging;
    using QLN.Common.DTO_s;
    using QLN.Common.Infrastructure.Constants;
    using QLN.Common.Infrastructure.DTO_s;
    using QLN.Common.Infrastructure.Utilities;
    using QLN.DataMigration.Models;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using static QLN.Common.Infrastructure.Constants.ConstantValues;

    public class DataOutputService : IDataOutputService
    {
        private readonly DaprClient _daprClient;
        private readonly ILogger<DataOutputService> _logger;

        public DataOutputService(DaprClient daprClient, ILogger<DataOutputService> logger)
        {
            _daprClient = daprClient;
            _logger = logger;
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
                    await _daprClient.SaveStateAsync(V2Content.ContentStoreName, article.Id.ToString(), article, cancellationToken: cancellationToken);

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

                    var entity = new V2Events
                    {
                        Id = id,
                        Slug = dto.Slug,
                        CategoryId = destinationCategoryId,
                        CategoryName = dto.EventCategory,
                        EventTitle = dto.Title,
                        EventType = V2EventType.OpenRegistrations,
                        EventSchedule = new EventSchedule()
                        {
                            StartDate = DateOnly.TryParse(dto.EventStart, out var startDate) ? startDate : new DateOnly(),
                            EndDate = DateOnly.TryParse(dto.EventEnd, out var endDate) ? endDate : new DateOnly(),
                        },
                        Venue = dto.EventVenue,
                        Longitude = dto.EventLat,
                        Latitude = dto.EventLong,
                        EventDescription = dto.Description,
                        CoverImage = dto.ImageUrl,
                        IsFeatured = false,
                        PublishedDate = DateTime.UtcNow,
                        IsActive = true,
                        CreatedBy = dto.UserName,
                        CreatedAt = DateTime.UtcNow
                    };


                    // modify this to send to the Content Service directly
                    await _daprClient.SaveStateAsync(
                        ConstantValues.V2Content.ContentStoreName,
                        id.ToString(),
                        entity,
                        cancellationToken: cancellationToken
                    );

                    var keys = await _daprClient.GetStateAsync<List<string>>(
                        ConstantValues.V2Content.ContentStoreName,
                        ConstantValues.V2Content.EventIndexKey,
                        cancellationToken: cancellationToken
                    ) ?? new List<string>();

                    if (!keys.Contains(id.ToString()))
                    {
                        keys.Add(id.ToString());
                        await _daprClient.SaveStateAsync(
                            ConstantValues.V2Content.ContentStoreName,
                            ConstantValues.V2Content.EventIndexKey,
                            keys,
                            cancellationToken: cancellationToken
                        );
                    }

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
                    await _daprClient.SaveStateAsync(
                        ConstantValues.V2Content.ContentStoreName,
                        id.ToString(),
                        entity,
                        cancellationToken: cancellationToken
                    );

                    var keys = await _daprClient.GetStateAsync<List<string>>(
                        ConstantValues.V2Content.ContentStoreName,
                        "community-index",
                        cancellationToken: cancellationToken
                    ) ?? new List<string>();

                    if (!keys.Contains(id.ToString()))
                    {
                        keys.Add(id.ToString());
                        await _daprClient.SaveStateAsync(
                            ConstantValues.V2Content.ContentStoreName,
                            "community-index",
                            keys,
                            cancellationToken: cancellationToken
                        );
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to create article {dto.Nid} - {ex.Message}");
                    throw new Exception("Unexpected error during article creation", ex);
                }

            }
        }


    }
}
