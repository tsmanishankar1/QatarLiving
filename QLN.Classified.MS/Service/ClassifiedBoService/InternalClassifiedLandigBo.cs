using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using Dapr.Client;
using Google.Apis.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using QLN.Classified.MS.Utilities;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.ClassifiedsBo;
using QLN.Common.DTO_s.ClassifiedsBoIndex;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.IService.ISubscriptionService;
using QLN.Common.Infrastructure.IService.V2IClassifiedBoService;
using QLN.Common.Infrastructure.QLDbContext;
using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Xml.Serialization;
using static QLN.Common.Infrastructure.Constants.ConstantValues;

namespace QLN.Content.MS.Service.ClassifiedBoService
{
    public class InternalClassifiedLandigBo : IClassifiedBoLandingService
    {
        private readonly Dapr.Client.DaprClient _dapr;
        private readonly ILogger<IClassifiedBoLandingService> _logger;
        private readonly IClassifiedService _classified;
        private readonly List<TransactionDto> _mockTransactions;
        private readonly List<PrelovedTransactionDto> _mockPrelovedTransactions;
        private readonly ClassifiedDevContext _context;
        private const string StoreName = ConstantValues.StateStoreNames.LandingBackOfficeStore;
        private const string ItemsIndexKey = ConstantValues.StateStoreNames.LandingBOIndex;
        private const string ItemsServiceIndexKey = ConstantValues.StateStoreNames.LandingServiceBOIndex;
        private const string ClassifiedsFeaturedStoresIndexKey = ConstantValues.StateStoreNames.FeaturedStoreClassifiedsIndexKey;
        private const string ServicesFeaturedStoresIndexKey = ConstantValues.StateStoreNames.FeaturedStoreServicesIndexKey;
        private const string FeaturedCategoryClassifiedIndex = ConstantValues.StateStoreNames.FeaturedCategoryClassifiedIndex;
        private const string FeaturedCategoryServiceIndex = ConstantValues.StateStoreNames.FeaturedCategoryServiceIndex;
        // private const string SubscriptionStoreName = ConstantValues.StateStoreNames.SubscriptionStores;
        private const string SubscriptionStoresIndexKey = ConstantValues.StateStoreNames.SubscriptionStoresIndexKey;


        public InternalClassifiedLandigBo(IClassifiedService classified, DaprClient dapr, ILogger<IClassifiedBoLandingService> logger, ClassifiedDevContext context)
        {
            _classified = classified;
            _dapr = dapr;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mockTransactions = GenerateMockTransactions();
            _mockPrelovedTransactions = GenerateMockPrelovedTransactions();            
            _context = context;
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
                    p.CategoryId.Equals(dto.CategoryId, StringComparison.OrdinalIgnoreCase) &&
                    p.Vertical?.Equals(dto.Vertical, StringComparison.OrdinalIgnoreCase) == true &&
                    (p.EndDate == null || p.EndDate >= today));

                if (duplicateExists)
                {
                    var message = $"A seasonal pick with the category '{dto.CategoryName}' already exists for vertical '{dto.Vertical}'.";
                    _logger.LogWarning(message);
                    throw new ConflictException(message);
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
            catch (ConflictException ex)
            {
                _logger.LogError(ex.Message, "Failed to post landing bo. Category: {Category}, User: {UserId} (409)", dto.CategoryName, userId);
                throw new ConflictException(ex.Message);

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

            var slotNumbers = request.SlotAssignments.Select(sa => sa.SlotOrder).ToList();
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
                var slotKey = $"seasonal-pick-slot-{assignment.SlotOrder}";

                if (string.IsNullOrWhiteSpace(assignment.PickId))
                {
                    await _dapr.DeleteStateAsync(StoreName, slotKey, cancellationToken: cancellationToken);
                    continue;
                }

                var pick = loadedPicks[assignment.PickId];
                pick.SlotOrder = assignment.SlotOrder;
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
            catch (Exception ex)
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
                    p.StoreId.Equals(dto.StoreId, StringComparison.OrdinalIgnoreCase) &&
                    p.Vertical?.Equals(dto.Vertical, StringComparison.OrdinalIgnoreCase) == true &&
                    (p.EndDate == null && p.EndDate >= today));

                if (duplicateExists)
                {
                    var message = $"A featured store with the name '{dto.StoreName}' already exists for vertical '{dto.Vertical}'.";
                    _logger.LogWarning(message);
                    throw new ConflictException(message);
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
            catch (ConflictException ex)
            {
                _logger.LogError(ex.Message, "Failed to post landing bo. Category: {Category}, User: {UserId} (409)", dto.StoreName, userId);
                throw new ConflictException(ex.Message);

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

            var slotNumbers = request.SlotAssignments.Select(sa => sa.SlotOrder).ToList();
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
                var slotKey = $"featured-store-slot-{assignment.SlotOrder}";

                if (string.IsNullOrWhiteSpace(assignment.StoreId))
                {
                    await _dapr.DeleteStateAsync(StoreName, slotKey, cancellationToken: cancellationToken);
                    continue;
                }

                var store = loadedStores[assignment.StoreId];
                store.SlotOrder = assignment.SlotOrder;
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
                    p.CategoryId.Equals(dto.CategoryId, StringComparison.OrdinalIgnoreCase) &&
                    p.Vertical?.Equals(dto.Vertical, StringComparison.OrdinalIgnoreCase) == true &&
                    (p.EndDate == null || p.EndDate >= today));

                if (duplicateExists)
                {
                    var message = $"A featured category '{dto.CategoryName}' already exists for vertical '{dto.Vertical}'.";
                    _logger.LogWarning(message);
                    throw new ConflictException(message);
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
            catch (ConflictException ex)
            {
                _logger.LogError(ex.Message, "Failed to post landing bo. Category: {Category}, User: {UserId} (409)", dto.CategoryName, userId);
                throw new ConflictException(ex.Message);

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
                .Where(p => p != null && p.IsActive == true && (p.SlotOrder == null || p.SlotOrder < 1 || p.SlotOrder > 6) &&
                (p.EndDate == null || p.EndDate >= DateOnly.FromDateTime(DateTime.UtcNow)))
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

        public async Task<string> BulkItemsAction(BulkActionRequest request, string userId, CancellationToken ct)
        {
            var indexKeys = await _dapr.GetStateAsync<List<string>>(
                ConstantValues.StateStoreNames.UnifiedStore,
                ConstantValues.StateStoreNames.ItemsIndexKey,
                cancellationToken: ct
            ) ?? new();
            Console.WriteLine(indexKeys);
            var updated = new List<ClassifiedsItems>();

            foreach (var id in request.AdIds)
            {
                var adKey = GetAdKey(id);
                if (!indexKeys.Contains(adKey.ToString()))
                {
                    continue;
                }

                var ad = await _dapr.GetStateAsync<ClassifiedsItems>(
                    ConstantValues.StateStoreNames.UnifiedStore,
                    adKey.ToString(),
                    cancellationToken: ct
                );

                if (ad is null)
                {
                    continue;
                }

                bool shouldUpdate = false;

                switch (request.Action)
                {
                    case BulkActionEnum.Approve:
                        if (ad.Status == AdStatus.PendingApproval)
                        {
                            ad.Status = AdStatus.Published;
                            shouldUpdate = true;
                        }
                        else
                        {
                            throw new InvalidOperationException($"Cannot approve ad with status '{ad.Status}'. Only 'PendingApproval' is allowed.");
                        }
                        break;

                    case BulkActionEnum.NeedChanges:
                        if (ad.Status == AdStatus.PendingApproval)
                        {
                            ad.Status = AdStatus.NeedsModification;
                            shouldUpdate = true;
                        }
                        else
                        {
                            throw new InvalidOperationException($"Cannot need changes ad with status '{ad.Status}'. Only 'PendingApproval' is allowed.");
                        }
                        break;

                    case BulkActionEnum.Publish:
                        if (ad.Status == AdStatus.Unpublished)
                        {
                            ad.Status = AdStatus.Published;
                            shouldUpdate = true;
                        }
                        else
                        {
                            throw new InvalidOperationException($"Cannot publish ad with status '{ad.Status}'. Only 'Unpublished' is allowed.");
                        }
                        break;

                    case BulkActionEnum.Unpublish:
                        if (ad.Status == AdStatus.Published)
                        {
                            ad.Status = AdStatus.Unpublished;
                            shouldUpdate = true;
                        }
                        else
                        {
                            throw new InvalidOperationException($"Cannot unpublish ad with status '{ad.Status}'. Only 'Published' is allowed.");
                        }
                        break;

                    case BulkActionEnum.UnPromote:
                        if (ad.IsPromoted)
                        {
                            ad.IsPromoted = false;
                            shouldUpdate = true;
                        }
                        else
                        {
                            throw new InvalidOperationException("Cannot unpromote an ad that is not promoted.");
                        }
                        break;

                    case BulkActionEnum.UnFeature:
                        if (ad.IsFeatured)
                        {
                            ad.IsFeatured = false;
                            shouldUpdate = true;
                        }
                        else
                        {
                            throw new InvalidOperationException("Cannot unfeature an ad that is not featured.");
                        }
                        break;

                    case BulkActionEnum.Promote:
                        if (!ad.IsPromoted)
                        {
                            ad.IsPromoted = true;
                            shouldUpdate = true;
                        }
                        else
                        {
                            throw new InvalidOperationException("Cannot promote an ad that is not unpromoted.");
                        }
                        break;

                    case BulkActionEnum.Feature:
                        if (!ad.IsFeatured)
                        {
                            ad.IsFeatured = true;
                            shouldUpdate = true;
                        }
                        else
                        {
                            throw new InvalidOperationException("Cannot feature an ad that is not unfeatured.");
                        }
                        break;

                    case BulkActionEnum.Remove:
                        ad.Status = AdStatus.Rejected;
                        shouldUpdate = true;
                        break;

                    default:
                        throw new InvalidOperationException("Invalid action");
                }

                if (shouldUpdate)
                {
                    ad.UpdatedAt = DateTime.UtcNow;
                    ad.UpdatedBy = userId;
                    await _dapr.SaveStateAsync(ConstantValues.StateStoreNames.UnifiedStore, adKey.ToString(), ad, cancellationToken: ct);
                    await IndexItemsToAzureSearch(ad, cancellationToken: ct);
                    updated.Add(ad);
                }
            }

            return "Action completed successfully";
        }

        public async Task<string> BulkCollectiblesAction(BulkActionRequest request, string userId, CancellationToken ct)
        {
            var indexKeys = await _dapr.GetStateAsync<List<string>>(
                ConstantValues.StateStoreNames.UnifiedStore,
                ConstantValues.StateStoreNames.CollectiblesIndexKey,
                cancellationToken: ct
            ) ?? new();
            var updated = new List<ClassifiedsCollectibles>();

            foreach (var id in request.AdIds)
            {
                var adKey = GetAdKey(id);
                if (!indexKeys.Contains(adKey.ToString()))
                {
                    continue;
                }

                var ad = await _dapr.GetStateAsync<ClassifiedsCollectibles>(
                    ConstantValues.StateStoreNames.UnifiedStore,
                    adKey.ToString(),
                    cancellationToken: ct
                );

                if (ad is null)
                {
                    continue;
                }

                bool shouldUpdate = false;

                switch (request.Action)
                {
                    case BulkActionEnum.Approve:
                        if (ad.Status == AdStatus.PendingApproval)
                        {
                            ad.Status = AdStatus.Published;
                            shouldUpdate = true;
                        }
                        else
                        {
                            throw new InvalidOperationException($"Cannot approve ad with status '{ad.Status}'. Only 'PendingApproval' is allowed.");
                        }
                        break;

                    case BulkActionEnum.NeedChanges:
                        if (ad.Status == AdStatus.PendingApproval)
                        {
                            ad.Status = AdStatus.NeedsModification;
                            shouldUpdate = true;
                        }
                        else
                        {
                            throw new InvalidOperationException($"Cannot need changes ad with status '{ad.Status}'. Only 'PendingApproval' is allowed.");
                        }
                        break;

                    case BulkActionEnum.Publish:
                        if (ad.Status == AdStatus.Unpublished)
                        {
                            ad.Status = AdStatus.Published;
                            shouldUpdate = true;
                        }
                        else
                        {
                            throw new InvalidOperationException($"Cannot publish ad with status '{ad.Status}'. Only 'Unpublished' is allowed.");
                        }
                        break;

                    case BulkActionEnum.Unpublish:
                        if (ad.Status == AdStatus.Published)
                        {
                            ad.Status = AdStatus.Unpublished;
                            shouldUpdate = true;
                        }
                        else
                        {
                            throw new InvalidOperationException($"Cannot unpublish ad with status '{ad.Status}'. Only 'Published' is allowed.");
                        }
                        break;

                    case BulkActionEnum.UnPromote:
                        if (ad.IsPromoted)
                        {
                            ad.IsPromoted = false;
                            shouldUpdate = true;
                        }
                        else
                        {
                            throw new InvalidOperationException("Cannot unpromote an ad that is not promoted.");
                        }
                        break;

                    case BulkActionEnum.UnFeature:
                        if (ad.IsFeatured)
                        {
                            ad.IsFeatured = false;
                            shouldUpdate = true;
                        }
                        else
                        {
                            throw new InvalidOperationException("Cannot unfeature an ad that is not featured.");
                        }
                        break;

                    case BulkActionEnum.Promote:
                        if (!ad.IsPromoted)
                        {
                            ad.IsPromoted = true;
                            shouldUpdate = true;
                        }
                        else
                        {
                            throw new InvalidOperationException("Cannot promote an ad that is not unpromoted.");
                        }
                        break;

                    case BulkActionEnum.Feature:
                        if (!ad.IsFeatured)
                        {
                            ad.IsFeatured = true;
                            shouldUpdate = true;
                        }
                        else
                        {
                            throw new InvalidOperationException("Cannot feature an ad that is not unfeatured.");
                        }
                        break;

                    case BulkActionEnum.Remove:
                        ad.Status = AdStatus.Rejected;
                        shouldUpdate = true;
                        break;

                    default:
                        throw new InvalidOperationException("Invalid action");
                }

                if (shouldUpdate)
                {
                    ad.UpdatedAt = DateTime.UtcNow;
                    ad.UpdatedBy = userId;
                    await _dapr.SaveStateAsync(ConstantValues.StateStoreNames.UnifiedStore, adKey.ToString(), ad, cancellationToken: ct);
                    await IndexCollectiblesToAzureSearch(ad, cancellationToken: ct);
                    updated.Add(ad);                }
            }

            return "Action completed successfully";
        }
        private string GetAdKey(Guid id) => $"ad-{id}";
        public async Task<TransactionListResponseDto> GetTransactionsAsync(
            TransactionFilterRequestDto request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting transactions. Page: {PageNumber}, Size: {PageSize}", request.PageNumber, request.PageSize);

                await Task.Delay(50, cancellationToken);

                var allTransactions = _mockTransactions.AsQueryable();

                if (!string.IsNullOrWhiteSpace(request.TransactionType))
                    allTransactions = allTransactions.Where(t => t.TransactionType.Equals(request.TransactionType, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(request.Status))
                    allTransactions = allTransactions.Where(t => t.Status.Equals(request.Status, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(request.PaymentMethod))
                    allTransactions = allTransactions.Where(t => t.PaymentMethod.Equals(request.PaymentMethod, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(request.DateCreated))
                    allTransactions = allTransactions.Where(t => t.CreationDate.Equals(request.DateCreated, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(request.DatePublished))
                    allTransactions = allTransactions.Where(t => t.PublishedDate.Equals(request.DatePublished, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(request.DateStart))
                    allTransactions = allTransactions.Where(t => t.StartDate.Equals(request.DateStart, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(request.DateEnd))
                    allTransactions = allTransactions.Where(t => t.EndDate.Equals(request.DateEnd, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(request.SearchText))
                {
                    var search = request.SearchText.ToLower();
                    allTransactions = allTransactions.Where(t =>
                        t.AdId.ToLower().Contains(search) ||
                        t.OrderId.ToLower().Contains(search) ||
                        t.Username.ToLower().Contains(search) ||
                        t.UserEmail.ToLower().Contains(search) ||
                        t.TransactionType.ToLower().Contains(search) ||
                        t.ProductType.ToLower().Contains(search) ||
                        t.Category.ToLower().Contains(search) ||
                        t.Status.ToLower().Contains(search) ||
                        t.Mobile.Contains(search) ||
                        t.PaymentMethod.ToLower().Contains(search) ||
                        t.Description.ToLower().Contains(search)
                    );
                }

                allTransactions = request.SortBy.ToLower() switch
                {
                    "amount" => request.SortOrder == "desc"
                        ? allTransactions.OrderByDescending(t => t.Amount)
                        : allTransactions.OrderBy(t => t.Amount),

                    "status" => request.SortOrder == "desc"
                        ? allTransactions.OrderByDescending(t => t.Status)
                        : allTransactions.OrderBy(t => t.Status),

                    "transactiontype" => request.SortOrder == "desc"
                        ? allTransactions.OrderByDescending(t => t.TransactionType)
                        : allTransactions.OrderBy(t => t.TransactionType),

                    _ => request.SortOrder == "desc"
                        ? allTransactions.OrderByDescending(t => ParseDate(t.CreationDate))
                        : allTransactions.OrderBy(t => ParseDate(t.CreationDate))
                };

                var totalRecords = allTransactions.Count();
                var totalPages = (int)Math.Ceiling((double)totalRecords / request.PageSize);

                var paginated = allTransactions
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                return new TransactionListResponseDto
                {
                    Records = paginated,
                    TotalRecords = totalRecords,
                    CurrentPage = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = totalPages
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get transactions");
                throw;
            }
        }

        private DateTime ParseDate(string dateString)
        {
            try
            {
                return DateTime.ParseExact(dateString, "dd-MM-yyyy", null);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private List<TransactionDto> GenerateMockTransactions()
        {
            var random = new Random();
            var users = new[] { "john_doe", "jane_smith", "bob_wilson", "alice_brown", "charlie_davis" };
            var emails = new[] { "john@example.com", "jane@example.com", "bob@example.com", "alice@example.com", "charlie@example.com" };
            var categories = new[] { "Electronics", "Vehicles", "Real Estate", "Jobs", "Services", "Fashion" };
            var productTypes = new[] { "Phone", "Car", "Apartment", "Furniture", "Laptop", "Clothing" };
            var transactionTypes = new[] { "Pay To Publish", "Pay To Promote", "Pay To Feature", "Bulk Refresh" };
            var statuses = new[] { "Active", "Pending Approval", "Expired", "Unpublished", "Awaits" };
            var paymentMethods = new[] { "Credit Card", "PayPal", "Bank Transfer", "Digital Wallet" };

            var transactions = new List<TransactionDto>();

            for (int i = 1; i <= 100; i++)
            {
                var userIndex = random.Next(users.Length);
                var transactionType = transactionTypes[random.Next(transactionTypes.Length)];
                var createdDate = DateTime.UtcNow.AddDays(-random.Next(90));
                var startDate = createdDate.AddDays(random.Next(0, 5));
                var endDate = startDate.AddDays(random.Next(30, 90));

                // Some transactions may not have published/start/end dates
                var hasPublishedDate = random.Next(100) > 20; // 80% have published date
                var hasStartDate = random.Next(100) > 30; // 70% have start date
                var hasEndDate = random.Next(100) > 40; // 60% have end date

                transactions.Add(new TransactionDto
                {
                    Id = $"txn_{i:D6}",
                    AdId = $"{random.Next(21430, 21440)}",
                    OrderId = $"{random.Next(21400, 21500)}",
                    UserId = $"usr_{userIndex + 1}",
                    Username = users[userIndex],
                    UserEmail = emails[userIndex],
                    TransactionType = transactionType,
                    ProductType = productTypes[random.Next(productTypes.Length)],
                    Category = categories[random.Next(categories.Length)],
                    Status = statuses[random.Next(statuses.Length)],
                    Email = emails[userIndex],
                    Mobile = $"+974 {random.Next(1000, 9999)} {random.Next(1000, 9999)}",
                    Whatsapp = $"+974 {random.Next(1000, 9999)} {random.Next(1000, 9999)}",
                    Account = "User-account@gmail.com",
                    CreationDate = createdDate.ToString("dd-MM-yyyy"),
                    PublishedDate = hasPublishedDate ? createdDate.AddHours(random.Next(1, 24)).ToString("dd-MM-yyyy") : "",
                    StartDate = hasStartDate ? startDate.ToString("dd-MM-yyyy") : "",
                    EndDate = hasEndDate ? endDate.ToString("dd-MM-yyyy") : "",
                    Amount = GetAmountByType(transactionType, random),
                    PaymentMethod = paymentMethods[random.Next(paymentMethods.Length)],
                    Description = GetDescriptionByType(transactionType)
                });
            }

            return transactions.OrderByDescending(t => ParseDate(t.CreationDate)).ToList();
        }

        private static decimal GetAmountByType(string type, Random random) => type switch
        {
            "Pay To Publish" => random.Next(10, 60),
            "Pay To Promote" => random.Next(25, 125),
            "Pay To Feature" => random.Next(50, 250),
            "Bulk Refresh" => random.Next(100, 400),
            _ => 25
        };

        private static string GetDescriptionByType(string type) => type switch
        {
            "Pay To Publish" => "Payment for ad publication",
            "Pay To Promote" => "Payment for ad promotion",
            "Pay To Feature" => "Payment for featured listing",
            "Bulk Refresh" => "Bulk refresh payment",
            _ => "Transaction"
        };


        public async Task<PaginatedResult<PrelovedAdPaymentSummaryDto>> GetAllPrelovedAdPaymentSummaries(int? pageNumber = 1, int? pageSize = 12, string? search = null,
            string? sortBy = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = new List<PrelovedAdPaymentSummaryDto>();

                var keys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.StateStoreNames.UnifiedStore,
                    ConstantValues.StateStoreNames.PrelovedIndexKey,
                    cancellationToken: cancellationToken) ?? new();

                _logger.LogInformation("Fetched {Count} ad keys from index.", keys.Count);

                foreach (var key in keys)
                {
                    var ad = await _dapr.GetStateAsync<ClassifiedsPreloved>(
                        ConstantValues.StateStoreNames.UnifiedStore,
                        key,
                        cancellationToken: cancellationToken);

                    if (ad == null)
                    {
                        continue;
                    }

                    var dto = new PrelovedAdPaymentSummaryDto
                    {
                        AdId = ad.Id,
                        SubscriptionType = "12 Months Super",
                        UserName = ad.UserName,
                        EmailAddress = ad.ContactEmail,
                        Mobile = ad.ContactNumber,
                        WhatsappNumber = ad.WhatsAppNumber,
                        Amount = 250,
                        Status = ad.Status.ToString(),
                        StartDate = ad.PublishedDate.HasValue && ad.PublishedDate.Value != DateTime.MinValue
                        ? ad.PublishedDate.Value.ToString("dd-MM-yyyy hh:mmtt")
                        : "N/A",
                        EndDate = ad.ExpiryDate.HasValue && ad.ExpiryDate.Value != DateTime.MinValue
                        ? ad.ExpiryDate.Value.ToString("dd-MM-yyyy hh:mmtt")
                        : "N/A",

                        OrderId = ad.Id.ToString().Substring(0, 6)
                    };

                    if (string.IsNullOrWhiteSpace(search) ||
                        dto.AdId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) == true ||
                        dto.UserName?.Contains(search, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        result.Add(dto);
                    }
                }
                _logger.LogInformation("Total matched ads after filtering: {Count}", result.Count);
                result = sortBy?.ToLower() switch
                {
                    "startdate" => result.OrderBy(x => x.StartDate).ToList(),
                    "enddate" => result.OrderBy(x => x.EndDate).ToList(),
                    _ => result.OrderByDescending(x => x.StartDate).ToList()
                };

                var totalCount = result.Count;
                int currentPage = pageNumber ?? 1;
                int currentSize = pageSize ?? 12;

                var paginatedItems = result
                    .Skip((currentPage - 1) * currentSize)
                    .Take(currentSize)
                    .ToList();
                _logger.LogInformation("Returning {Count} items for page {Page} (pageSize={Size})", paginatedItems.Count, currentPage, currentSize);
                return new PaginatedResult<PrelovedAdPaymentSummaryDto>
                {
                    TotalCount = totalCount,
                    PageNumber = currentPage,
                    PageSize = currentSize,
                    Items = paginatedItems
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching preloved ad payment summaries.");
                throw new InvalidOperationException("Failed to fetch preloved ad payment summaries.", ex);
            }
        }

        public async Task<PaginatedResult<PrelovedAdSummaryDto>> GetAllPrelovedBoAds(
            string? sortBy = "CreationDate",
            string? search = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            DateTime? publishedFrom = null,
            DateTime? publishedTo = null,
            int? status = null,
            bool? isFeatured = null,
            bool? isPromoted = null,
            int pageNumber = 1,
            int pageSize = 12,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var keys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.StateStoreNames.UnifiedStore,
                    ConstantValues.StateStoreNames.PrelovedIndexKey,
                    cancellationToken: cancellationToken) ?? new();

                var ads = new List<PrelovedAdSummaryDto>();

                foreach (var key in keys)
                {
                    var ad = await _dapr.GetStateAsync<ClassifiedsPreloved>(
                        ConstantValues.StateStoreNames.UnifiedStore,
                        key,
                        cancellationToken: cancellationToken);

                    if (ad == null) continue;

                    ads.Add(new PrelovedAdSummaryDto
                    {
                        Id = ad.Id,
                        UserId = ad.UserId,
                        UserName = ad.UserName,
                        AdTitle = ad.Title,
                        Category = ad.Category,
                        SubCategory = ad.L1Category,
                        Status = ad.Status.ToString(),
                        IsFeatured = ad.IsFeatured,
                        IsPromoted = ad.IsPromoted,
                        CreationDate = ad.CreatedAt,
                        DatePublished = ad.PublishedDate,
                        DateExpiry = ad.ExpiryDate,
                        ImageUpload = ad.Images?.Select(img => new ImageDto
                        {
                            Url = img.Url,
                        }).ToList(),
                        OrderId = ad.Id.ToString().Substring(0, 6)
                    });
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var lowerSearch = search.ToLowerInvariant();
                    ads = ads.Where(ad =>
                        (!string.IsNullOrEmpty(ad.AdTitle) && ad.AdTitle.ToLowerInvariant().Contains(lowerSearch)) ||
                        ad.Id.ToString().Contains(lowerSearch) ||
                        (!string.IsNullOrEmpty(ad.UserId) && ad.UserId.ToLowerInvariant().Contains(lowerSearch)) ||
                        (!string.IsNullOrEmpty(ad.UserName) && ad.UserName.ToLowerInvariant().Contains(lowerSearch))
                    ).ToList();
                }

                if (fromDate.HasValue)
                    ads = ads.Where(x => x.CreationDate >= fromDate.Value).ToList();

                if (toDate.HasValue)
                    ads = ads.Where(x => x.CreationDate <= toDate.Value).ToList();

                if (publishedFrom.HasValue)
                    ads = ads.Where(x => x.DatePublished.HasValue && x.DatePublished >= publishedFrom.Value).ToList();

                if (publishedTo.HasValue)
                    ads = ads.Where(x => x.DatePublished.HasValue && x.DatePublished <= publishedTo.Value).ToList();

                if (status.HasValue && Enum.IsDefined(typeof(AdStatus), status.Value))
                {
                    var statusEnum = ((AdStatus)status.Value).ToString();
                    ads = ads.Where(x => x.Status != null && x.Status.Equals(statusEnum, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (isFeatured.HasValue)
                    ads = ads.Where(x => x.IsFeatured == isFeatured.Value).ToList();

                if (isPromoted.HasValue)
                    ads = ads.Where(x => x.IsPromoted == isPromoted.Value).ToList();

                sortBy = sortBy?.ToLowerInvariant();
                ads = sortBy switch
                {
                    "title" => ads.OrderByDescending(x => x.AdTitle).ToList(),
                    "username" => ads.OrderByDescending(x => x.UserName).ToList(),
                    "status" => ads.OrderByDescending(x => x.Status).ToList(),
                    "published" => ads.OrderByDescending(x => x.DatePublished).ToList(),
                    _ => ads.OrderByDescending(x => x.CreationDate).ToList()
                };

                // Paginate
                var totalCount = ads.Count;
                var pagedItems = ads
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return new PaginatedResult<PrelovedAdSummaryDto>
                {
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    Items = pagedItems
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching preloved ads.");
                throw new InvalidOperationException("Failed to fetch preloved ads.", ex);
            }
        }

        public async Task<PaginatedResult<DealsAdSummaryDto>> GetAllDeals(int? pageNumber = 1, int? pageSize = 12, string? search = null,
    string? sortBy = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = new List<DealsAdSummaryDto>();

                var keys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.StateStoreNames.UnifiedStore,
                    ConstantValues.StateStoreNames.DealsIndexKey,
                    cancellationToken: cancellationToken) ?? new();


                foreach (var key in keys)
                {
                    var ad = await _dapr.GetStateAsync<ClassifiedsDeals>(
                        ConstantValues.StateStoreNames.UnifiedStore,
                        key,
                        cancellationToken: cancellationToken);

                    if (ad == null) continue;

                    var dto = new DealsAdSummaryDto
                    {
                        AdId = ad.Id,
                        subscriptiontype = "12 Months Super",
                        createdby = ad.CreatedBy,
                        ContactNumber = ad.ContactNumber,
                        WhatsappNumber = ad.WhatsappNumber,
                        price = "250",
                        status = ad.IsActive.ToString(),
                        WhatsAppLeads = "12",
                        PhoneLeads = "14",
                        StartDate = ad.UpdatedAt.ToString(),                        
                        EndDate = ad.ExpiryDate.ToString(),
                        orderid = ad.Id.ToString().Substring(0, 6)
                    };

                    if (string.IsNullOrWhiteSpace(search) ||
                        dto.AdId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) == true ||
                       dto.createdby?.Contains(search, StringComparison.OrdinalIgnoreCase) == true)

                    {
                        result.Add(dto);
                    }
                }
                result = sortBy?.ToLower() switch
                {
                    "startdate" => result.OrderBy(x => x.StartDate).ToList(),
                    "enddate" => result.OrderBy(x => x.EndDate).ToList(),
                    _ => result.OrderByDescending(x => x.StartDate).ToList()
                };

                var totalCount = result.Count;
                int currentPage = pageNumber ?? 1;
                int currentSize = pageSize ?? 12;

                var paginatedItems = result
                    .Skip((currentPage - 1) * currentSize)
                    .Take(currentSize)
                    .ToList();

                return new PaginatedResult<DealsAdSummaryDto>
                {
                    TotalCount = totalCount,
                    PageNumber = currentPage,
                    PageSize = currentSize,
                    Items = paginatedItems
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching preloved ad payment summaries.");
                throw new InvalidOperationException("Failed to fetch preloved ad payment summaries.", ex);
            }
        }

        public async Task<PaginatedResult<DealsViewSummaryDto>> DealsViewSummary(
            int? pageNumber = 1,
            int? pageSize = 12,
            string? search = null,
            string? sortBy = null, string? status = null,
            bool? isPromoted = null,
            bool? isFeatured = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = new List<DealsViewSummaryDto>();

                var keys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.StateStoreNames.UnifiedStore,
                    ConstantValues.StateStoreNames.DealsIndexKey,
                    cancellationToken: cancellationToken) ?? new();

                foreach (var key in keys)
                {
                    var ad = await _dapr.GetStateAsync<ClassifiedsDeals>(
                        ConstantValues.StateStoreNames.UnifiedStore,
                        key,
                        cancellationToken: cancellationToken);

                    if (ad == null) continue;

                    if (!ad.IsActive)
                    {
                        continue;
                    }

                    if (isPromoted.HasValue && ad.IsPromoted != isPromoted.Value)
                    {
                        continue;
                    }

                    if (isFeatured.HasValue && ad.IsFeatured != isFeatured.Value)
                    {
                        continue;
                    }
                    var dto = new DealsViewSummaryDto
                    {
                        AdId = ad.Id,
                        Dealtitle = ad.Title,
                        subscriptiontype = "12 Months Super",
                        DateCreated = ad.CreatedAt,
                        createdby = ad.CreatedBy,
                        ContactNumber = ad.ContactNumber,
                        WhatsappNumber = ad.WhatsappNumber,
                        StartDate = ad.UpdatedAt ?? DateTime.UtcNow,
                        EndDate = ad.ExpiryDate,
                        WebClick = 2,
                        Weburl = "linkup.com",
                        Location = ad.Locations,
                        Views = 3,
                        Impression = 5,
                        Phonelead = 4
                    };


                    if (string.IsNullOrWhiteSpace(search) ||

                        dto.AdId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) == true ||

                        dto.createdby?.Contains(search, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        result.Add(dto);
                    }
                }


                result = sortBy?.ToLower() switch
                {
                    "startdate" => result.OrderBy(x => x.StartDate).ToList(),
                    "enddate" => result.OrderBy(x => x.EndDate).ToList(),
                    _ => result.OrderByDescending(x => x.StartDate).ToList()
                };


                var totalCount = result.Count;

                int currentPage = pageNumber ?? 1;

                int currentSize = pageSize ?? 12;

                var paginatedItems = result
                    .Skip((currentPage - 1) * currentSize)
                    .Take(currentSize)
                    .ToList();
                return new PaginatedResult<DealsViewSummaryDto>
                {
                    TotalCount = totalCount,
                    PageNumber = currentPage,
                    PageSize = currentSize,
                    Items = paginatedItems
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching preloved ad payment summaries.");

                throw new InvalidOperationException("Failed to fetch preloved ad payment summaries.", ex);

            }

        }

        public async Task<string> SoftDeleteDeals(DealsBulkDelete dto, string userId, CancellationToken cancellationToken = default)
        {
            if (dto.AdId == null || dto.AdId.Count == 0)
            {
                _logger.LogWarning("Soft delete aborted: No Ad IDs provided. UserId: {UserId}", userId);
                throw new ArgumentException("At least one Ad ID must be provided.", nameof(dto.AdId));
            }

            _logger.LogInformation("Soft delete requested for {Count} deals. UserId: {UserId}", dto.AdId.Count, userId);


            var indexKey = ConstantValues.StateStoreNames.DealsIndexKey;
            var index = await _dapr.GetStateAsync<List<string>>(ConstantValues.StateStoreNames.UnifiedStore, indexKey) ?? new();

            if (index.Count == 0)
            {
                _logger.LogWarning("Deals index is empty. Nothing to soft delete.");
                return "No deals found in index.";
            }

            var failedDeletes = new List<string>();
            var deletedDeals = new List<string>();

            foreach (var key in index)
            {
                try
                {
                    var deal = await _dapr.GetStateAsync<ClassifiedsDeals>(
                        ConstantValues.StateStoreNames.UnifiedStore,
                        key,
                        cancellationToken: cancellationToken);

                    if (deal == null)
                    {
                        _logger.LogWarning("Deal not found for key: {Key}", key);
                        continue;
                    }

                    if (!dto.AdId.Contains(deal.Id.ToString()))
                    {
                        continue;
                    }

                    if (!deal.IsActive)
                    {
                        _logger.LogInformation("Deal already inactive. AdId: {AdId}, skipping.", deal.Id);
                        continue;
                    }

                    deal.IsActive = false;
                    deal.UpdatedAt = DateTime.UtcNow;
                    deal.UpdatedBy = userId;

                    await _dapr.SaveStateAsync(ConstantValues.StateStoreNames.UnifiedStore, key, deal, cancellationToken: cancellationToken);

                    _logger.LogInformation("Soft deleted deal: {AdId}", deal.Id);
                    deletedDeals.Add(deal.Id.ToString());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to soft delete deal with key: {Key}", key);
                    failedDeletes.Add(key);
                }
            }

            _logger.LogInformation("Soft delete operation completed. Total Requested: {Total}, Deleted: {Deleted}, Failed: {Failed}, UserId: {UserId}",
                dto.AdId.Count, deletedDeals.Count, failedDeletes.Count, userId);

            return $"Soft delete completed. Deleted: {deletedDeals.Count}, Failed: {failedDeletes.Count}.";
        }

        public async Task<string> BulkPrelovedAction(BulkActionRequest request, string userId, CancellationToken ct)
        {
            try
            {
                var indexKeys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.StateStoreNames.UnifiedStore,
                    ConstantValues.StateStoreNames.PrelovedIndexKey,
                    cancellationToken: ct
                    ) ?? new();
                var updated = new List<ClassifiedsPreloved>();

                foreach (var id in request.AdIds)
                {
                    var adKey = GetAdKey(id);
                    if (!indexKeys.Contains(adKey.ToString()))
                    {
                        continue;
                    }

                    var ad = await _dapr.GetStateAsync<ClassifiedsPreloved>(
                        ConstantValues.StateStoreNames.UnifiedStore,
                        adKey.ToString(),
                        cancellationToken: ct
                    );

                    if (ad is null)
                    {
                        continue;
                    }

                    bool shouldUpdate = false;

                    switch (request.Action)
                    {
                        case BulkActionEnum.Approve:
                            if (ad.Status == AdStatus.PendingApproval)
                            {
                                ad.Status = AdStatus.Published;
                                shouldUpdate = true;
                            }
                            else
                            {
                                throw new InvalidOperationException($"Cannot approve ad with status '{ad.Status}'. Only 'PendingApproval' is allowed.");
                            }
                            break;

                        case BulkActionEnum.NeedChanges:
                            if (ad.Status == AdStatus.PendingApproval)
                            {
                                ad.Status = AdStatus.NeedsModification;
                                shouldUpdate = true;
                            }
                            else
                            {
                                throw new InvalidOperationException($"Cannot need changes ad with status '{ad.Status}'. Only 'PendingApproval' is allowed.");
                            }
                            break;

                        case BulkActionEnum.Publish:
                            if (ad.Status == AdStatus.Unpublished)
                            {
                                ad.Status = AdStatus.Published;
                                shouldUpdate = true;
                            }
                            else
                            {
                                throw new InvalidOperationException($"Cannot publish ad with status '{ad.Status}'. Only 'Unpublished' is allowed.");
                            }
                            break;

                        case BulkActionEnum.Unpublish:
                            if (ad.Status == AdStatus.Published)
                            {
                                ad.Status = AdStatus.Unpublished;
                                shouldUpdate = true;
                            }
                            else
                            {
                                throw new InvalidOperationException($"Cannot unpublish ad with status '{ad.Status}'. Only 'Published' is allowed.");
                            }
                            break;

                        case BulkActionEnum.UnPromote:
                            if (ad.IsPromoted)
                            {
                                ad.IsPromoted = false;
                                shouldUpdate = true;
                            }
                            else
                            {
                                throw new InvalidOperationException("Cannot unpromote an ad that is not promoted.");
                            }
                            break;

                        case BulkActionEnum.UnFeature:
                            if (ad.IsFeatured)
                            {
                                ad.IsFeatured = false;
                                shouldUpdate = true;
                            }
                            else
                            {
                                throw new InvalidOperationException("Cannot unfeature an ad that is not featured.");
                            }
                            break;

                        case BulkActionEnum.Promote:
                            if (!ad.IsPromoted)
                            {
                                ad.IsPromoted = true;
                                shouldUpdate = true;
                            }
                            else
                            {
                                throw new InvalidOperationException("Cannot promote an ad that is not unpromoted.");
                            }
                            break;

                        case BulkActionEnum.Feature:
                            if (!ad.IsFeatured)
                            {
                                ad.IsFeatured = true;
                                shouldUpdate = true;
                            }
                            else
                            {
                                throw new InvalidOperationException("Cannot feature an ad that is not unfeatured.");
                            }
                            break;


                        case BulkActionEnum.Remove:
                            ad.Status = AdStatus.Rejected;
                            shouldUpdate = true;
                            break;

                        default:
                            throw new InvalidOperationException("Invalid action");
                    }

                    if (shouldUpdate)
                    {
                        ad.UpdatedAt = DateTime.UtcNow;
                        ad.UpdatedBy = userId;
                        await _dapr.SaveStateAsync(ConstantValues.StateStoreNames.UnifiedStore, adKey.ToString(), ad, cancellationToken: ct);
                        await IndexPrelovedToAzureSearch(ad, cancellationToken: ct);
                        updated.Add(ad);
                    }
                }
                return "Action completed successfully";
            }
            catch (ConflictException ex)
            {
                throw new ConflictException(ex.Message);

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<PrelovedTransactionListResponseDto> GetPrelovedTransactionsAsync(
            int pageNumber,
            int pageSize,
            string? searchText,
            string? dateCreated,
            string? datePublished,
            string? dateStart,
            string? dateEnd,
            string? status,
            string sortBy,
            string sortOrder,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Getting transactions. Page: {PageNumber}, Size: {PageSize}", pageNumber, pageSize);

                await Task.Delay(50, cancellationToken);

                var allTransactions = _mockPrelovedTransactions.AsQueryable();


                if (!string.IsNullOrWhiteSpace(status))
                {
                    allTransactions = allTransactions.Where(t =>
                    t.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(dateCreated))
                {
                    allTransactions = allTransactions.Where(t =>
                    t.CreationDate.Equals(dateCreated, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(datePublished))
                {
                    allTransactions = allTransactions.Where(t =>
                    t.PublishedDate.Equals(datePublished, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(dateStart))
                {
                    allTransactions = allTransactions.Where(t =>
                    t.StartDate.Equals(dateStart, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(dateEnd))
                {
                    allTransactions = allTransactions.Where(t =>
                    t.EndDate.Equals(dateEnd, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var search = searchText.ToLower();
                    allTransactions = allTransactions.Where(t =>
                    t.AdId.ToLower().Contains(search) ||
                    t.OrderId.ToLower().Contains(search) ||
                    t.Username.ToLower().Contains(search) ||
                    t.Email.ToLower().Contains(search) ||
                    t.Status.ToLower().Contains(search) ||
                    t.Mobile.Contains(search)
                    );
                }

                allTransactions = sortBy.ToLower() switch
                {
                    "amount" => sortOrder == "desc" ?
                   allTransactions.OrderByDescending(t => t.Amount) :
                   allTransactions.OrderBy(t => t.Amount),
                    "status" => sortOrder == "desc" ?
                   allTransactions.OrderByDescending(t => t.Status) :
                   allTransactions.OrderBy(t => t.Status),
                    _ => sortOrder == "desc" ?
                    allTransactions.OrderByDescending(t => ParseDate(t.CreationDate)) :
                    allTransactions.OrderBy(t => ParseDate(t.CreationDate))
                };

                var totalRecords = allTransactions.Count();
                var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

                // Pagination
                var paginatedTransactions = allTransactions
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                _logger.LogInformation("Returning {Count} transactions out of {Total} total records",
                paginatedTransactions.Count, totalRecords);

                return new PrelovedTransactionListResponseDto
                {
                    Records = paginatedTransactions,
                    TotalCount = totalRecords,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get transactions");
                throw;
            }
        }

        private List<PrelovedTransactionDto> GenerateMockPrelovedTransactions()
        {
            var transactions = new List<PrelovedTransactionDto>();
            var random = new Random();
            var statuses = new[] { "Published", "Unpublished", "Pending For Approval" };
            var subscriptionTypes = new[] { "P2P", "Standard", "Premium" };
            for (int i = 1; i <= 100; i++)
            {
                var creationDate = DateTime.Now.AddDays(-random.Next(1, 60));
                var publishedDate = creationDate.AddDays(1);
                var startDate = publishedDate.AddDays(1);
                var endDate = startDate.AddDays(random.Next(10, 30));

                transactions.Add(new PrelovedTransactionDto
                {
                    Id = $"txn_{i:D6}",
                    AdId = random.Next(20000, 22000).ToString(),
                    OrderId = random.Next(21000, 22000).ToString(),
                    SubscriptionType = subscriptionTypes[random.Next(subscriptionTypes.Length)],
                    UserId = $"usr_{random.Next(1, 5)}",
                    Username = $"user_{i}",
                    Email = $"user{i}@example.com",
                    Mobile = $"+974 {random.Next(5000, 5999)} {random.Next(1000, 9999)}",
                    Whatsapp = $"+974 {random.Next(3000, 3999)} {random.Next(1000, 9999)}",
                    Amount = random.Next(100, 150),
                    Status = statuses[random.Next(statuses.Length)],
                    CreationDate = creationDate.ToString("dd-MM-yyyy"),
                    PublishedDate = publishedDate.ToString("dd-MM-yyyy"),
                    StartDate = startDate.ToString("dd-MM-yyyy"),
                    EndDate = endDate.ToString("dd-MM-yyyy"),
                    Views = random.Next(1, 500),
                    MobileCount = random.Next(1, 100),
                    WhatsappCount = random.Next(1, 100)
                });

            }

            return transactions.OrderByDescending(t => ParseDate(t.CreationDate)).ToList();

        }



        public async Task<List<StoresSubscriptionDto>> getStoreSubscriptions(string? subscriptionType, string? filterDate, CancellationToken cancellationToken = default)
        {
            try
            {



                DateTime filterDateParsed;
                try
                {
                    if (string.IsNullOrEmpty(filterDate))
                    {
                        filterDateParsed = DateTime.UtcNow;
                    }
                    else if (!DateTime.TryParse(filterDate, out filterDateParsed))
                    {
                        _logger.LogWarning("Invalid filterDate format provided: {FilterDate}. Using current UTC date instead.", filterDate);
                        filterDateParsed = DateTime.UtcNow;
                    }
                }
                catch (FormatException formatEx)
                {
                    _logger.LogError(formatEx, "Failed to parse filterDate. Value: {FilterDate}", filterDate);
                    throw;
                }
                var dateThreshold = filterDateParsed.AddDays(-90);
                var filtered = await _context.StoresSubscriptions
     .AsNoTracking()
     .Where(x =>
         (string.IsNullOrEmpty(subscriptionType) || x.SubscriptionType == subscriptionType) &&
         x.StartDate >= dateThreshold &&
         x.StartDate <= filterDateParsed)
     .ToListAsync(cancellationToken);

                return filtered;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch stores subscriptions.");
                throw;
            }
        }
        public async Task<string> CreateStoreSubscriptions(StoresSubscriptionDto dto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("create store subscriptions");
            try
            {

                _context.StoresSubscriptions.Add(dto);
                await _context.SaveChangesAsync();

                return "Store Subscription Created successfully";
            }
            catch (ArgumentException ex)
            {
                throw new InvalidDataException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating stores subscriptions.");
                throw;
            }
        }
        public async Task<string> EditStoreSubscriptions(int OrderID, string Status, CancellationToken cancellationToken = default)
        {
            try
            {
                var subscription = await _context.StoresSubscriptions
             .FirstOrDefaultAsync(x => x.OrderId == OrderID, cancellationToken);

                if (subscription == null)
                {
                    return "Subscription not found.";
                }

                subscription.Status = Status;
                _context.StoresSubscriptions.Update(subscription);
                await _context.SaveChangesAsync(cancellationToken);

                return "Subscription status updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit stores subscriptions.");
                throw;
            }
        }
        public async Task<ClassifiedsBoItemsResponseDto> GetAllItems(GetAllSearch request, CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Starting GetAllItems processing for request: {Request}",
                    System.Text.Json.JsonSerializer.Serialize(request));

                var indexKeys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.StateStoreNames.UnifiedStore,
                    ConstantValues.StateStoreNames.ItemsIndexKey,
                    cancellationToken: ct
                ) ?? new List<string>();

                _logger.LogInformation("Found {Count} index keys", indexKeys.Count);

                if (!indexKeys.Any())
                {
                    return new ClassifiedsBoItemsResponseDto
                    {
                        TotalCount = 0,
                        ClassifiedsItems = new List<ClassifiedsItems>()
                    };
                }

                var items = new List<ClassifiedsItems>();
                var failedKeys = new List<string>();

                foreach (var key in indexKeys)
                {
                    try
                    {
                        ct.ThrowIfCancellationRequested();

                        var dto = await _dapr.GetStateAsync<ClassifiedsItems>(
                            ConstantValues.StateStoreNames.UnifiedStore,
                            key,
                            cancellationToken: ct);

                        if (dto != null)
                        {
                            items.Add(dto);
                        }
                        else
                        {
                            _logger.LogWarning("Item with key {Key} returned null", key);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to retrieve item with key {Key}", key);
                        failedKeys.Add(key);
                    }
                }

                if (!string.IsNullOrEmpty(request.Text) && request.Text.Trim() != "*")
                {
                    items = items.Where(item =>
                        (item.Title?.Contains(request.Text, StringComparison.OrdinalIgnoreCase) == true) ||
                        (item.UserId?.Contains(request.Text, StringComparison.OrdinalIgnoreCase) == true)
                    ).ToList();

                    _logger.LogInformation("Applied text filter '{Text}', resulting count: {Count}", request.Text, items.Count);
                }

                if (request.IsFeatured.HasValue)
                {
                    var originalCount = items.Count;
                    items = items.Where(item =>
                    {
                        try
                        {
                            var prop = item.GetType().GetProperty(nameof(request.IsFeatured), BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                            if (prop == null) return false;

                            var value = prop.GetValue(item) as bool?;
                            return value == request.IsFeatured;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error applying IsFeatured filter to item");
                            return false;
                        }
                    }).ToList();

                    _logger.LogInformation("Filter 'IsFeatured={IsFeatured}' reduced items from {Original} to {Filtered}",
                        request.IsFeatured, originalCount, items.Count);
                }

                if (request.IsPromoted.HasValue)
                {
                    var originalCount = items.Count;
                    items = items.Where(item =>
                    {
                        try
                        {
                            var prop = item.GetType().GetProperty(nameof(request.IsPromoted), BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                            if (prop == null) return false;

                            var value = prop.GetValue(item) as bool?;
                            return value == request.IsPromoted;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error applying IsPromoted filter to item");
                            return false;
                        }
                    }).ToList();

                    _logger.LogInformation("Filter 'IsPromoted={IsPromoted}' reduced items from {Original} to {Filtered}",
                        request.IsPromoted, originalCount, items.Count);
                }

                if (request.Status.HasValue)
                {
                    var originalCount = items.Count;
                    items = items.Where(item =>
                    {
                        try
                        {
                            var prop = item.GetType().GetProperty(nameof(request.Status), BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                            if (prop == null) return false;

                            var value = prop.GetValue(item) as AdStatus?;
                            return value == request.Status;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error applying Status filter to item");
                            return false;
                        }
                    }).ToList();

                    _logger.LogInformation("Filter 'Status={Status}' reduced items from {Original} to {Filtered}",
                        request.Status, originalCount, items.Count);
                }

                if (request.CreatedAt.HasValue)
                {
                    var createdDate = request.CreatedAt.Value.Date;
                    items = items.Where(item => item.CreatedAt.Date == createdDate).ToList();
                    _logger.LogInformation("Filter 'CreatedAt={CreatedAt}' reduced items to {Filtered}", createdDate, items.Count);
                }

                if (request.PublishedDate.HasValue)
                {
                    var publishedDate = request.PublishedDate.Value.Date;
                    items = items.Where(item => item.PublishedDate.HasValue && item.PublishedDate.Value.Date == publishedDate).ToList();
                    _logger.LogInformation("Filter 'PublishedDate={PublishedDate}' reduced items to {Filtered}", publishedDate, items.Count);
                }

                if (request.AdType.HasValue)
                {
                    items = items.Where(item => item.AdType == request.AdType).ToList();
                    _logger.LogInformation("Filter 'AdType={AdType}' reduced items to {Filtered}", request.AdType, items.Count);
                }

                if (!string.IsNullOrWhiteSpace(request.OrderBy))
                {
                    try
                    {
                        var parts = request.OrderBy.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        var propertyName = parts[0];
                        var direction = parts.Length > 1 ? parts[1].ToLower() : "asc";

                        var orderProp = typeof(ClassifiedsItems).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                        if (orderProp != null)
                        {
                            items = direction == "desc"
                                ? items.OrderByDescending(i => orderProp.GetValue(i)).ToList()
                                : items.OrderBy(i => orderProp.GetValue(i)).ToList();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error applying sorting by {OrderBy}", request.OrderBy);
                    }
                }

                int page = Math.Max(1, request.PageNumber);
                int pageSize = Math.Max(1, Math.Min(1000, request.PageSize));

                var totalCount = items.Count;
                var pagedItems = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                _logger.LogInformation("Returning {Count} items out of {Total}", pagedItems.Count, totalCount);

                return new ClassifiedsBoItemsResponseDto
                {
                    TotalCount = totalCount,
                    ClassifiedsItems = pagedItems
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetAllItems");
                throw new Exception($"GetAllItems failed: {ex.Message}", ex);
            }
        }

        public async Task<ClassifiedsBoCollectiblesResponseDto> GetAllCollectibles(GetAllSearch request, CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Starting GetAllCollectibles processing for request: {Request}",
                    System.Text.Json.JsonSerializer.Serialize(request));

                var indexKeys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.StateStoreNames.UnifiedStore,
                    ConstantValues.StateStoreNames.CollectiblesIndexKey,
                    cancellationToken: ct
                ) ?? new List<string>();

                _logger.LogInformation("Found {Count} index keys", indexKeys.Count);

                if (!indexKeys.Any())
                {
                    return new ClassifiedsBoCollectiblesResponseDto
                    {
                        TotalCount = 0,
                        ClassifiedsCollectibles = new List<ClassifiedsCollectibles>()
                    };
                }

                var items = new List<ClassifiedsCollectibles>();
                var failedKeys = new List<string>();

                foreach (var key in indexKeys)
                {
                    try
                    {
                        ct.ThrowIfCancellationRequested();

                        var dto = await _dapr.GetStateAsync<ClassifiedsCollectibles>(
                            ConstantValues.StateStoreNames.UnifiedStore,
                            key,
                            cancellationToken: ct);

                        if (dto != null)
                        {
                            items.Add(dto);
                        }
                        else
                        {
                            _logger.LogWarning("Collectible with key {Key} returned null", key);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to retrieve item with key {Key}", key);
                        failedKeys.Add(key);
                    }
                }

                if (!string.IsNullOrEmpty(request.Text) && request.Text.Trim() != "*")
                {
                    items = items.Where(item =>
                        (item.Title?.Contains(request.Text, StringComparison.OrdinalIgnoreCase) == true) ||
                        (item.UserId?.Contains(request.Text, StringComparison.OrdinalIgnoreCase) == true)
                    ).ToList();

                    _logger.LogInformation("Applied text filter '{Text}', resulting count: {Count}", request.Text, items.Count);
                }

                if (request.IsFeatured.HasValue)
                {
                    var originalCount = items.Count;
                    items = items.Where(item =>
                    {
                        try
                        {
                            var prop = item.GetType().GetProperty(nameof(request.IsFeatured), BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                            if (prop == null) return false;

                            var value = prop.GetValue(item) as bool?;
                            return value == request.IsFeatured;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error applying IsFeatured filter to item");
                            return false;
                        }
                    }).ToList();

                    _logger.LogInformation("Filter 'IsFeatured={IsFeatured}' reduced items from {Original} to {Filtered}",
                        request.IsFeatured, originalCount, items.Count);
                }

                if (request.IsPromoted.HasValue)
                {
                    var originalCount = items.Count;
                    items = items.Where(item =>
                    {
                        try
                        {
                            var prop = item.GetType().GetProperty(nameof(request.IsPromoted), BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                            if (prop == null) return false;

                            var value = prop.GetValue(item) as bool?;
                            return value == request.IsPromoted;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error applying IsPromoted filter to item");
                            return false;
                        }
                    }).ToList();

                    _logger.LogInformation("Filter 'IsPromoted={IsPromoted}' reduced items from {Original} to {Filtered}",
                        request.IsPromoted, originalCount, items.Count);
                }

                if (request.Status.HasValue)
                {
                    var originalCount = items.Count;
                    items = items.Where(item =>
                    {
                        try
                        {
                            var prop = item.GetType().GetProperty(nameof(request.Status), BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                            if (prop == null) return false;

                            var value = prop.GetValue(item) as AdStatus?;
                            return value == request.Status;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error applying Status filter to item");
                            return false;
                        }
                    }).ToList();

                    _logger.LogInformation("Filter 'Status={Status}' reduced items from {Original} to {Filtered}",
                        request.Status, originalCount, items.Count);
                }

                if (request.CreatedAt.HasValue)
                {
                    var createdDate = request.CreatedAt.Value.Date;
                    items = items.Where(item => item.CreatedAt.Date == createdDate).ToList();
                    _logger.LogInformation("Filter 'CreatedAt={CreatedAt}' reduced items to {Filtered}", createdDate, items.Count);
                }

                if (request.PublishedDate.HasValue)
                {
                    var publishedDate = request.PublishedDate.Value.Date;
                    items = items.Where(item => item.PublishedDate.HasValue && item.PublishedDate.Value.Date == publishedDate).ToList();
                    _logger.LogInformation("Filter 'PublishedDate={PublishedDate}' reduced items to {Filtered}", publishedDate, items.Count);
                }

                if (request.AdType.HasValue)
                {
                    items = items.Where(item => item.AdType == request.AdType).ToList();
                    _logger.LogInformation("Filter 'AdType={AdType}' reduced items to {Filtered}", request.AdType, items.Count);
                }

                if (!string.IsNullOrWhiteSpace(request.OrderBy))
                {
                    try
                    {
                        var parts = request.OrderBy.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        var propertyName = parts[0];
                        var direction = parts.Length > 1 ? parts[1].ToLower() : "asc";

                        var orderProp = typeof(ClassifiedsItems).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                        if (orderProp != null)
                        {
                            items = direction == "desc"
                                ? items.OrderByDescending(i => orderProp.GetValue(i)).ToList()
                                : items.OrderBy(i => orderProp.GetValue(i)).ToList();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error applying sorting by {OrderBy}", request.OrderBy);
                    }
                }

                int page = Math.Max(1, request.PageNumber);
                int pageSize = Math.Max(1, Math.Min(1000, request.PageSize));

                var totalCount = items.Count;
                var pagedItems = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                _logger.LogInformation("Returning {Count} items out of {Total}", pagedItems.Count, totalCount);

                return new ClassifiedsBoCollectiblesResponseDto
                {
                    TotalCount = totalCount,
                    ClassifiedsCollectibles = pagedItems
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetAllCollectibles");
                throw new Exception($"GetAllCollectibles failed: {ex.Message}", ex);
            }
        }

        private async Task IndexItemsToAzureSearch(ClassifiedsItems dto, CancellationToken cancellationToken)
        {
            var indexDoc = new ClassifiedsItemsIndex
            {
                Id = dto.Id.ToString(),
                SubVertical = dto.SubVertical,
                AdType = dto.AdType.ToString(),
                Title = dto.Title,
                Description = dto.Description,
                CategoryId = dto.CategoryId.ToString(),
                L1CategoryId = dto.L1CategoryId.ToString(),
                L2CategoryId = dto.L2CategoryId.ToString(),
                Category = dto.Category,
                L1Category = dto.L1Category,
                L2Category = dto.L2Category,
                Brand = dto.Brand,
                Model = dto.Model,
                Color = dto.Color,
                Condition = dto.Condition,
                SubscriptionId = dto.SubscriptionId,
                Price = (double)dto.Price,
                PriceType = dto.PriceType,
                Location = dto.Location,
                Longitude = (double)dto.Longitude,
                Latitude = (double)dto.Latitude,
                IsFeatured = dto.IsFeatured,
                IsPromoted = dto.IsPromoted,
                Status = dto.Status.ToString(),
                FeaturedExpiryDate = dto.FeaturedExpiryDate,
                PromotedExpiryDate = dto.PromotedExpiryDate,
                UserId = dto.UserId,
                LastRefreshedOn = dto.LastRefreshedOn,
                BuildingNumber = dto.BuildingNumber,
                ContactEmail = dto.ContactEmail,
                ContactNumber = dto.ContactNumber,
                ContactNumberCountryCode = dto.ContactNumberCountryCode,
                StreetNumber = dto.StreetNumber,
                WhatsAppNumber = dto.WhatsAppNumber,
                WhatsappNumberCountryCode = dto.WhatsappNumberCountryCode,
                Zone = dto.zone,
                IsRefreshed = dto.IsRefreshed,
                PublishedDate = dto.PublishedDate,
                ExpiryDate = dto.ExpiryDate,
                UserName = dto.UserName,
                AttributesJson = dto.Attributes != null ? System.Text.Json.JsonSerializer.Serialize(dto.Attributes) : null,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt,
                UpdatedBy = dto.UpdatedBy,
                Images = dto.Images.Select(i => new ImageInfo
                {
                    Url = i.Url,
                    Order = i.Order
                }).ToList()
            };
            var indexRequest = new CommonIndexRequest
            {
                IndexName = ConstantValues.IndexNames.ClassifiedsItemsIndex,
                ClassifiedsItem = indexDoc
            };
            if (indexRequest != null)
            {
                var message = new IndexMessage
                {
                    Action = "Upsert",
                    Vertical = ConstantValues.IndexNames.ClassifiedsItemsIndex,
                    UpsertRequest = indexRequest
                };

                await _dapr.PublishEventAsync(
                    pubsubName: ConstantValues.PubSubName,
                    topicName: ConstantValues.PubSubTopics.IndexUpdates,
                    data: message,
                    cancellationToken: cancellationToken
                );
            }
        }
        private async Task IndexPrelovedToAzureSearch(ClassifiedsPreloved dto, CancellationToken cancellationToken)
        {
            var indexDoc = new ClassifiedsPrelovedIndex
            {
                Id = dto.Id.ToString(),
                SubscriptionId = dto.SubscriptionId,
                SubVertical = dto.SubVertical,
                AdType = dto.AdType.ToString(),
                Title = dto.Title,
                Description = dto.Description,
                Price = dto.Price,
                PriceType = dto.PriceType,
                CategoryId = dto.CategoryId,
                Category = dto.Category,
                L1CategoryId = dto.L1CategoryId,
                L1Category = dto.L1Category,
                L2CategoryId = dto.L2CategoryId,
                L2Category = dto.L2Category,
                Location = dto.Location,
                CreatedAt = dto.CreatedAt,
                PublishedDate = dto.PublishedDate,
                ExpiryDate = dto.ExpiryDate,
                Status = dto.Status.ToString(),
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Zone = dto.zone,
                WhatsappNumberCountryCode = dto.WhatsappNumberCountryCode,
                WhatsAppNumber = dto.WhatsAppNumber,
                StreetNumber = dto.StreetNumber,
                LastRefreshedOn = dto.LastRefreshedOn,
                BuildingNumber = dto.BuildingNumber,
                ContactEmail = dto.ContactEmail,
                ContactNumberCountryCode = dto.ContactNumberCountryCode,
                ContactNumber = dto.ContactNumber,
                UserId = dto.UserId,
                AuthenticityCertificateUrl = dto.AuthenticityCertificateUrl,
                Brand = dto.Brand,
                Color = dto.Color,
                Condition = dto.Condition,
                CreatedBy = dto.CreatedBy,
                HasAuthenticityCertificate = dto.HasAuthenticityCertificate,
                Inclusion = dto.Inclusion,
                Model = dto.Model,
                UserName = dto.UserName,
                IsActive = true,
                UpdatedAt = dto.UpdatedAt,
                UpdatedBy = dto.UpdatedBy,
                Images = dto.Images.Select(i => new ImageInfo
                {
                    Url = i.Url,
                    Order = i.Order
                }).ToList(),
                AttributesJson = System.Text.Json.JsonSerializer.Serialize(dto.Attributes ?? new Dictionary<string, string>()),

                IsFeatured = dto.IsFeatured,
                FeaturedExpiryDate = dto.FeaturedExpiryDate,
                IsPromoted = dto.IsPromoted,
                PromotedExpiryDate = dto.PromotedExpiryDate,
                IsRefreshed = dto.IsRefreshed
            };
            var indexRequest = new CommonIndexRequest
            {
                IndexName = ConstantValues.IndexNames.ClassifiedsPrelovedIndex,
                ClassifiedsPrelovedItem = indexDoc
            };
            if (indexRequest != null)
            {
                var message = new IndexMessage
                {
                    Action = "Upsert",
                    Vertical = ConstantValues.IndexNames.ClassifiedsPrelovedIndex,
                    UpsertRequest = indexRequest
                };

                await _dapr.PublishEventAsync(
                    pubsubName: ConstantValues.PubSubName,
                    topicName: ConstantValues.PubSubTopics.IndexUpdates,
                    data: message,
                    cancellationToken: cancellationToken
                );
            }
        }
        private async Task IndexCollectiblesToAzureSearch(ClassifiedsCollectibles dto, CancellationToken cancellationToken)
        {
            var indexDoc = new ClassifiedsCollectiblesIndex
            {
                Id = dto.Id.ToString(),
                SubVertical = dto.SubVertical,
                SubscriptionId = dto.SubscriptionId,
                AdType = dto.AdType.ToString(),
                Title = dto.Title,
                Description = dto.Description,
                Price = dto.Price,
                PriceType = dto.PriceType,
                CategoryId = dto.CategoryId,
                Category = dto.Category,
                L1CategoryId = dto.L1CategoryId,
                L1Category = dto.L1Category,
                L2CategoryId = dto.L2CategoryId,
                L2Category = dto.L2Category,
                Location = dto.Location,
                CreatedAt = dto.CreatedAt,
                PublishedDate = dto.PublishedDate,
                ExpiryDate = dto.ExpiryDate,
                Status = dto.Status.ToString(),
                Latitude = dto.Latitude,
                Color = dto.Color,
                ContactNumber = dto.ContactNumber,
                BuildingNumber = dto.BuildingNumber,
                ContactNumberCountryCode = dto.ContactNumberCountryCode,
                ContactEmail = dto.ContactEmail,
                StreetNumber = dto.StreetNumber,
                Model = dto.Model,
                IsHandmade = dto.IsHandmade,
                HasWarranty = dto.HasWarranty,
                Condition = dto.Condition,
                Brand = dto.Brand,
                AuthenticityCertificateUrl = dto.AuthenticityCertificateUrl,
                CreatedBy = dto.CreatedBy,
                HasAuthenticityCertificate = dto.HasAuthenticityCertificate,
                WhatsAppNumber = dto.WhatsAppNumber,
                WhatsappNumberCountryCode = dto.WhatsappNumberCountryCode,
                YearOrEra = dto.YearOrEra,
                Zone = dto.zone,
                Longitude = dto.Longitude,
                UserId = dto.UserId,
                UserName = dto.UserName,
                IsActive = true,
                UpdatedAt = dto.UpdatedAt,
                UpdatedBy = dto.UpdatedBy,
                Images = dto.Images.Select(i => new ImageInfo
                {
                    Url = i.Url,
                    Order = i.Order
                }).ToList(),
                AttributesJson = System.Text.Json.JsonSerializer.Serialize(dto.Attributes ?? new Dictionary<string, string>()),

                IsFeatured = dto.IsFeatured,
                FeaturedExpiryDate = dto.FeaturedExpiryDate,
                IsPromoted = dto.IsPromoted,
                PromotedExpiryDate = dto.PromotedExpiryDate



            };
            var indexRequest = new CommonIndexRequest
            {
                IndexName = ConstantValues.IndexNames.ClassifiedsCollectiblesIndex,
                ClassifiedsCollectiblesItem = indexDoc
            };
            if (indexRequest != null)
            {
                var message = new IndexMessage
                {
                    Action = "Upsert",
                    Vertical = ConstantValues.IndexNames.ClassifiedsCollectiblesIndex,
                    UpsertRequest = indexRequest
                };

                await _dapr.PublishEventAsync(
                    pubsubName: ConstantValues.PubSubName,
                    topicName: ConstantValues.PubSubTopics.IndexUpdates,
                    data: message,
                    cancellationToken: cancellationToken
                );
            }
        }       


      

        public async Task<List<SubscriptionTypes>> GetSubscriptionTypes(CancellationToken cancellationToken = default)
        {
            try
            {
                var getSubscriptionTypes = await _context.SubscriptionType.AsNoTracking().ToListAsync();
                return getSubscriptionTypes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting subscription types.");
                return new List<SubscriptionTypes>();
            }
        }
        public async Task<SubscriptionTypes> GetSubscriptionById(int Id, CancellationToken cancellationToken = default)
        {
            try
            {
                var getSubscriptionType = await _context.SubscriptionType.AsNoTracking().Where(x => x.SubscriptionId == Id).FirstOrDefaultAsync();
                return getSubscriptionType ?? new SubscriptionTypes();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting subscription types.");
                return new SubscriptionTypes();
            }
        }

        public async Task<string> GetTestXMLValidation(CancellationToken cancellationToken = default)
        {
            try
            {
                string result = string.Empty;
                string errors = string.Empty;
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string xmlPath = Path.Combine(basePath, "Data", "Products-Incorrect.xml");
                string xsdPath = Path.Combine(basePath, "Data", "Products.XSD");
                var manager = new ProductXmlManager(xsdPath);
                errors = manager.ValidateXml(xmlPath);
                if (string.IsNullOrEmpty(errors))
                {
                    result = "Valid XML";
                    return result;
                }
                else
                {
                    return errors;
                }


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting subscription types.");
                return ex.Message;
            }
        }

       
        public async Task<string> GetProcessStoresXML(string Url, string CompanyId, int SubscriptionId, string UserName, CancellationToken cancellationToken = default)
        {
            try
            { 
                string result = string.Empty;
                string errors = string.Empty;
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string xmlPath = Url;
                string xsdPath = Path.Combine(basePath, "Data", "Products.XSD");
                var manager = new ProductXmlManager(xsdPath);
                errors = manager.ValidateXml(xmlPath);
                if (string.IsNullOrEmpty(errors))
                {
                    //result = "Valid XML";
                    using var httpClient = new HttpClient();
                    string xml = await httpClient.GetStringAsync(xmlPath);
                    //string xml = File.ReadAllText(xmlPath);
                    XmlSerializer serializer = new XmlSerializer(typeof(Products));
                    using StringReader reader = new StringReader(xml);
                    Products xmlproducts = (Products)serializer.Deserialize(reader);
                    if (xmlproducts != null && xmlproducts.ProductList != null && xmlproducts.ProductList.Count > 0)
                    {
                        await DeleteProductsByCompanyIdAsync(Guid.Parse(CompanyId), UserName);
                        foreach (var xmlproduct in xmlproducts.ProductList)
                        {

                            StoreProducts storeProducts = new StoreProducts();
                            Guid StoreProductId = Guid.NewGuid();
                            storeProducts.StoreProductId = StoreProductId;
                            DateTime now = DateTime.UtcNow;
                            storeProducts.CompanyId = Guid.Parse(CompanyId);
                            storeProducts.SubscriptionId = SubscriptionId;
                            storeProducts.ProductName = xmlproduct.ProductName;
                            storeProducts.ProductLogo = xmlproduct.ProductLogo;
                            storeProducts.ProductPrice = xmlproduct.ProductPrice;
                            storeProducts.Currency = xmlproduct.Currency; storeProducts.ProductSummary = xmlproduct.ProductDetails.ProductSummary;
                            storeProducts.ProductDescription = xmlproduct.ProductDetails.ProductDescription;
                            storeProducts.CreatedDate = now;
                            storeProducts.UpdatedDate = now;
                            storeProducts.CreatedUser = UserName;
                            storeProducts.UpdatedUser = UserName;
                            storeProducts.Features = xmlproduct.ProductDetails.Features.Select(f => new ProductFeatures
                            {
                                ProductFeaturesId = Guid.NewGuid(),
                                Features = f,
                                CreatedDate = now,
                                UpdatedDate = now,
                                CreatedUser = UserName,
                                UpdatedUser = UserName,
                                StoreProductId = StoreProductId
                            }).ToList();
                            storeProducts.Images = xmlproduct.ProductDetails.Images.Select(img => new ProductImages
                            {
                                ProductImagesId = Guid.NewGuid(),
                                Images = img,
                                CreatedDate = now,
                                UpdatedDate = now,
                                CreatedUser = UserName,
                                UpdatedUser = UserName,
                                StoreProductId = StoreProductId
                            }).ToList();

                            _context.StoreProduct.Add(storeProducts);
                            await _context.SaveChangesAsync();
                        }
                    }

                    return "created";
                }
                else
                {
                    return errors;
                }


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting subscription types.");
                return ex.Message;
            }
        }
        public async Task DeleteProductsByCompanyIdAsync(Guid companyId, string UserName)
        {
            var products = await _context.StoreProduct
                                .Where(p => p.CompanyId == companyId && p.Status == true)
                                .Include(p => p.Features)
                                .Include(p => p.Images)
                                .ToListAsync();

            foreach (var product in products)
            {
                product.Status = false;
                foreach (var feature in product.Features)
                {
                    feature.Status = false; // or "InActive" if you change type
                    feature.UpdatedDate = DateTime.UtcNow;
                    feature.UpdatedUser = UserName;
                }
                foreach (var image in product.Images)
                {
                    image.Status = false;
                    image.UpdatedDate = DateTime.UtcNow;
                    image.UpdatedUser = UserName;
                }
            }
            await _context.SaveChangesAsync();
        }

    }
}
