using Dapr.Client;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.IService.V2IClassifiedBoService;
using QLN.Common.Infrastructure.Subscriptions;
using static QLN.Common.Infrastructure.Constants.ConstantValues;

namespace QLN.Content.MS.Service.ClassifiedBoService
{
    public class InternalClassifiedLandigBo : IClassifiedBoLandingService
    {
        private readonly Dapr.Client.DaprClient _dapr;
        private readonly ILogger<IClassifiedBoLandingService> _logger;
        private readonly IClassifiedService _classified;

        private const string StoreName = ConstantValues.StateStoreNames.LandingBackOfficeStore;
        private const string ItemsIndexKey = ConstantValues.StateStoreNames.LandingBOIndex;
        private const string ItemsServiceIndexKey = ConstantValues.StateStoreNames.LandingServiceBOIndex;

        public InternalClassifiedLandigBo(IClassifiedService classified, DaprClient dapr, ILogger<IClassifiedBoLandingService> logger)
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
                dto.Id = Guid.NewGuid();
                dto.CreatedAt = DateTime.UtcNow;
                dto.UpdatedAt = DateTime.UtcNow;
                dto.IsActive = true;

                _logger.LogInformation("Creating new seasonal pick. Category: {CategoryName}, User: {UserId}, ID: {Id}", dto.CategoryName, dto.UserId, dto.Id);

                await _dapr.SaveStateAsync(StoreName, dto.Id.ToString(), dto);

                _logger.LogInformation("Saved seasonal pick state successfully. ID: {Id}", dto.Id);

                string indexKey = dto.Vertical?.ToLower() switch
                {
                    Verticals.Classifieds => ItemsIndexKey,
                    Verticals.Services => ItemsServiceIndexKey,
                    _ => throw new ArgumentOutOfRangeException(nameof(dto.Vertical), $"Unsupported vertical: {dto.Vertical}")
                };


                var index = await _dapr.GetStateAsync<List<string>>(StoreName, indexKey) ?? new List<string>();
                if (!index.Contains(dto.Id.ToString()))
                {
                    index.Add(dto.Id.ToString());
                    await _dapr.SaveStateAsync(StoreName, indexKey, index);
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

        public async Task<List<SeasonalPicksDto>> GetSeasonalPicks(string vertical, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(vertical))
                    throw new ArgumentException("Vertical is required to retrieve seasonal picks.", nameof(vertical));

                _logger.LogInformation("Fetching seasonal picks from state store...");

                string indexKey = vertical.ToLower() switch
                {
                    Verticals.Classifieds => ItemsIndexKey,
                    Verticals.Services => ItemsServiceIndexKey,
                    _ => throw new ArgumentOutOfRangeException(nameof(vertical), $"Unsupported vertical: {vertical}")
                };

                var index = await _dapr.GetStateAsync<List<string>>(StoreName, indexKey) ?? new();

                if (!index.Any())
                {
                    _logger.LogInformation("No seasonal picks found in the index.");
                    return new List<SeasonalPicksDto>();
                }

                var stateTasks = index.Select(id =>
                    _dapr.GetStateAsync<SeasonalPicksDto>(StoreName, id)).ToList();

                var seasonalPicks = await Task.WhenAll(stateTasks);

                var activePicks = seasonalPicks
                    .Where(p => p != null && p.IsActive == true && (p.SlotOrder == null || p.SlotOrder < 1 || p.SlotOrder > 6) &&
                    (p.EndDate == null || p.EndDate > DateTime.UtcNow))
                    .OrderByDescending(p => p.UpdatedAt)
                    .ToList();

                _logger.LogInformation("Retrieved {Count} active seasonal picks for vertical: {Vertical}", activePicks.Count, vertical);

                return activePicks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch seasonal picks.");
                throw;
            }
        }

        public async Task<List<SeasonalPicksDto>> GetSlottedSeasonalPicks(string vertical, CancellationToken cancellationToken = default)
        {            
            try
            {
                if (string.IsNullOrWhiteSpace(vertical))
                    throw new ArgumentException("Vertical is required to retrieve slotted seasonal picks.", nameof(vertical));

                _logger.LogInformation("Fetching slotted seasonal picks from state store...");

                string indexKey = vertical.ToLower() switch
                {
                    Verticals.Classifieds => ItemsIndexKey,        
                    Verticals.Services => ItemsServiceIndexKey,    
                    _ => throw new ArgumentOutOfRangeException(nameof(vertical), $"Unsupported vertical: {vertical}")
                };

                var index = await _dapr.GetStateAsync<List<string>>(StoreName, indexKey)
                            ?? new List<string>();

                if (!index.Any())
                {
                    _logger.LogInformation("No seasonal picks found in the index.");
                    return new List<SeasonalPicksDto>();
                }

                var stateTasks = index.Select(id =>
                    _dapr.GetStateAsync<SeasonalPicksDto>(StoreName, id)).ToList();

                var seasonalPicks = await Task.WhenAll(stateTasks);

                var slottedPicks = seasonalPicks
                    .Where(p => p != null && p.IsActive == true && (p.SlotOrder >= 1 && p.SlotOrder <= 6) &&
                    (p.EndDate == null || p.EndDate > DateTime.UtcNow))
                    .OrderBy(p => p.SlotOrder) 
                    .ToList();

                _logger.LogInformation("Fetched {Count} slotted seasonal picks.", slottedPicks.Count);

                return slottedPicks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch slotted seasonal picks.");
                throw new InvalidOperationException("Error fetching slotted seasonal picks.", ex);
            }
        }

        public async Task<string> ReplaceSlotWithSeasonalPick(string vertical, string? userId, Guid newPickId, int targetSlot, CancellationToken cancellationToken = default)
        {
            if (targetSlot < 1 || targetSlot > 6)
                throw new ArgumentOutOfRangeException(nameof(targetSlot), "Slot must be between 1 and 6.");

            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical is required.", nameof(vertical));

            try
            {
                string indexKey = vertical.ToLower() switch
                {
                    Verticals.Classifieds => ItemsIndexKey,         
                    Verticals.Services => ItemsServiceIndexKey,    
                    _ => throw new ArgumentOutOfRangeException(nameof(vertical), $"Unsupported vertical: {vertical}")
                };

                var index = await _dapr.GetStateAsync<List<string>>(StoreName, indexKey) ?? new List<string>();

                if (!index.Contains(newPickId.ToString()))
                    throw new InvalidOperationException("Selected pick ID not found.");

                SeasonalPicksDto? newPick = null;

                foreach (var id in index)
                {
                    var pick = await _dapr.GetStateAsync<SeasonalPicksDto>(StoreName, id);
                    if (pick == null) continue;

                    // Case 1: Slot is currently occupied by someone else — clear it
                    if (pick.SlotOrder == targetSlot && pick.Id != newPickId)
                    {
                        pick.SlotOrder = 0;
                        pick.UpdatedAt = DateTime.UtcNow;
                        await _dapr.SaveStateAsync(StoreName, id, pick);
                    }

                    // Case 2: The new pick is already slotted somewhere else — clear it before reassign
                    if (pick.Id == newPickId)
                    {
                        newPick = pick;
                    }
                }

                if (newPick == null)
                    throw new InvalidOperationException("New pick data not found in state.");

                // Update the selected pick with new slot
                newPick.SlotOrder = targetSlot;
                newPick.UpdatedAt = DateTime.UtcNow;

                await _dapr.SaveStateAsync(StoreName, newPick.Id.ToString(), newPick);

                return $"Successfully replaced slot {targetSlot} with seasonal pick '{newPick.CategoryName}' under vertical '{vertical}'.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error replacing slot {Slot} with pick {PickId} in vertical: {Vertical}", targetSlot, newPickId, vertical);
                throw new InvalidOperationException("Failed to replace slot with selected seasonal pick.", ex);
            }
        }

        public async Task<string> ReorderSeasonalPickSlots(SeasonalPickSlotReorderRequest request, CancellationToken cancellationToken = default)
        {
            const int MaxSlot = 6;           

            if (string.IsNullOrWhiteSpace(request.UserId))
                throw new ArgumentException("UserId is required.");

            if (string.IsNullOrWhiteSpace(request.Vertical))
                throw new ArgumentException("Vertical is required.");

            if (request.SlotAssignments == null || request.SlotAssignments.Count != MaxSlot)
                throw new InvalidDataException($"Exactly {MaxSlot} slot assignments must be provided.");

            var slotNumbers = request.SlotAssignments.Select(sa => sa.SlotNumber).ToList();
            if (slotNumbers.Distinct().Count() != MaxSlot || slotNumbers.Any(s => s < 1 || s > MaxSlot))
                throw new InvalidDataException("SlotNumber must be unique and between 1 and 6.");

            string indexKey = request.Vertical.ToLower() switch
            {
                Verticals.Classifieds => ItemsIndexKey,
                Verticals.Services => ItemsServiceIndexKey,
                _ => throw new ArgumentOutOfRangeException(nameof(request.Vertical), $"Unsupported vertical: {request.Vertical}")
            };

            var seasonalIndex = await _dapr.GetStateAsync<List<string>>(StoreName, indexKey) ?? new();
            var loadedPicks = new Dictionary<string, SeasonalPicksDto>();

            foreach (var assignment in request.SlotAssignments)
            {
                if (string.IsNullOrWhiteSpace(assignment.PickId))
                    continue;

                if (!seasonalIndex.Contains(assignment.PickId))
                    continue;

                var pick = await _dapr.GetStateAsync<SeasonalPicksDto>(StoreName, assignment.PickId);
                if (pick == null)
                    throw new InvalidDataException($"Pick with ID '{assignment.PickId}' not found.");

                if (pick.UserId != request.UserId)
                    throw new UnauthorizedAccessException("You are not authorized to update this pick.");

                loadedPicks[assignment.PickId] = pick;
            }

            foreach (var assignment in request.SlotAssignments)
            {
                var slotKey = $"seasonal-pick-slot-{assignment.SlotNumber}";

                if (string.IsNullOrWhiteSpace(assignment.PickId))
                {
                    await _dapr.DeleteStateAsync(StoreName, slotKey, cancellationToken: cancellationToken);
                    continue;
                }

                var pick = loadedPicks[assignment.PickId];
                pick.SlotOrder = assignment.SlotNumber;
                pick.UpdatedAt = DateTime.UtcNow;

                await _dapr.SaveStateAsync(StoreName, slotKey, pick);
                await _dapr.SaveStateAsync(StoreName, pick.Id.ToString(), pick);
            }

            return "Slots updated successfully.";
        }

        public async Task<string> SoftDeleteSeasonalPick(string pickId, string userId, string vertical, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(pickId))
                throw new ArgumentException("Pick ID must be provided.", nameof(pickId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID must be provided.", nameof(userId));

            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical must be provided.", nameof(vertical));
            try
            {
                _logger.LogInformation("Attempting delete for seasonal pick. PickId: {PickId}, UserId: {UserId}", pickId, userId);

                string indexKey = vertical.ToLower() switch
                {
                    Verticals.Classifieds => ItemsIndexKey,
                    Verticals.Services => ItemsServiceIndexKey,
                    _ => throw new ArgumentOutOfRangeException(nameof(vertical), $"Unsupported vertical: {vertical}")
                };

                var index = await _dapr.GetStateAsync<List<string>>(StoreName, indexKey) ?? new();

                if (!index.Contains(pickId))
                {
                    _logger.LogWarning("PickId {PickId} not found in vertical index: {Vertical}", pickId, vertical);
                    throw new UnauthorizedAccessException($"PickId '{pickId}' does not belong to vertical '{vertical}'.");
                }

                var pick = await _dapr.GetStateAsync<SeasonalPicksDto>(StoreName, pickId);
                if (pick == null)
                {
                    _logger.LogWarning("Pick not found for delete. PickId: {PickId}", pickId);
                    throw new KeyNotFoundException($"Pick with ID '{pickId}' not found.");
                }

                if (pick.UserId != userId)
                {
                    _logger.LogWarning("Unauthorized attempt to delete pick. PickId: {PickId}, UserId: {UserId}", pickId, userId);
                    throw new UnauthorizedAccessException("You are not authorized to delete this pick.");
                }

                pick.IsActive = false;
                pick.UpdatedAt = DateTime.UtcNow;

                await _dapr.SaveStateAsync(StoreName, pickId, pick);

                _logger.LogInformation("Successfully deleted pick. PickId: {PickId}", pickId);

                return $"Pick '{pick.CategoryName}' has been deleted.";
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error performing soft delete on pick. PickId: {PickId}, UserId: {UserId}", pickId, userId);
                throw;
            }
        }


    }
}
