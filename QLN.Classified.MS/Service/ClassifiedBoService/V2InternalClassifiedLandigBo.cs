using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.IService.V2IClassifiedBoService;

namespace QLN.Content.MS.Service.ClassifiedBoService
{
    public class V2InternalClassifiedLandigBo : V2IClassifiedBoLandingService
    {
        private readonly Dapr.Client.DaprClient _dapr;
        private readonly ILogger<V2IClassifiedBoLandingService> _logger;
        private readonly IClassifiedService _classified;

        private const string StoreName = "contentstatestore";
        private const string ItemsIndexKey = "qln-classifiedBo-ms";

        public V2InternalClassifiedLandigBo(IClassifiedService classified, DaprClient dapr, ILogger<V2IClassifiedBoLandingService> logger)
        {
            _classified = classified;
            _dapr = dapr;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public async Task<List<L1CategoryDto>> GetL1CategoriesByVerticalAsync(string vertical, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical is required.", nameof(vertical));

            var trees = await _classified.GetAllCategoryTrees(vertical, cancellationToken);

            var l1Categories = trees
                .Select(t => new L1CategoryDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Fields = t.Fields
                })
                .ToList();

            return l1Categories;
        }

        public async Task<string> CreateLandingBoItemAsync(string userId,V2ClassifiedLandingBoDto dto, CancellationToken cancellationToken = default)
        {
            if (dto.SlotOrder < 1 || dto.SlotOrder > 6)
                throw new InvalidDataException("Only slots 1 to 6 are allowed.");

            var slotKey = $"classified-slot-{dto.SlotOrder}";
            var existing = await _dapr.GetStateAsync<V2ClassifiedLandingBoDto>(StoreName, slotKey, cancellationToken: cancellationToken);

            if (existing != null)
            {
                throw new InvalidDataException($"Slot {dto.SlotOrder} is already occupied by '{existing.Title}' (Category: {existing.Category}).");
            }

            dto.Id = Guid.NewGuid().ToString();
            dto.IsActive = true;

            // Save to slot
            await _dapr.SaveStateAsync(StoreName, slotKey, dto, cancellationToken: cancellationToken);

            // Save full record by ID
            await _dapr.SaveStateAsync(StoreName, dto.Id, dto, cancellationToken: cancellationToken);

            // Maintain index
            var index = await _dapr.GetStateAsync<List<string>>(StoreName, ItemsIndexKey, cancellationToken: cancellationToken) ?? new();
            if (!index.Contains(dto.Id))
            {
                index.Add(dto.Id);
                await _dapr.SaveStateAsync(StoreName, ItemsIndexKey, index, cancellationToken: cancellationToken);
            }

            return $"Category '{dto.Category}' added to slot {dto.SlotOrder}";
        }

        public async Task<string> CreateSeasonalPick(SeasonalPicksDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                const string StoreName = "contentstatestore";
                const string IndexKey = "seasonal-picks-index";

                dto.Id = Guid.NewGuid();
                dto.CreatedAt = DateTime.UtcNow;
                dto.UpdatedAt = DateTime.UtcNow;
                dto.IsActive = true;

                _logger.LogInformation("Creating new seasonal pick. Category: {CategoryName}, User: {UserId}, ID: {Id}", dto.CategoryName, dto.UserId, dto.Id);

                await _dapr.SaveStateAsync(StoreName, dto.Id.ToString(), dto);

                _logger.LogInformation("Saved seasonal pick state successfully. ID: {Id}", dto.Id);

                var index = await _dapr.GetStateAsync<List<string>>(StoreName, IndexKey) ?? new List<string>();
                if (!index.Contains(dto.Id.ToString()))
                {
                    index.Add(dto.Id.ToString());
                    await _dapr.SaveStateAsync(StoreName, IndexKey, index);
                    _logger.LogInformation("Updated seasonal pick index with new ID: {Id}", dto.Id);
                }

                var result = $"Seasonal pick '{dto.CategoryName}' created successfully.";
                _logger.LogInformation("Successfully completed seasonal pick creation: {Message}", result);

                return result;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Failed to post seasonal pick. Category: {CategoryName}, User: {UserId}", dto.CategoryName, dto.UserId);
                throw;
            }
        }



    }
}
