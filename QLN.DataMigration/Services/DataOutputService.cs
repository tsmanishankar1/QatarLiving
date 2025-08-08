namespace QLN.DataMigration.Services
{
    using Dapr.Client;
    using Dapr.Client.Autogen.Grpc.v1;
    using Microsoft.Extensions.Logging;
    using QLN.Common.DTO_s;
    using QLN.Common.Infrastructure.Constants;
    using QLN.DataMigration.Helpers;
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
                        SlotId = (int)Slot.UnPublished
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

                    await _daprClient.SaveStateAsync(V2Content.ContentStoreName, article.Id.ToString(), article, cancellationToken: cancellationToken);

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
