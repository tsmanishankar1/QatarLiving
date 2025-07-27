using Dapr.Client;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.ClassifiedsBo;
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
        private const string ClassifiedsFeaturedStoresIndexKey = ConstantValues.StateStoreNames.FeaturedStoreClassifiedsIndexKey;
        private const string ServicesFeaturedStoresIndexKey = ConstantValues.StateStoreNames.FeaturedStoreServicesIndexKey;
        private const string FeaturedCategoryClassifiedIndex = ConstantValues.StateStoreNames.FeaturedCategoryClassifiedIndex;
        private const string FeaturedCategoryServiceIndex = ConstantValues.StateStoreNames.FeaturedCategoryServiceIndex;


        public InternalClassifiedLandigBo(IClassifiedService classified, DaprClient dapr, ILogger<IClassifiedBoLandingService> logger)
        {
            _classified = classified;
            _dapr = dapr;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }



        public async Task<string> CreateSeasonalPick(string userId, string userName, SeasonalPicksDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var newPick = new SeasonalPicks
                {
                    Id = Guid.NewGuid(),
                    Vertical = dto.Vertical,
                    CategoryId = dto.CategoryId,
                    CategoryName = dto.CategoryName,
                    L1CategoryId = dto.L1CategoryId,
                    L1categoryName = dto.L1categoryName,
                    L2categoryId = dto.L2categoryId,
                    L2categoryName = dto.L2categoryName,
                    StartDate = dto.StartDate,
                    SlotOrder = 0,
                    EndDate = dto.EndDate,
                    ImageUrl = dto.ImageUrl,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true,
                    UserId = userId,
                    UserName = userName
                };

                _logger.LogInformation("Creating new seasonal pick. Category: {CategoryName}, User: {UserId}, ID: {Id}", dto.CategoryName, newPick.UserId, newPick.Id);

                // Resolve index key based on vertical
                string indexKey = dto.Vertical?.ToLower() switch
                {
                    Verticals.Classifieds => ItemsIndexKey,
                    Verticals.Services => ItemsServiceIndexKey,
                    _ => throw new ArgumentOutOfRangeException(nameof(dto.Vertical), $"Unsupported vertical: {dto.Vertical}")
                };

                // Get existing seasonal pick IDs from index
                var index = await _dapr.GetStateAsync<List<string>>(StoreName, indexKey) ?? new List<string>();

                // Load existing seasonal pick objects to check for duplicate category name
                var existingPickTasks = index.Select(id => _dapr.GetStateAsync<SeasonalPicks>(StoreName, id)).ToList();
                var existingPicks = await Task.WhenAll(existingPickTasks);

                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                bool duplicateExists = existingPicks.Any(p =>
                    p != null &&
                    p.IsActive == true &&
                    p.CategoryName.Equals(dto.CategoryName, StringComparison.OrdinalIgnoreCase) &&
                    p.Vertical?.Equals(dto.Vertical, StringComparison.OrdinalIgnoreCase) == true && 
                    (p.EndDate == null || p.EndDate >= today));

                if (duplicateExists)
                {
                    var message = $"A seasonal pick with the category '{dto.CategoryName}' already exists for vertical '{dto.Vertical}'.";
                    _logger.LogWarning(message);
                    throw new InvalidOperationException(message);
                }

                // Save new seasonal pick
                await _dapr.SaveStateAsync(StoreName, newPick.Id.ToString(), newPick);
                _logger.LogInformation("Saved seasonal pick state successfully. ID: {Id}", newPick.Id);

                // Update index if not already present
                if (!index.Contains(newPick.Id.ToString()))
                {
                    index.Add(newPick.Id.ToString());
                    await _dapr.SaveStateAsync(StoreName, indexKey, index);
                    _logger.LogInformation("Updated seasonal pick index with new ID: {Id}", newPick.Id);
                }

                var result = $"Seasonal pick '{dto.CategoryName}' created successfully.";
                _logger.LogInformation("Successfully completed seasonal pick creation: {Message}", result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to post seasonal pick. Category: {CategoryName}", dto.CategoryName);
                throw;
            }
        }

        public async Task<List<SeasonalPicks>> GetSeasonalPicks(string vertical, CancellationToken cancellationToken = default)
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
                    return new List<SeasonalPicks>();
                }

                var stateTasks = index.Select(id =>
                    _dapr.GetStateAsync<SeasonalPicks>(StoreName, id)).ToList();

                var seasonalPicks = await Task.WhenAll(stateTasks);

                var activePicks = seasonalPicks
                    .Where(p => p != null && p.IsActive == true && (p.SlotOrder == null || p.SlotOrder < 1 || p.SlotOrder > 6) &&
                    (p.EndDate == null || p.EndDate >= DateOnly.FromDateTime(DateTime.UtcNow)))
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

        public async Task<List<SeasonalPicks>> GetSlottedSeasonalPicks(string vertical, CancellationToken cancellationToken = default)
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
                    return new List<SeasonalPicks>();
                }

                var stateTasks = index.Select(id =>
                    _dapr.GetStateAsync<SeasonalPicks>(StoreName, id)).ToList();

                var seasonalPicks = await Task.WhenAll(stateTasks);

                var slottedPicks = seasonalPicks
                    .Where(p => p != null && p.IsActive == true && (p.SlotOrder >= 1 && p.SlotOrder <= 6) &&
                    (p.EndDate == null || p.EndDate >= DateOnly.FromDateTime(DateTime.UtcNow)))
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

        public async Task<string> ReplaceSlotWithSeasonalPick(string userId, ReplaceSeasonalPickSlotRequest dto, CancellationToken cancellationToken = default)
        {
            if (dto.TargetSlotId < 1 || dto.TargetSlotId > 6)
                throw new ArgumentOutOfRangeException(nameof(dto.TargetSlotId), "Slot must be between 1 and 6.");

            if (string.IsNullOrWhiteSpace(dto.Vertical))
                throw new ArgumentException("Vertical is required.", nameof(dto.Vertical));

            try
            {
                string indexKey = dto.Vertical.ToLower() switch
                {
                    Verticals.Classifieds => ItemsIndexKey,         
                    Verticals.Services => ItemsServiceIndexKey,    
                    _ => throw new ArgumentOutOfRangeException(nameof(dto.Vertical), $"Unsupported vertical: {dto.Vertical}")
                };

                var index = await _dapr.GetStateAsync<List<string>>(StoreName, indexKey) ?? new List<string>();

                if (!Guid.TryParse(dto.PickId, out var pickGuid))
                    throw new ArgumentException("Invalid PickId format. Must be a valid GUID.", nameof(dto.PickId));

                if (!index.Contains(pickGuid.ToString()))
                    throw new InvalidOperationException("Selected pick ID not found.");

                SeasonalPicks? newPick = null;

                foreach (var id in index)
                {
                    var pick = await _dapr.GetStateAsync<SeasonalPicks>(StoreName, id);
                    if (pick == null) continue;

                    // Case 1: Slot is currently occupied by someone else — clear it
                    if (pick.SlotOrder == dto.TargetSlotId && pick.Id != pickGuid)
                    {
                        pick.SlotOrder = 0;
                        pick.UpdatedAt = DateTime.UtcNow;
                        await _dapr.SaveStateAsync(StoreName, id, pick);
                    }

                    // Case 2: The new pick is already slotted somewhere else — clear it before reassign
                    if (pick.Id == pickGuid)
                    {
                        newPick = pick;
                    }
                }

                if (newPick == null)
                    throw new InvalidOperationException("New pick data not found in state.");

                // Update the selected pick with new slot
                newPick.SlotOrder = dto.TargetSlotId;
                newPick.UpdatedAt = DateTime.UtcNow;

                await _dapr.SaveStateAsync(StoreName, newPick.Id.ToString(), newPick);

                return $"Successfully replaced slot {dto.TargetSlotId} with seasonal pick '{newPick.CategoryName}' under vertical '{dto.Vertical}'.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error replacing slot {Slot} with pick {PickId} in vertical: {Vertical}", dto.TargetSlotId, dto.PickId, dto.Vertical);
                throw new InvalidOperationException("Failed to replace slot with selected seasonal pick.", ex);
            }
        }

        public async Task<string> ReorderSeasonalPickSlots(string userId, SeasonalPickSlotReorderRequest request, CancellationToken cancellationToken = default)
        {
            const int MaxSlot = 6;           

            if (string.IsNullOrWhiteSpace(userId))
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
            var loadedPicks = new Dictionary<string, SeasonalPicks>();

            foreach (var assignment in request.SlotAssignments)
            {
                if (string.IsNullOrWhiteSpace(assignment.PickId))
                    continue;

                if (!seasonalIndex.Contains(assignment.PickId))
                    continue;

                var pick = await _dapr.GetStateAsync<SeasonalPicks>(StoreName, assignment.PickId);
                if (pick == null)
                    throw new InvalidDataException($"Pick with ID '{assignment.PickId}' not found.");

                if (pick.UserId != userId)
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

                var pick = await _dapr.GetStateAsync<SeasonalPicks>(StoreName, pickId);
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

        public async Task<string> CreateFeaturedStore(string userId, string userName, FeaturedStoreDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var stores = new FeaturedStore
                {
                    Id = Guid.NewGuid(),
                    Vertical = dto.Vertical,
                    StoreId = dto.StoreId,
                    StoreName = dto.StoreName,
                    ImageUrl = dto.ImageUrl,
                    StartDate = dto.StartDate,
                    SlotOrder = 0,
                    EndDate = dto.EndDate,
                    IsActive = true,
                    UserId = userId,
                    UserName = userName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Creating new featured store. Store: {StoreName}, User: {UserId}, ID: {Id}", dto.StoreName, userId, stores.Id);

                
                string indexKey = dto.Vertical?.ToLower() switch
                {
                    Verticals.Classifieds => ClassifiedsFeaturedStoresIndexKey,
                    Verticals.Services => ServicesFeaturedStoresIndexKey,
                    _ => throw new ArgumentOutOfRangeException(nameof(dto.Vertical), $"Unsupported vertical: {dto.Vertical}")
                };

                // Get existing store index
                var index = await _dapr.GetStateAsync<List<string>>(StoreName, indexKey) ?? new List<string>();

                var existingStoreTasks = index.Select(id => _dapr.GetStateAsync<FeaturedStore>(StoreName, id)).ToList();
                var existingStores = await Task.WhenAll(existingStoreTasks);

                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                bool duplicateExists = existingStores.Any(p =>
                    p != null &&
                    p.IsActive == true &&
                    p.StoreName.Equals(dto.StoreName, StringComparison.OrdinalIgnoreCase) &&
                    p.Vertical?.Equals(dto.Vertical, StringComparison.OrdinalIgnoreCase) == true &&
                    (p.EndDate == null && p.EndDate >= today));

                if (duplicateExists)
                {
                    var message = $"A featured store with the name '{dto.StoreName}' already exists and is still active for vertical '{dto.Vertical}'.";
                    _logger.LogWarning(message);
                    throw new InvalidOperationException(message);
                }

                
                await _dapr.SaveStateAsync(StoreName, stores.Id.ToString(), stores);
                _logger.LogInformation("Saved featured store state successfully. ID: {Id}", stores.Id);

                if (!index.Contains(stores.Id.ToString()))
                {
                    index.Add(stores.Id.ToString());
                    await _dapr.SaveStateAsync(StoreName, indexKey, index);
                    _logger.LogInformation("Updated featured store index with new ID: {Id}", stores.Id);
                }

                var result = $"Featured store '{dto.StoreName}' created successfully.";
                _logger.LogInformation("Successfully completed featured store creation: {Message}", result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to post featured store. Store: {StoreName}, User: {UserId}", dto.StoreName, userId);
                throw;
            }
        }

        public async Task<List<FeaturedStore>> GetFeaturedStores(string vertical, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(vertical))
                    throw new ArgumentException("Vertical is required to retrieve featured stores.", nameof(vertical));

                _logger.LogInformation("Fetching featured stores from state store...");

                string indexKey = vertical.ToLower() switch
                {
                    Verticals.Classifieds => ClassifiedsFeaturedStoresIndexKey,
                    Verticals.Services => ServicesFeaturedStoresIndexKey,
                    _ => throw new ArgumentOutOfRangeException(nameof(vertical), $"Unsupported vertical: {vertical}")
                };

                var index = await _dapr.GetStateAsync<List<string>>(StoreName, indexKey) ?? new();

                if (!index.Any())
                {
                    _logger.LogInformation("No featured stores found in the index.");
                    return new List<FeaturedStore>();
                }

                var stateTasks = index.Select(id =>
                    _dapr.GetStateAsync<FeaturedStore>(StoreName, id)).ToList();

                var featuredStores = await Task.WhenAll(stateTasks);

                var activeStores = featuredStores
                    .Where(p =>
                        p != null &&
                        p.IsActive == true &&
                        (p.SlotOrder == null || p.SlotOrder < 1 || p.SlotOrder > 6) &&
                        (p.EndDate == null || p.EndDate >= DateOnly.FromDateTime(DateTime.UtcNow))
                    )
                    .OrderByDescending(p => p.UpdatedAt)
                    .ToList();

                _logger.LogInformation("Retrieved {Count} active featured stores for vertical: {Vertical}", activeStores.Count, vertical);

                return activeStores;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch featured stores.");
                throw;
            }
        }

        public async Task<List<FeaturedStore>> GetSlottedFeaturedStores(string vertical, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(vertical))
                    throw new ArgumentException("Vertical is required to retrieve slotted featured stores.", nameof(vertical));

                _logger.LogInformation("Fetching slotted featured stores from state store...");

                string indexKey = vertical.ToLower() switch
                {
                    Verticals.Classifieds => ClassifiedsFeaturedStoresIndexKey,
                    Verticals.Services => ServicesFeaturedStoresIndexKey,
                    _ => throw new ArgumentOutOfRangeException(nameof(vertical), $"Unsupported vertical: {vertical}")
                };

                var index = await _dapr.GetStateAsync<List<string>>(StoreName, indexKey) ?? new List<string>();

                if (!index.Any())
                {
                    _logger.LogInformation("No featured stores found in the index.");
                    return new List<FeaturedStore>();
                }

                var stateTasks = index.Select(id =>
                    _dapr.GetStateAsync<FeaturedStore>(StoreName, id)).ToList();

                var featuredStores = await Task.WhenAll(stateTasks);

                var slottedStores = featuredStores
                    .Where(p =>
                        p != null &&
                        p.IsActive == true &&
                        p.SlotOrder >= 1 && p.SlotOrder <= 6 &&
                        (p.EndDate == null || p.EndDate >= DateOnly.FromDateTime(DateTime.UtcNow))
                    )
                    .OrderBy(p => p.SlotOrder)
                    .ToList();

                _logger.LogInformation("Fetched {Count} slotted featured stores.", slottedStores.Count);

                return slottedStores;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch slotted featured stores.");
                throw new InvalidOperationException("Error fetching slotted featured stores.", ex);
            }
        }

        public async Task<string> ReplaceSlotWithFeaturedStore(string userId, ReplaceFeaturedStoresSlotRequest dto, CancellationToken cancellationToken = default)
        {
            if (dto.TargetSlotId < 1 || dto.TargetSlotId > 6)
                throw new ArgumentOutOfRangeException(nameof(dto.TargetSlotId), "Slot must be between 1 and 6.");

            if (string.IsNullOrWhiteSpace(dto.Vertical))
                throw new ArgumentException("Vertical is required.", nameof(dto.Vertical));

            try
            {
                string indexKey = dto.Vertical.ToLower() switch
                {
                    Verticals.Classifieds => ClassifiedsFeaturedStoresIndexKey,
                    Verticals.Services => ServicesFeaturedStoresIndexKey,
                    _ => throw new ArgumentOutOfRangeException(nameof(dto.Vertical), $"Unsupported vertical: {dto.Vertical}")
                };

                var index = await _dapr.GetStateAsync<List<string>>(StoreName, indexKey) ?? new List<string>();

                if (!index.Contains(dto.StoreId.ToString()))
                    throw new InvalidOperationException("Selected featured store ID not found.");

                FeaturedStore? newStore = null;

                foreach (var id in index)
                {
                    var store = await _dapr.GetStateAsync<FeaturedStore>(StoreName, id);
                    if (store == null) continue;

                    if (store.SlotOrder == dto.TargetSlotId && store.Id.ToString() != dto.StoreId.ToString())
                    {
                        store.SlotOrder = 0;
                        store.UpdatedAt = DateTime.UtcNow;
                        await _dapr.SaveStateAsync(StoreName, id, store);
                    }

                    if (store.Id.ToString() == dto.StoreId.ToString())
                    {
                        newStore = store;
                    }
                }

                if (newStore == null)
                    throw new InvalidOperationException("New featured store data not found in state.");

                newStore.SlotOrder = dto.TargetSlotId;
                newStore.UpdatedAt = DateTime.UtcNow;

                await _dapr.SaveStateAsync(StoreName, newStore.Id.ToString(), newStore);

                return $"Successfully replaced slot {dto.TargetSlotId} with featured store '{newStore.StoreName}' under vertical '{dto.Vertical}'.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error replacing slot {Slot} with featured store {StoreId} in vertical: {Vertical}", dto.TargetSlotId, dto.StoreId, dto.Vertical);
                throw new InvalidOperationException("Failed to replace slot with selected featured store.", ex);
            }
        }

        public async Task<string> ReorderFeaturedStoreSlots(string userId, FeaturedStoreSlotReorderRequest request, CancellationToken cancellationToken = default)
        {
            const int MaxSlot = 6;

            if (string.IsNullOrWhiteSpace(userId))
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
                Verticals.Classifieds => ClassifiedsFeaturedStoresIndexKey,
                Verticals.Services => ServicesFeaturedStoresIndexKey,
                _ => throw new ArgumentOutOfRangeException(nameof(request.Vertical), $"Unsupported vertical: {request.Vertical}")
            };

            var storeIndex = await _dapr.GetStateAsync<List<string>>(StoreName, indexKey) ?? new();
            var loadedStores = new Dictionary<string, FeaturedStore>();

            foreach (var assignment in request.SlotAssignments)
            {
                if (string.IsNullOrWhiteSpace(assignment.StoreId))
                    continue;

                if (!storeIndex.Contains(assignment.StoreId))
                    continue;

                var store = await _dapr.GetStateAsync<FeaturedStore>(StoreName, assignment.StoreId);
                if (store == null)
                    throw new InvalidDataException($"Store with ID '{assignment.StoreId}' not found.");

                if (store.UserId != userId)
                    throw new UnauthorizedAccessException("You are not authorized to update this store.");

                loadedStores[assignment.StoreId] = store;
            }

            foreach (var assignment in request.SlotAssignments)
            {
                var slotKey = $"featured-store-slot-{assignment.SlotNumber}";

                if (string.IsNullOrWhiteSpace(assignment.StoreId))
                {
                    await _dapr.DeleteStateAsync(StoreName, slotKey, cancellationToken: cancellationToken);
                    continue;
                }

                var store = loadedStores[assignment.StoreId];
                store.SlotOrder = assignment.SlotNumber;
                store.UpdatedAt = DateTime.UtcNow;

                await _dapr.SaveStateAsync(StoreName, slotKey, store);
                await _dapr.SaveStateAsync(StoreName, store.Id.ToString(), store);
            }

            return "Slots updated successfully.";
        }

        public async Task<string> SoftDeleteFeaturedStore(string storeId, string userId, string vertical, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(storeId))
                throw new ArgumentException("Store ID must be provided.", nameof(storeId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID must be provided.", nameof(userId));

            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical must be provided.", nameof(vertical));

            try
            {
                _logger.LogInformation("Attempting delete for featured store. StoreId: {StoreId}, UserId: {UserId}", storeId, userId);

                string indexKey = vertical.ToLower() switch
                {
                    Verticals.Classifieds => ClassifiedsFeaturedStoresIndexKey,
                    Verticals.Services => ServicesFeaturedStoresIndexKey,
                    _ => throw new ArgumentOutOfRangeException(nameof(vertical), $"Unsupported vertical: {vertical}")
                };

                var index = await _dapr.GetStateAsync<List<string>>(StoreName, indexKey) ?? new();

                if (!index.Contains(storeId))
                {
                    _logger.LogWarning("StoreId {StoreId} not found in vertical index: {Vertical}", storeId, vertical);
                    throw new UnauthorizedAccessException($"StoreId '{storeId}' does not belong to vertical '{vertical}'.");
                }

                var store = await _dapr.GetStateAsync<FeaturedStore>(StoreName, storeId);
                if (store == null)
                {
                    _logger.LogWarning("Featured store not found for delete. StoreId: {StoreId}", storeId);
                    throw new KeyNotFoundException($"Featured store with ID '{storeId}' not found.");
                }

                if (store.UserId != userId)
                {
                    _logger.LogWarning("Unauthorized attempt to delete store. StoreId: {StoreId}, UserId: {UserId}", storeId, userId);
                    throw new UnauthorizedAccessException("You are not authorized to delete this featured store.");
                }

                store.IsActive = false;
                store.UpdatedAt = DateTime.UtcNow;

                await _dapr.SaveStateAsync(StoreName, storeId, store);

                _logger.LogInformation("Successfully deleted featured store. StoreId: {StoreId}", storeId);

                return $"Featured store '{store.StoreName}' has been deleted.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing soft delete on featured store. StoreId: {StoreId}, UserId: {UserId}", storeId, userId);
                throw;
            }
        }

        public async Task<string> CreateFeaturedCategory(string userId, string userName, FeaturedCategoryDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var categories = new FeaturedCategory
                {
                    Id = Guid.NewGuid(),
                    Vertical = dto.Vertical,
                    CategoryName = dto.CategoryName,
                    CategoryId = dto.CategoryId,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    ImageUrl = dto.ImageUrl,
                    SlotOrder = 0,
                    IsActive = true,
                    UserId = userId,
                    UserName = userName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Creating new landing bo. Category: {Category}, User: {UserId}, ID: {Id}", dto.CategoryName, userId, categories.Id);

                // Determine index key by vertical
                string indexKey = dto.Vertical?.ToLower() switch
                {
                    Verticals.Classifieds => FeaturedCategoryClassifiedIndex,
                    Verticals.Services => FeaturedCategoryServiceIndex,
                    _ => throw new ArgumentOutOfRangeException(nameof(dto.Vertical), $"Unsupported vertical: {dto.Vertical}")
                };

                var index = await _dapr.GetStateAsync<List<string>>(StoreName, indexKey, cancellationToken: cancellationToken) ?? new List<string>();
                var existingTasks = index.Select(id => _dapr.GetStateAsync<FeaturedCategory>(StoreName, id)).ToList();
                var existingItems = await Task.WhenAll(existingTasks);

                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                bool duplicateExists = existingItems.Any(p =>
                    p != null &&
                    p.IsActive == true &&
                    p.CategoryName.Equals(dto.CategoryName, StringComparison.OrdinalIgnoreCase) &&
                    p.Vertical?.Equals(dto.Vertical, StringComparison.OrdinalIgnoreCase) == true &&
                    (p.EndDate == null && p.EndDate >= today));

                if (duplicateExists)
                {
                    var message = $"A featured category '{dto.CategoryName}' already exists and is still active for vertical '{dto.Vertical}'.";
                    _logger.LogWarning(message);
                    throw new InvalidOperationException(message);
                }
                                
                await _dapr.SaveStateAsync(StoreName, categories.Id.ToString(), categories, cancellationToken: cancellationToken);
                _logger.LogInformation("Saved featured category state successfully. ID: {Id}", categories.Id);

                if (!index.Contains(categories.Id.ToString()))
                {
                    index.Add(categories.Id.ToString());
                    await _dapr.SaveStateAsync(StoreName, indexKey, index, cancellationToken: cancellationToken);
                    _logger.LogInformation("Updated index {IndexKey} with new ID: {Id}", indexKey, categories.Id);
                }

                var result = $"Landing bo '{dto.CategoryName}' created successfully.";
                _logger.LogInformation("Successfully completed landing bo creation: {Message}", result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to post landing bo. Category: {Category}, User: {UserId}", dto.CategoryName, userId);
                throw;
            }
        }
        
        public async Task<string> DeleteFeaturedCategory(string categoryId, string userId, string vertical, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(categoryId))
                throw new ArgumentException("Category ID must be provided.", nameof(categoryId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID must be provided.", nameof(userId));

            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical must be provided.", nameof(vertical));
            try
            {
                _logger.LogInformation($"Attempting delete for landing bo. FeaturedCategoryId: {categoryId}, UserId: {userId}", categoryId, userId);

                string indexKey = vertical.ToLower() switch
                {
                    Verticals.Classifieds => FeaturedCategoryClassifiedIndex,
                    Verticals.Services => FeaturedCategoryServiceIndex,
                    _ => throw new ArgumentOutOfRangeException(nameof(vertical), $"Unsupported vertical: {vertical}")
                };

                var index = await _dapr.GetStateAsync<List<string>>(StoreName, indexKey) ?? new();

                if (!index.Contains(categoryId))
                {
                    _logger.LogWarning($"FeaturedCategoryId {categoryId} not found in vertical index: {vertical}", categoryId, vertical);
                    throw new UnauthorizedAccessException($"FeaturedCategoryId '{categoryId}' does not belong to vertical '{vertical}'.");
                }

                var featuredCategory = await _dapr.GetStateAsync<FeaturedCategory>(StoreName, categoryId);
                if (featuredCategory == null)
                {
                    _logger.LogWarning("FeaturedCategory not found for delete. FeaturedCategoryId: {FeaturedCategoryId}", categoryId);
                    throw new KeyNotFoundException($"FeaturedCategory with ID '{categoryId}' not found.");
                }

                if (featuredCategory.UserId != userId)
                {
                    _logger.LogWarning($"Unauthorized attempt to delete FeaturedCategory. FeaturedCategoryId: {categoryId}, UserId: {userId}", categoryId, userId);
                    throw new UnauthorizedAccessException("You are not authorized to delete this FeaturedCategory.");
                }

                featuredCategory.IsActive = false;
                featuredCategory.UpdatedAt = DateTime.UtcNow;

                await _dapr.SaveStateAsync(StoreName, categoryId, featuredCategory);

                _logger.LogInformation($"Successfully deleted FeaturedCategory. FeaturedCategoryId: {categoryId}", categoryId);

                return $"FeaturedCategory '{featuredCategory.CategoryName}' has been deleted.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error performing soft delete on FeaturedCategory. FeaturedCategoryId: {categoryId}, UserId: {userId}", categoryId, userId);
                throw;
            }
        }

        public async Task<List<FeaturedCategory>> GetSlottedFeaturedCategory(string vertical, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(vertical))
                    throw new ArgumentException("Vertical is required to retrieve slotted featured category.", nameof(vertical));

                _logger.LogInformation("Fetching slotted featured category from state store...");

                // Determine index key
                string indexKey = vertical.ToLower() switch
                {
                    Verticals.Classifieds => FeaturedCategoryClassifiedIndex,
                    Verticals.Services => FeaturedCategoryServiceIndex,
                    _ => throw new ArgumentOutOfRangeException(nameof(vertical), $"Unsupported vertical: {vertical}")
                };

                var index = await _dapr.GetStateAsync<List<string>>(StoreName, indexKey, cancellationToken: cancellationToken)
                            ?? new List<string>();

                _logger.LogInformation("Index key '{IndexKey}' contains {Count} IDs", indexKey, index.Count);

                if (!index.Any())
                {
                    _logger.LogInformation("No featured category found in the index.");
                    return new List<FeaturedCategory>();
                }

                // Load all DTOs from index
                var stateTasks = index.Select(id =>
                    _dapr.GetStateAsync<FeaturedCategory>(StoreName, id, cancellationToken: cancellationToken)).ToList();

                var featuredCategories = await Task.WhenAll(stateTasks);
                
                var slottedFeaturedCategories = featuredCategories
                    .Where(p => p != null &&
                                p.IsActive == true &&
                                p.SlotOrder >= 1 && p.SlotOrder <= 6 &&
                                (p.EndDate == null || p.EndDate >= DateOnly.FromDateTime(DateTime.UtcNow)))
                    .OrderBy(p => p.SlotOrder)
                    .ToList();

                _logger.LogInformation("Retrieved {Count} active featured category for vertical: {Vertical}", slottedFeaturedCategories.Count, vertical);
                return slottedFeaturedCategories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch slotted featured category.");
                throw new InvalidOperationException("Error fetching slotted featured category.", ex);
            }
        }

        public async Task<List<FeaturedCategory>> GetFeaturedCategoriesByVertical(string vertical, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(vertical))
                throw new ArgumentException("Vertical is required to retrieve featured categories.", nameof(vertical));

            _logger.LogInformation("Fetching featured categories from state store...");

            string indexKey = vertical.ToLower() switch
            {
                Verticals.Classifieds => FeaturedCategoryClassifiedIndex,
                Verticals.Services => FeaturedCategoryServiceIndex,
                _ => throw new ArgumentOutOfRangeException(nameof(vertical), $"Unsupported vertical: {vertical}")
            };

            var index = await _dapr.GetStateAsync<List<string>>(StoreName, indexKey)
                        ?? new List<string>();

            if (!index.Any())
            {
                _logger.LogInformation("No featured categories found in the index.");
                return new List<FeaturedCategory>();
            }

            var stateTasks = index.Select(id =>
                _dapr.GetStateAsync<FeaturedCategory>(StoreName, id)).ToList();

            var featuredCategories = await Task.WhenAll(stateTasks);

            var activeFeaturedCategories = featuredCategories
                .Where(p => p != null && p.IsActive == true && (p.SlotOrder == null || p.SlotOrder < 1 || p.SlotOrder > 6) && (p.EndDate == null || p.EndDate > DateOnly.FromDateTime(DateTime.UtcNow)))
                .OrderByDescending(p => p.UpdatedAt)
                .ToList();

            _logger.LogInformation("Retrieved {Count} active featured categories for vertical: {Vertical}", activeFeaturedCategories.Count, vertical);

            return activeFeaturedCategories;
        }

        public async Task<string> ReorderFeaturedCategorySlots(string userId, LandingBoSlotReorderRequest request, CancellationToken cancellationToken = default)
        {
            const int MaxSlot = 6;

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId is required.");

            if (string.IsNullOrWhiteSpace(request.Vertical))
                throw new ArgumentException("Vertical is required.");

            if (request.SlotAssignments == null || request.SlotAssignments.Count != MaxSlot)
                throw new InvalidDataException($"Exactly {MaxSlot} slot assignments must be provided.");

            var slotNumbers = request.SlotAssignments.Select(sa => sa.SlotOrder).ToList();
            if (slotNumbers.Distinct().Count() != MaxSlot || slotNumbers.Any(s => s < 1 || s > MaxSlot))
                throw new InvalidDataException("SlotNumber must be unique and between 1 and 6.");

            string indexKey = request.Vertical.ToLower() switch
            {
                Verticals.Classifieds => FeaturedCategoryClassifiedIndex,
                Verticals.Services => FeaturedCategoryServiceIndex,
                _ => throw new ArgumentOutOfRangeException(nameof(request.Vertical), $"Unsupported vertical: {request.Vertical}")
            };

            var featuredCategoryIndex = await _dapr.GetStateAsync<List<string>>(StoreName, indexKey) ?? new();
            var loadedFeaturedCategories = new Dictionary<string, FeaturedCategory>();

            foreach (var assignment in request.SlotAssignments)
            {
                if (string.IsNullOrWhiteSpace(assignment.CategoryId))
                    continue;

                if (!featuredCategoryIndex.Contains(assignment.CategoryId))
                    continue;

                var featuredCategory = await _dapr.GetStateAsync<FeaturedCategory>(StoreName, assignment.CategoryId);
                if (featuredCategory == null)
                    throw new InvalidDataException($"Featured Category with ID '{assignment.CategoryId}' not found.");

                if (featuredCategory.UserId != userId)
                    throw new UnauthorizedAccessException("You are not authorized to update this Featured Category.");

                loadedFeaturedCategories[assignment.CategoryId] = featuredCategory;
            }

            foreach (var assignment in request.SlotAssignments)
            {
                var slotKey = $"classified-slot-{assignment.SlotOrder}";

                if (string.IsNullOrWhiteSpace(assignment.CategoryId))
                {
                    await _dapr.DeleteStateAsync(StoreName, slotKey, cancellationToken: cancellationToken);
                    continue;
                }

                var featuredCategory = loadedFeaturedCategories[assignment.CategoryId];
                featuredCategory.SlotOrder = assignment.SlotOrder;
                featuredCategory.UpdatedAt = DateTime.UtcNow;

                await _dapr.SaveStateAsync(StoreName, slotKey, featuredCategory);
                await _dapr.SaveStateAsync(StoreName, featuredCategory.Id.ToString(), featuredCategory);
            }

            return "Slots updated successfully.";
        }

        public async Task<string> ReplaceFeaturedCategorySlots(string userId, LandingBoSlotReplaceRequest dto, CancellationToken cancellationToken = default)
        {
            if (dto.TargetSlotId < 1 || dto.TargetSlotId > 6)
                throw new ArgumentOutOfRangeException(nameof(dto.TargetSlotId), "Slot must be between 1 and 6.");

            try
            {
                string indexKey = dto.Vertical.ToLower() switch
                {
                    Verticals.Classifieds => FeaturedCategoryClassifiedIndex,
                    Verticals.Services => FeaturedCategoryServiceIndex,
                    _ => throw new ArgumentOutOfRangeException(nameof(dto.Vertical), $"Unsupported vertical: {dto.Vertical}")
                };
                var index = await _dapr.GetStateAsync<List<string>>(StoreName, indexKey) ?? new List<string>();

                if (!index.Contains(dto.CategoryId.ToString()))
                    throw new InvalidOperationException("Selected featured category ID not found.");

                FeaturedCategory? newItem = null;

                foreach (var id in index)
                {
                    var item = await _dapr.GetStateAsync<FeaturedCategory>(StoreName, id);
                    if (item == null) continue;

                    // Case 1: Slot is currently occupied by someone else — clear it
                    if (item.SlotOrder == dto.TargetSlotId && item.Id.ToString() != dto.CategoryId)
                    {
                        item.SlotOrder = 0;
                        item.UpdatedAt = DateTime.UtcNow;
                        await _dapr.SaveStateAsync(StoreName, id, item);
                    }

                    // Case 2: The new pick is already slotted somewhere else — clear it before reassign
                    if (item.Id.ToString() == dto.CategoryId)
                    {
                        newItem = item;
                    }
                }

                if (newItem == null)
                    throw new InvalidOperationException("New featured category data not found in state.");

                // Update the selected pick with new slot
                newItem.SlotOrder = dto.TargetSlotId;
                newItem.UpdatedAt = DateTime.UtcNow;

                await _dapr.SaveStateAsync(StoreName, newItem.Id.ToString(), newItem);

                return $"Successfully replaced slot {dto.TargetSlotId} with category '{newItem.CategoryName}'.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error replacing slot {Slot} with category {CategoryId}", dto.TargetSlotId, dto.CategoryId);
                throw new InvalidOperationException("Failed to replace slot with selected category.", ex);
            }
        }
    }
}
