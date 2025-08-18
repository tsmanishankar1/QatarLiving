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
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.QLDbContext;
using QLN.Common.Infrastructure.Subscriptions;

using System;
using System.ComponentModel.Design;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Xml.Serialization;
using static QLN.Common.Infrastructure.Constants.ConstantValues;

namespace QLN.Classified.MS.Service.ClassifiedBoService
{
    public class InternalClassifiedLandigBo : IClassifiedBoLandingService
    {
        private readonly Dapr.Client.DaprClient _dapr;
        private readonly ILogger<IClassifiedBoLandingService> _logger;
        private readonly IClassifiedService _classified;
        private readonly List<TransactionDto> _mockTransactions;
        private readonly List<PrelovedTransactionDto> _mockPrelovedTransactions;
        private readonly QLClassifiedContext _context;
        private const string StoreName = ConstantValues.StateStoreNames.LandingBackOfficeStore;
        private const string ItemsIndexKey = ConstantValues.StateStoreNames.LandingBOIndex;
        private const string ItemsServiceIndexKey = ConstantValues.StateStoreNames.LandingServiceBOIndex;
        private const string ClassifiedsFeaturedStoresIndexKey = ConstantValues.StateStoreNames.FeaturedStoreClassifiedsIndexKey;
        private const string ServicesFeaturedStoresIndexKey = ConstantValues.StateStoreNames.FeaturedStoreServicesIndexKey;
        private const string FeaturedCategoryClassifiedIndex = ConstantValues.StateStoreNames.FeaturedCategoryClassifiedIndex;
        private const string FeaturedCategoryServiceIndex = ConstantValues.StateStoreNames.FeaturedCategoryServiceIndex;
        // private const string SubscriptionStoreName = ConstantValues.StateStoreNames.SubscriptionStores;
        private const string SubscriptionStoresIndexKey = ConstantValues.StateStoreNames.SubscriptionStoresIndexKey;
        private readonly QLSubscriptionContext _subscriptioncontext;
        private readonly QLPaymentsContext _Paymentcontext;
        private readonly QLApplicationContext _usercontext;


        public InternalClassifiedLandigBo(IClassifiedService classified, DaprClient dapr, ILogger<IClassifiedBoLandingService> logger, QLClassifiedContext context, QLSubscriptionContext subscriptioncontext, QLPaymentsContext Paymentcontext , QLApplicationContext usercontext)
        {
            _classified = classified;
            _dapr = dapr;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _mockPrelovedTransactions = GenerateMockPrelovedTransactions();            
            _context = context;
            _subscriptioncontext = subscriptioncontext;
            _Paymentcontext = Paymentcontext;
            _usercontext = usercontext;
        }



        public async Task<string> CreateSeasonalPick(string userId, string userName, SeasonalPicksDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var duplicateExists = await _context.SeasonalPicks
                    .AnyAsync(p =>
                        p.IsActive == true &&
                        p.Title == dto.Title &&                       
                        p.Vertical == dto.Vertical &&
                        p.EndDate >= today,
                        cancellationToken);

                if (duplicateExists)
                {
                    var message = $"A seasonal pick with the title '{dto.Title}' already exists for vertical '{dto.Vertical}'.";
                    _logger.LogWarning(message);
                    throw new ConflictException(message);
                }

                var newPick = new SeasonalPicks
                {
                    Id = Guid.NewGuid(),
                    Title = dto.Title,
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
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _logger.LogInformation("Creating new seasonal pick. Category: {CategoryName}, User: {UserId}, ID: {Id}", dto.CategoryName, newPick.CreatedBy, newPick.Id);

                _context.SeasonalPicks.Add(newPick);
                await _context.SaveChangesAsync(cancellationToken);

                var result = $"Seasonal pick '{dto.CategoryName}' created successfully.";
                _logger.LogInformation("Successfully completed seasonal pick creation: {Message}", result);

                return result;
            }
            catch (ConflictException ex)
            {
                _logger.LogError(ex.Message, "Failed to post seasonal pick. Category: {Category}, User: {UserId} (409)", dto.CategoryName, userId);
                throw new ConflictException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to post seasonal pick. Category: {CategoryName}", dto.CategoryName);
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<SeasonalPicks>> GetSeasonalPicks(Vertical vertical, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching seasonal picks from PostgreSQL for vertical: {Vertical}", vertical);

                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                var activePicks = await _context.SeasonalPicks
                    .Where(p =>
                        p.IsActive == true &&
                        p.Vertical == vertical &&
                        p.SlotOrder == 0 &&
                        p.EndDate >= today)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync(cancellationToken);                

                return activePicks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch seasonal picks.");
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<SeasonalPicks>> GetSlottedSeasonalPicks(Vertical vertical, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching slotted seasonal picks from PostgreSQL for vertical: {Vertical}", vertical);

                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                var slottedPicks = await _context.SeasonalPicks
                    .Where(p =>
                        p.IsActive == true &&
                        p.Vertical == vertical &&
                        p.SlotOrder >= 1 &&
                        p.SlotOrder <= 6 &&
                        p.EndDate >= today)
                    .OrderBy(p => p.SlotOrder)
                    .ToListAsync(cancellationToken);


                return slottedPicks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch slotted seasonal picks.");
                throw new Exception(ex.Message);
            }
        }

        public async Task<string> ReplaceSlotWithSeasonalPick(string userId, string userName, ReplaceSeasonalPickSlotRequest dto, CancellationToken cancellationToken = default)
        {
            if (dto.TargetSlotId < 1 || dto.TargetSlotId > 6)
                throw new ArgumentOutOfRangeException(nameof(dto.TargetSlotId), "Slot must be between 1 and 6.");

            if (!Guid.TryParse(dto.PickId, out var pickGuid))
                throw new ArgumentException("Invalid PickId format. Must be a valid GUID.", nameof(dto.PickId));

            try
            {
                _logger.LogInformation("Replacing slot {SlotId} for pick {PickId} under vertical {Vertical}", dto.TargetSlotId, dto.PickId, dto.Vertical);

                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                var seasonalPicks = await _context.SeasonalPicks
                    .Where(p =>
                        p.Vertical == dto.Vertical &&
                        p.IsActive &&
                        p.EndDate >= today)
                    .ToListAsync(cancellationToken);

                if (!seasonalPicks.Any())
                    throw new KeyNotFoundException("No seasonal picks found for the given vertical.");

                var newPick = seasonalPicks.FirstOrDefault(p => p.Id == pickGuid);

                if (newPick == null)
                    throw new KeyNotFoundException("The selected seasonal pick does not exist.");

                foreach (var pick in seasonalPicks)
                {
                    if (pick.SlotOrder == dto.TargetSlotId && pick.Id != pickGuid)
                    {
                        pick.SlotOrder = 0;
                        pick.UpdatedBy = userId;
                        pick.UpdatedAt = DateTime.UtcNow;
                        _context.SeasonalPicks.Update(pick);
                    }

                    if (pick.Id == pickGuid)
                    {
                        pick.SlotOrder = dto.TargetSlotId;
                        pick.UpdatedBy = userId;
                        pick.UpdatedAt = DateTime.UtcNow;
                        _context.SeasonalPicks.Update(pick);
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);

                return $"Successfully replaced slot {dto.TargetSlotId} with seasonal pick '{newPick.CategoryName}' under vertical '{dto.Vertical}'.";
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogError(ex, "Seasonal pick not found for replacement. PickId: {PickId}, Vertical: {Vertical}", dto.PickId, dto.Vertical);
                throw new KeyNotFoundException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error replacing slot {Slot} with pick {PickId} in vertical: {Vertical}", dto.TargetSlotId, dto.PickId, dto.Vertical);
                throw new Exception(ex.Message);
            }
        }

        public async Task<string> ReorderSeasonalPickSlots(string userId, string userName, SeasonalPickSlotReorderRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                const int MaxSlot = 6;

                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("UserId is required.");

                if (request.SlotAssignments == null || request.SlotAssignments.Count != MaxSlot)
                    throw new InvalidDataException($"Exactly {MaxSlot} slot assignments must be provided.");

                var slotNumbers = request.SlotAssignments.Select(sa => sa.SlotOrder).ToList();
                if (slotNumbers.Distinct().Count() != MaxSlot || slotNumbers.Any(s => s < 1 || s > MaxSlot))
                    throw new InvalidDataException("Slot numbers must be unique and between 1 and 6.");

                var seasonalPicksList = await _context.SeasonalPicks
                    .Where(p => p.Vertical == request.Vertical && p.IsActive)
                    .ToListAsync(cancellationToken);

                var seasonalPicksMap = seasonalPicksList.ToDictionary(fc => fc.Id, fc => fc);

                foreach (var assignment in request.SlotAssignments)
                {
                    if (string.IsNullOrWhiteSpace(assignment.PickId))
                        continue;

                    if (!Guid.TryParse(assignment.PickId, out var pickId))
                        continue;

                    if (!seasonalPicksMap.TryGetValue(pickId, out var seasonalPicks))
                        throw new InvalidDataException($"Seasonal Pick with ID '{assignment.PickId}' not found or inactive.");
                 

                    seasonalPicks.SlotOrder = assignment.SlotOrder;
                    seasonalPicks.UpdatedBy = userId;
                    seasonalPicks.UpdatedAt = DateTime.UtcNow;

                    _context.SeasonalPicks.Update(seasonalPicks);
                }

                await _context.SaveChangesAsync(cancellationToken);

                return "Slots updated successfully.";
            }
            catch (KeyNotFoundException ex)
            {
                throw new KeyNotFoundException(ex.Message);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }


        public async Task<string> SoftDeleteSeasonalPick(string pickId, string userId, string userName, Vertical vertical, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(pickId))
                throw new ArgumentException("Pick ID must be provided.", nameof(pickId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID must be provided.", nameof(userId));

            try
            {
                _logger.LogInformation("Attempting delete for seasonal pick. PickId: {PickId}, UserId: {UserId}", pickId, userId);

                if (!Guid.TryParse(pickId, out var pickGuid))
                    throw new ArgumentException("Invalid PickId format. Must be a valid GUID.", nameof(pickId));

                var pick = await _context.SeasonalPicks
                    .FirstOrDefaultAsync(p => p.Id == pickGuid && p.Vertical == vertical && p.IsActive, cancellationToken);

                if (pick == null)
                {
                    _logger.LogWarning("Pick not found for delete. PickId: {PickId}", pickId);
                    throw new KeyNotFoundException($"Pick with ID '{pickId}' not found.");
                }

                pick.IsActive = false;
                pick.UpdatedBy = userId;
                pick.UpdatedAt = DateTime.UtcNow;

                _context.SeasonalPicks.Update(pick);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully deleted pick. PickId: {PickId}", pickId);

                return $"Pick '{pick.CategoryName}' has been deleted.";
            }
            catch (KeyNotFoundException ex)
            {
                throw new KeyNotFoundException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing soft delete on pick. PickId: {PickId}, UserId: {UserId}", pickId, userId);
                throw new Exception(ex.Message);
            }
        }

        public async Task<SeasonalPicks> GetSeasonalPickById(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Guid.TryParse(id, out Guid parsedId))
                    throw new ArgumentException("Invalid GUID format.", nameof(id));
                var featuredCategoryId = await _context.SeasonalPicks.FirstOrDefaultAsync(f => f.Id == parsedId && f.IsActive);
                if (featuredCategoryId == null) throw new KeyNotFoundException("Seasonal pick not found.");
                return featuredCategoryId;
            }
            catch (KeyNotFoundException ex)
            {
                throw new KeyNotFoundException(ex.Message);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<string> EditSeasonalPick(string userId, string userName, EditSeasonalPickDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Guid.TryParse(dto.Id, out Guid parsedId))
                    throw new ArgumentException("Invalid GUID format.", nameof(dto.Id));

                var seasonalPick = await _context.SeasonalPicks
                    .FirstOrDefaultAsync(f => f.Id == parsedId && f.IsActive, cancellationToken);

                if (seasonalPick == null)
                    throw new KeyNotFoundException("Seasonal pick not found.");

                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                bool duplicateExists = await _context.SeasonalPicks.AnyAsync(p =>
                    p.IsActive &&
                    p.Id != parsedId &&
                    p.Vertical == dto.Vertical &&
                    p.Title == dto.Title &&
                    p.EndDate >= today,
                    cancellationToken);

                if (duplicateExists)
                {
                    var message = $"A seasonal pick title '{dto.Title}' already exists for vertical '{dto.Vertical}'.";
                    _logger.LogWarning(message);
                    throw new ConflictException(message);
                }

                seasonalPick.Title = dto.Title;
                seasonalPick.Vertical = dto.Vertical;
                seasonalPick.CategoryName = dto.CategoryName;
                seasonalPick.CategoryId = dto.CategoryId;
                seasonalPick.L1categoryName = dto.L1categoryName;
                seasonalPick.L1CategoryId = dto.L1CategoryId;
                seasonalPick.L2categoryId = dto.L2categoryId;
                seasonalPick.L2categoryName = dto.L2categoryName;
                seasonalPick.StartDate = dto.StartDate;
                seasonalPick.EndDate = dto.EndDate;
                seasonalPick.ImageUrl = dto.ImageUrl;
                seasonalPick.SlotOrder = dto.SlotOrder;
                seasonalPick.UpdatedAt = DateTime.UtcNow;
                seasonalPick.UpdatedBy = userId;

                _context.SeasonalPicks.Update(seasonalPick);
                await _context.SaveChangesAsync(cancellationToken);

                var messageSuccess = $"Landing BO category '{dto.CategoryName}' updated successfully.";
                _logger.LogInformation("Successfully edited seasonal picks. ID: {Id}, User: {UserId}", parsedId, userId);

                return messageSuccess;
            }
            catch (ConflictException ex)
            {
                _logger.LogError(ex, "Conflict while editing seasonal picks. ID: {Id}, User: {UserId}", dto.Id, userId);
                throw new ConflictException(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Seasonal picks not found for editing. ID: {Id}", dto.Id);
                throw new KeyNotFoundException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during seasonal picks edit. ID: {Id}, User: {UserId}", dto.Id, userId);
                throw new Exception("An error occurred while editing the seasonal picks.", ex);
            }
        }

        public async Task<string> CreateFeaturedStore(string userId, string userName, FeaturedStoreDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                bool duplicateExists = await _context.FeaturedStores
                    .AnyAsync(p =>
                        p.IsActive &&
                        p.Title == dto.Title &&                        
                        p.Vertical == dto.Vertical &&
                        p.EndDate >= today,
                        cancellationToken);

                if (duplicateExists)
                {
                    var message = $"A featured store with the title '{dto.Title}' already exists for vertical '{dto.Vertical}'.";
                    _logger.LogWarning(message);
                    throw new ConflictException(message);
                }

                var store = new FeaturedStore
                {
                    Id = Guid.NewGuid(),
                    Title = dto.Title,
                    Vertical = dto.Vertical,
                    StoreId = dto.StoreId,
                    StoreName = dto.StoreName,
                    ImageUrl = dto.ImageUrl,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    SlotOrder = 0,
                    IsActive = true,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Creating new featured store. Store: {StoreName}, User: {UserId}, ID: {Id}", dto.StoreName, userId, store.Id);

                _context.FeaturedStores.Add(store);
                await _context.SaveChangesAsync(cancellationToken);

                var result = $"Featured store '{dto.StoreName}' created successfully.";
                _logger.LogInformation("Successfully created featured store: {Message}", result);

                return result;
            }
            catch (ConflictException ex)
            {
                _logger.LogError(ex.Message, "Conflict while creating store: {StoreName}, User: {UserId}", dto.StoreName, userId);
                throw new ConflictException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create featured store. Store: {StoreName}, User: {UserId}", dto.StoreName, userId);
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<FeaturedStore>> GetFeaturedStores(Vertical vertical, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching featured stores from database...");

                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                var activeStores = await _context.FeaturedStores
                    .Where(p =>
                        p.Vertical == vertical &&
                        p.IsActive &&
                        p.SlotOrder == 0 &&
                        p.EndDate >= today)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {Count} active featured stores for vertical: {Vertical}", activeStores.Count, vertical);

                return activeStores;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch featured stores.");
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<FeaturedStoreItem>> GetSlottedFeaturedStores(Vertical vertical, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Fetching slotted featured stores from database...");

                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                var slottedStores = await _context.FeaturedStores
                    .Where(p =>
                        p.Vertical == vertical &&
                        p.IsActive &&
                        p.SlotOrder >= 1 && p.SlotOrder <= 6 &&
                        p.EndDate >= today)
                    .OrderBy(p => p.SlotOrder)
                    .ToListAsync(cancellationToken);

                var productSummary = _context.StoresDashboardSummaryItems.ToList();

                var featuredStoreDtos = (from store in slottedStores
                                         join summary in productSummary
                                             on store.StoreId.ToLower() equals summary.CompanyId.ToString().ToLower() into summaryGroup
                                         from summary in summaryGroup.DefaultIfEmpty()
                                         select new FeaturedStoreItem
                                         {
                                             Id = store.Id,
                                             Title = store.Title,
                                             Vertical = store.Vertical,
                                             StoreId = store.StoreId,
                                             StoreName = store.StoreName,
                                             ImageUrl = store.ImageUrl,
                                             StartDate = store.StartDate,
                                             EndDate = store.EndDate,
                                             SlotOrder = store.SlotOrder,
                                             IsActive = store.IsActive,
                                             CreatedBy = store.CreatedBy,
                                             CreatedAt = store.CreatedAt,
                                             UpdatedBy = store.UpdatedBy,
                                             UpdatedAt = store.UpdatedAt,
                                             ProductCount = summary?.ProductCount ?? 0
                                         }).ToList();

                _logger.LogInformation("Fetched {Count} slotted featured stores.", featuredStoreDtos.Count);

                return featuredStoreDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch slotted featured stores.");
                throw new Exception(ex.Message);
            }
        }

        public async Task<string> ReplaceSlotWithFeaturedStore(string userId, string userName, ReplaceFeaturedStoresSlotRequest dto, CancellationToken cancellationToken = default)
        {
            if (dto.TargetSlotId < 1 || dto.TargetSlotId > 6)
                throw new ArgumentOutOfRangeException(nameof(dto.TargetSlotId), "Slot must be between 1 and 6.");

            try
            {
                _logger.LogInformation("Replacing slot {Slot} with store {StoreId} for vertical: {Vertical}", dto.TargetSlotId, dto.StoreId, dto.Vertical);
                if (!Guid.TryParse(dto.StoreId, out Guid storeGuid))
                    throw new ArgumentException("Invalid StoreId format.", nameof(dto.StoreId));
                var newStore = await _context.FeaturedStores
                    .FirstOrDefaultAsync(s =>
                        s.Id == storeGuid &&
                        s.Vertical == dto.Vertical &&
                        s.IsActive,
                        cancellationToken);

                if (newStore == null)
                    throw new KeyNotFoundException("Selected featured store ID not found or is inactive.");

                var storesInSlot = await _context.FeaturedStores
                    .Where(s =>
                        s.Vertical == dto.Vertical &&
                        s.SlotOrder == dto.TargetSlotId &&
                        s.Id != storeGuid)
                    .ToListAsync(cancellationToken);

                foreach (var store in storesInSlot)
                {
                    store.SlotOrder = 0;
                    store.UpdatedBy = userId;
                    store.UpdatedAt = DateTime.UtcNow;
                }

                _context.FeaturedStores.UpdateRange(storesInSlot);

                newStore.SlotOrder = dto.TargetSlotId;
                newStore.UpdatedBy = userId;
                newStore.UpdatedAt = DateTime.UtcNow;

                _context.FeaturedStores.Update(newStore);

                await _context.SaveChangesAsync(cancellationToken);

                var message = $"Successfully replaced slot {dto.TargetSlotId} with featured store '{newStore.StoreName}' under vertical '{dto.Vertical}'.";
                _logger.LogInformation(message);

                return message;
            }
            catch (KeyNotFoundException ex)
            {
                throw new KeyNotFoundException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error replacing slot {Slot} with featured store {StoreId} in vertical: {Vertical}", dto.TargetSlotId, dto.StoreId, dto.Vertical);
                throw new Exception(ex.Message);
            }
        }

        public async Task<string> ReorderFeaturedStoreSlots(string userId, string userName, FeaturedStoreSlotReorderRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                const int MaxSlot = 6;

                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("UserId is required.");

                if (request.SlotAssignments == null || request.SlotAssignments.Count != MaxSlot)
                    throw new InvalidDataException($"Exactly {MaxSlot} slot assignments must be provided.");

                var slotNumbers = request.SlotAssignments.Select(sa => sa.SlotOrder).ToList();
                if (slotNumbers.Distinct().Count() != MaxSlot || slotNumbers.Any(s => s < 1 || s > MaxSlot))
                    throw new InvalidDataException("SlotNumber must be unique and between 1 and 6.");

                var storeIds = request.SlotAssignments
                    .Where(a => !string.IsNullOrWhiteSpace(a.StoreId))
                    .Select(a => Guid.TryParse(a.StoreId, out var guid) ? guid : Guid.Empty)
                    .Where(guid => guid != Guid.Empty)
                    .ToList();

                var stores = await _context.FeaturedStores
                    .Where(fs => storeIds.Contains(fs.Id) && fs.Vertical == request.Vertical && fs.IsActive)
                    .ToListAsync(cancellationToken);

                var storeDict = stores.ToDictionary(s => s.Id.ToString(), s => s);

                foreach (var assignment in request.SlotAssignments)
                {
                    if (string.IsNullOrWhiteSpace(assignment.StoreId))
                        continue;

                    if (!Guid.TryParse(assignment.StoreId, out var storeGuid))
                        throw new InvalidDataException($"Invalid StoreId format: '{assignment.StoreId}'");

                    if (!storeDict.TryGetValue(storeGuid.ToString(), out var store))
                        throw new KeyNotFoundException($"Store with ID '{assignment.StoreId}' not found or not active in vertical '{request.Vertical}'.");

                    store.SlotOrder = assignment.SlotOrder;
                    store.UpdatedBy = userId;
                    store.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync(cancellationToken);

                return "Slots updated successfully.";
            }
            catch (KeyNotFoundException ex)
            {
                throw new KeyNotFoundException(ex.Message);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<string> SoftDeleteFeaturedStore(string storeId, string userId, string userName, Vertical vertical, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(storeId))
                throw new ArgumentException("Store ID must be provided.", nameof(storeId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID must be provided.", nameof(userId));

            try
            {
                _logger.LogInformation("Attempting soft delete for featured store. StoreId: {StoreId}, UserId: {UserId}", storeId, userId);

                if (!Guid.TryParse(storeId, out var storeGuid))
                    throw new ArgumentException("Invalid Store ID format.", nameof(storeId));

                var store = await _context.FeaturedStores
                    .FirstOrDefaultAsync(s => s.Id == storeGuid && s.Vertical == vertical, cancellationToken);

                if (store == null)
                {
                    _logger.LogWarning("Featured store not found. StoreId: {StoreId}", storeId);
                    throw new KeyNotFoundException($"Featured store with ID '{storeId}' not found.");
                }

                store.IsActive = false;
                store.UpdatedBy = userId;
                store.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully soft deleted featured store. StoreId: {StoreId}", storeId);

                return $"Featured store '{store.StoreName}' has been deleted.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing soft delete on featured store. StoreId: {StoreId}, UserId: {UserId}", storeId, userId);
                throw new Exception(ex.Message);
            }
        }

        public async Task<FeaturedStore> GetFeaturedStoreById(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Guid.TryParse(id, out Guid parsedId))
                    throw new ArgumentException("Invalid GUID format.", nameof(id));
                var featuredCategoryId = await _context.FeaturedStores.FirstOrDefaultAsync(f => f.Id == parsedId && f.IsActive);
                if (featuredCategoryId == null) throw new KeyNotFoundException("Featured store not found.");
                return featuredCategoryId;
            }
            catch (KeyNotFoundException ex)
            {
                throw new KeyNotFoundException(ex.Message);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<string> EditFeaturedStore(string userId, string userName, EditFeaturedStoreDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Guid.TryParse(dto.Id, out Guid parsedId))
                    throw new ArgumentException("Invalid GUID format.", nameof(dto.Id));

                var featuredStore = await _context.FeaturedStores
                    .FirstOrDefaultAsync(f => f.Id == parsedId && f.IsActive, cancellationToken);

                if (featuredStore == null)
                    throw new KeyNotFoundException("Featured store not found.");

                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                bool duplicateExists = await _context.FeaturedStores.AnyAsync(p =>
                    p.IsActive &&
                    p.Id != parsedId &&
                    p.Vertical == dto.Vertical &&
                    p.Title == dto.Title &&
                    p.EndDate >= today,
                    cancellationToken);

                if (duplicateExists)
                {
                    var message = $"A featured store title '{dto.Title}' already exists for vertical '{dto.Vertical}'.";
                    _logger.LogWarning(message);
                    throw new ConflictException(message);
                }

                featuredStore.Title = dto.Title;
                featuredStore.Vertical = dto.Vertical;
                featuredStore.StoreName = dto.StoreName;
                featuredStore.StoreId = dto.StoreId;
                featuredStore.StartDate = dto.StartDate;
                featuredStore.EndDate = dto.EndDate;
                featuredStore.ImageUrl = dto.ImageUrl;
                featuredStore.SlotOrder = dto.SlotOrder;
                featuredStore.UpdatedAt = DateTime.UtcNow;
                featuredStore.UpdatedBy = userId;

                _context.FeaturedStores.Update(featuredStore);
                await _context.SaveChangesAsync(cancellationToken);

                var messageSuccess = $"Landing BO featured store '{dto.StoreName}' updated successfully.";
                _logger.LogInformation("Successfully edited featured category. ID: {Id}, User: {UserId}", parsedId, userId);

                return messageSuccess;
            }
            catch (ConflictException ex)
            {
                _logger.LogError(ex, "Conflict while editing featured store. ID: {Id}, User: {UserId}", dto.Id, userId);
                throw new ConflictException(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Featured store not found for editing. ID: {Id}", dto.Id);
                throw new KeyNotFoundException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during featured store edit. ID: {Id}, User: {UserId}", dto.Id, userId);
                throw new Exception("An error occurred while editing the featured store.", ex);
            }
        }

        public async Task<string> CreateFeaturedCategory(string userId, string userName, FeaturedCategoryDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                bool duplicateExists = await _context.FeaturedCategories.AnyAsync(p =>
                    p.IsActive &&
                    p.Title == dto.Title &&
                    p.Vertical == dto.Vertical &&
                    p.EndDate >= today,
                    cancellationToken);

                if (duplicateExists)
                {
                    var message = $"A featured category Title '{dto.Title}' already exists for vertical '{dto.Vertical}'.";
                    _logger.LogWarning(message);
                    throw new ConflictException(message);
                }

                var newCategory = new FeaturedCategory
                {
                    Id = Guid.NewGuid(),
                    Title = dto.Title,
                    Vertical = dto.Vertical,
                    CategoryName = dto.CategoryName,
                    CategoryId = dto.CategoryId,
                    L1categoryName = dto.L1categoryName,
                    L1CategoryId = dto.L1CategoryId,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    ImageUrl = dto.ImageUrl,
                    SlotOrder = 0,
                    IsActive = true,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.FeaturedCategories.AddAsync(newCategory, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Created featured category. Category: {Category}, User: {UserId}, ID: {Id}",
                    dto.CategoryName, userId, newCategory.Id);

                var result = $"Landing bo Category '{dto.CategoryName}' created successfully.";
                _logger.LogInformation("Successfully completed landing bo creation: {Message}", result);

                return result;
            }
            catch (ConflictException ex)
            {
                _logger.LogError(ex, "Conflict while creating landing bo. Category: {Category}, User: {UserId}", dto.CategoryName, userId);
                throw new ConflictException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create landing bo. Category: {Category}, User: {UserId}", dto.CategoryName, userId);
                throw new Exception(ex.Message);
            }
        }
        public async Task<string> DeleteFeaturedCategory(string id, Vertical vertical, string userId, string userName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID must be provided.", nameof(userId));

            try
            {
                if (!Guid.TryParse(id, out var guidId))
                    throw new ArgumentException("Invalid featured category ID format.", nameof(id));

                var featuredCategory = await _context.FeaturedCategories
                    .FirstOrDefaultAsync(f => f.Id == guidId && f.Vertical == vertical && f.IsActive, cancellationToken);

                if (featuredCategory == null)
                {
                    _logger.LogWarning("FeaturedCategory not found for delete. FeaturedCategoryId: {FeaturedCategoryId}", id);
                    throw new KeyNotFoundException($"FeaturedCategory with ID '{id}' not found.");
                }

                featuredCategory.IsActive = false;
                featuredCategory.UpdatedBy = userId;
                featuredCategory.UpdatedAt = DateTime.UtcNow;

                _context.FeaturedCategories.Update(featuredCategory);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully soft-deleted FeaturedCategory. FeaturedCategoryId: {FeaturedCategoryId}", id);

                return "FeaturedCategory has been deleted.";
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                throw new KeyNotFoundException(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex.Message);
                throw new UnauthorizedAccessException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing soft delete on FeaturedCategory. FeaturedCategoryId: {FeaturedCategoryId}, UserId: {UserId}", id, userId);
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<FeaturedCategory>> GetSlottedFeaturedCategory(Vertical vertical, CancellationToken cancellationToken = default)
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var slottedFeaturedCategories = await _context.FeaturedCategories
                    .Where(p =>
                        p.Vertical == vertical &&
                        p.IsActive &&
                        p.SlotOrder >= 1 && p.SlotOrder <= 6 &&
                        p.EndDate >= today)
                    .OrderBy(p => p.SlotOrder)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {Count} slotted featured categories for vertical: {Vertical}", slottedFeaturedCategories.Count, vertical);

                return slottedFeaturedCategories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch slotted featured categories for vertical: {Vertical}", vertical);
                throw new Exception(ex.Message);
            }
        }
        

        public async Task<FeaturedCategory> GetFeaturedCategoryById(string id, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Guid.TryParse(id, out Guid parsedId))
                    throw new ArgumentException("Invalid GUID format.", nameof(id));
                var featuredCategoryId = await _context.FeaturedCategories.FirstOrDefaultAsync(f => f.Id == parsedId && f.IsActive);
                if (featuredCategoryId == null) throw new KeyNotFoundException("Featured category not found.");
                return featuredCategoryId;
            }
            catch (KeyNotFoundException ex)
            {
                throw new KeyNotFoundException(ex.Message);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<string> EditFeaturedCategory(string userId, string userName, EditFeaturedCategoryDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!Guid.TryParse(dto.Id, out Guid parsedId))
                    throw new ArgumentException("Invalid GUID format.", nameof(dto.Id));

                var featuredCategory = await _context.FeaturedCategories
                    .FirstOrDefaultAsync(f => f.Id == parsedId && f.IsActive, cancellationToken);

                if (featuredCategory == null)
                    throw new KeyNotFoundException("Featured category not found.");

                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                bool duplicateExists = await _context.FeaturedCategories.AnyAsync(p =>
                    p.IsActive &&
                    p.Id != parsedId &&
                    p.Vertical == dto.Vertical &&
                    p.Title == dto.Title &&
                    p.EndDate >= today,
                    cancellationToken);

                if (duplicateExists)
                {
                    var message = $"A featured category title '{dto.Title}' already exists for vertical '{dto.Vertical}'.";
                    _logger.LogWarning(message);
                    throw new ConflictException(message);
                }

                featuredCategory.Title = dto.Title;
                featuredCategory.Vertical = dto.Vertical;
                featuredCategory.CategoryName = dto.CategoryName;
                featuredCategory.CategoryId = dto.CategoryId;
                featuredCategory.L1categoryName = dto.L1categoryName;
                featuredCategory.L1CategoryId = dto.L1CategoryId;
                featuredCategory.StartDate = dto.StartDate;
                featuredCategory.EndDate = dto.EndDate;
                featuredCategory.ImageUrl = dto.ImageUrl;
                featuredCategory.SlotOrder = dto.SlotOrder;
                featuredCategory.UpdatedAt = DateTime.UtcNow;
                featuredCategory.UpdatedBy = userId;

                _context.FeaturedCategories.Update(featuredCategory);
                await _context.SaveChangesAsync(cancellationToken);

                var messageSuccess = $"Landing BO category '{dto.CategoryName}' updated successfully.";
                _logger.LogInformation("Successfully edited featured category. ID: {Id}, User: {UserId}", parsedId, userId);

                return messageSuccess;
            }
            catch (ConflictException ex)
            {
                _logger.LogError(ex, "Conflict while editing featured category. ID: {Id}, User: {UserId}", dto.Id, userId);
                throw new ConflictException(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Featured category not found for editing. ID: {Id}", dto.Id);
                throw new KeyNotFoundException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during featured category edit. ID: {Id}, User: {UserId}", dto.Id, userId);
                throw new Exception("An error occurred while editing the featured category.", ex);
            }
        }

        public async Task<List<FeaturedCategory>> GetFeaturedCategoriesByVertical(Vertical vertical, CancellationToken cancellationToken = default)
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var activeFeaturedCategories = await _context.FeaturedCategories
                    .Where(p =>
                        p.Vertical == vertical &&
                        p.IsActive &&
                        p.SlotOrder == 0 &&
                        p.EndDate >= today)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {Count} active featured categories for vertical: {Vertical}",
                    activeFeaturedCategories.Count, vertical);

                return activeFeaturedCategories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch featured categories for vertical: {Vertical}", vertical);
                throw new Exception(ex.Message);
            }
        }

        public async Task<string> ReorderFeaturedCategorySlots(string userId, string userName, LandingBoSlotReorderRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                const int MaxSlot = 6;

                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("UserId is required.");

                if (request.SlotAssignments == null || request.SlotAssignments.Count != MaxSlot)
                    throw new InvalidDataException($"Exactly {MaxSlot} slot assignments must be provided.");

                var slotNumbers = request.SlotAssignments.Select(sa => sa.SlotOrder).ToList();
                if (slotNumbers.Distinct().Count() != MaxSlot || slotNumbers.Any(s => s < 1 || s > MaxSlot))
                    throw new InvalidDataException("Slot numbers must be unique and between 1 and 6.");

                var featuredCategoryList = await _context.FeaturedCategories
                    .Where(p => p.Vertical == request.Vertical && p.IsActive)
                    .ToListAsync(cancellationToken);

                var featuredCategoryMap = featuredCategoryList.ToDictionary(fc => fc.Id, fc => fc);

                foreach (var assignment in request.SlotAssignments)
                {
                    if (string.IsNullOrWhiteSpace(assignment.CategoryId))
                        continue;

                    if (!Guid.TryParse(assignment.CategoryId, out var categoryId))
                        continue;

                    if (!featuredCategoryMap.TryGetValue(categoryId, out var featuredCategory))
                        throw new InvalidDataException($"Featured Category with ID '{assignment.CategoryId}' not found or inactive.");

                    featuredCategory.SlotOrder = assignment.SlotOrder;
                    featuredCategory.UpdatedBy = userId;
                    featuredCategory.UpdatedAt = DateTime.UtcNow;

                    _context.FeaturedCategories.Update(featuredCategory);
                }

                await _context.SaveChangesAsync(cancellationToken);

                return "Slots updated successfully.";
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<string> ReplaceFeaturedCategorySlots(string userId, string userName, LandingBoSlotReplaceRequest dto, CancellationToken cancellationToken = default)
        {
            if (dto.TargetSlotId < 1 || dto.TargetSlotId > 6)
                throw new ArgumentOutOfRangeException(nameof(dto.TargetSlotId), "Slot must be between 1 and 6.");

            try
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                if (!Guid.TryParse(dto.CategoryId, out var categoryGuid))
                    throw new ArgumentException("Invalid CategoryId", nameof(dto.CategoryId));
                var relevantCategories = await _context.FeaturedCategories
                    .Where(p =>
                        p.Vertical == dto.Vertical &&
                        p.IsActive &&
                        ((p.SlotOrder >= 1 && p.SlotOrder <= 6) || p.Id == categoryGuid) &&
                        p.EndDate >= today)
                    .ToListAsync(cancellationToken);

                var newItem = relevantCategories.FirstOrDefault(p => p.Id == categoryGuid);
                if (newItem == null)
                    throw new InvalidOperationException("Selected featured category not found.");

                foreach (var item in relevantCategories)
                {
                    if (item.Id != newItem.Id && item.SlotOrder == dto.TargetSlotId)
                    {
                        item.SlotOrder = 0;
                        item.UpdatedBy = userId;
                        item.UpdatedAt = DateTime.UtcNow;
                        _context.FeaturedCategories.Update(item);
                    }
                }

                newItem.SlotOrder = dto.TargetSlotId;
                newItem.UpdatedBy = userId;
                newItem.UpdatedAt = DateTime.UtcNow;
                _context.FeaturedCategories.Update(newItem);

                await _context.SaveChangesAsync(cancellationToken);

                return $"Successfully replaced slot {dto.TargetSlotId} with category '{newItem.CategoryName}'.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error replacing slot {Slot} with category {CategoryId}", dto.TargetSlotId, dto.CategoryId);
                throw new Exception(ex.Message);
            }
        }

        public async Task<BulkAdActionResponseitems> BulkItemsAction(
     BulkActionRequest request,
     string userId,
     CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId cannot be null or empty.");

            _logger.LogInformation("BulkItemsAction started by User {UserId} with Action {Action} for {Count} Ads.",
                userId, request.Action, request.AdIds?.Count);

            var ads = await _context.Item
                .Where(ad => request.AdIds.Contains(ad.Id) && ad.IsActive == true)
                .ToListAsync(cancellationToken);

            if (!ads.Any())
            {
                _logger.LogWarning("No active ads found for the given IDs: {Ids}", string.Join(",", request.AdIds));
                throw new InvalidOperationException("No ads found for the given IDs.");
            }

            var succeeded = new ResultGroup
            {
                Count = 0,
                Ids = new List<long>(),
                Reason = string.Empty
            };

            var failed = new ResultGroup
            {
                Count = 0,
                Ids = new List<long>(),
                Reason = string.Empty
            };

            var updatedAds = new List<Items>();

            foreach (var ad in ads)
            {
                bool shouldUpdate = false;
                string failReason = string.Empty;

                try
                {
                    switch (request.Action)
                    {
                        case BulkActionEnum.Approve:
                            if (ad.Status == AdStatus.PendingApproval)
                            {
                                ad.Status = AdStatus.Published;
                                shouldUpdate = true;
                                ad.CreatedAt = DateTime.UtcNow;
                                _logger.LogInformation("Ad {AdId} approved by {UserId}.", ad.Id, userId);
                            }
                            else failReason = $"Cannot approve ad with status '{ad.Status}'.";
                            break;

                        case BulkActionEnum.NeedChanges:
                            if (ad.Status == AdStatus.PendingApproval)
                            {
                                ad.Status = AdStatus.NeedsModification;
                                shouldUpdate = true;
                                ad.CreatedAt = DateTime.UtcNow;
                                _logger.LogInformation("Ad {AdId} marked as NeedsModification by {UserId}.", ad.Id, userId);
                            }
                            else failReason = $"Cannot need changes ad with status '{ad.Status}'.";
                            break;

                        case BulkActionEnum.Publish:
                            if (ad.Status == AdStatus.Unpublished || ad.Status == AdStatus.PendingApproval)
                            {
                                ad.Status = AdStatus.Published;
                                shouldUpdate = true;
                                ad.CreatedAt = DateTime.UtcNow;
                                _logger.LogInformation("Ad {AdId} published by {UserId}.", ad.Id, userId);
                            }
                            else failReason = $"Cannot publish ad with status '{ad.Status}'.";
                            break;

                        case BulkActionEnum.Unpublish:
                            if (ad.Status == AdStatus.Published)
                            {
                                ad.Status = AdStatus.Unpublished;
                                shouldUpdate = true;
                                ad.CreatedAt = DateTime.UtcNow;
                                _logger.LogInformation("Ad {AdId} unpublished by {UserId}.", ad.Id, userId);
                            }
                            else failReason = $"Cannot unpublish ad with status '{ad.Status}'.";
                            break;

                        case BulkActionEnum.UnPromote:
                            if (ad.IsPromoted)
                            {
                                ad.IsPromoted = false;
                                shouldUpdate = true;
                                ad.CreatedAt = DateTime.UtcNow;
                                _logger.LogInformation("Ad {AdId} unpromoted by {UserId}.", ad.Id, userId);
                            }
                            else failReason = "Cannot unpromote an ad that is not promoted.";
                            break;

                        case BulkActionEnum.UnFeature:
                            if (ad.IsFeatured)
                            {
                                ad.IsFeatured = false;
                                shouldUpdate = true;
                                ad.CreatedAt = DateTime.UtcNow;
                                _logger.LogInformation("Ad {AdId} unfeatured by {UserId}.", ad.Id, userId);
                            }
                            else failReason = "Cannot unfeature an ad that is not featured.";
                            break;

                        case BulkActionEnum.Promote:
                            if (!ad.IsPromoted)
                            {
                                ad.IsPromoted = true;
                                ad.PromotedExpiryDate = DateTime.UtcNow;
                                shouldUpdate = true;
                                ad.CreatedAt = DateTime.UtcNow;
                                _logger.LogInformation("Ad {AdId} promoted by {UserId}.", ad.Id, userId);
                            }
                            else failReason = "Cannot promote an ad that is already promoted.";
                            break;

                        case BulkActionEnum.Feature:
                            if (!ad.IsFeatured)
                            {
                                ad.IsFeatured = true;
                                ad.FeaturedExpiryDate = DateTime.UtcNow;
                                shouldUpdate = true;
                                ad.CreatedAt = DateTime.UtcNow;
                                _logger.LogInformation("Ad {AdId} featured by {UserId}.", ad.Id, userId);
                            }
                            else failReason = "Cannot feature an ad that is already featured.";
                            break;

                        case BulkActionEnum.Remove:
                            ad.Status = AdStatus.Rejected;
                            shouldUpdate = true;
                            ad.CreatedAt = DateTime.UtcNow;
                            _logger.LogInformation("Ad {AdId} removed (rejected) by {UserId}.", ad.Id, userId);
                            break;

                        case BulkActionEnum.Hold:
                            if (ad.Status == AdStatus.Draft)
                                failReason = "Cannot hold an ad that is in draft status.";
                            else if (ad.Status != AdStatus.Hold)
                            {
                                ad.Status = AdStatus.Hold;
                                shouldUpdate = true;
                                ad.CreatedAt = DateTime.UtcNow;
                                _logger.LogInformation("Ad {AdId} placed on Hold by {UserId}.", ad.Id, userId);
                            }
                            else failReason = "Ad is already on hold.";
                            break;

                        case BulkActionEnum.Onhold:
                            if (ad.Status != AdStatus.Onhold)
                            {
                                ad.Status = AdStatus.Unpublished;
                                shouldUpdate = true;
                                ad.CreatedAt = DateTime.UtcNow;
                                _logger.LogInformation("Ad {AdId} set to OnHold (Unpublished) by {UserId}.", ad.Id, userId);
                            }
                            else failReason = "Ad is not on hold.";
                            break;

                        default:
                            failReason = "Invalid action.";
                            break;
                    }

                    if (shouldUpdate)
                    {
                        ad.UpdatedAt = DateTime.UtcNow;
                        ad.UpdatedBy = userId;
                        updatedAds.Add(ad);

                        succeeded.Count++;
                        succeeded.Ids.Add(ad.Id);
                    }
                    else
                    {
                        failed.Count++;
                        failed.Ids.Add(ad.Id);
                        failed.Reason += $"Ad {ad.Id}: {failReason} ";
                        _logger.LogWarning("Ad {AdId} failed action {Action} by {UserId}. Reason: {Reason}",
                            ad.Id, request.Action, userId, failReason);
                    }
                }
                catch (Exception ex)
                {
                    failed.Count++;
                    failed.Ids.Add(ad.Id);
                    failed.Reason += $"Ad {ad.Id}: {ex.Message} ";
                    _logger.LogError(ex, "Error while processing Ad {AdId} with action {Action} by {UserId}.",
                        ad.Id, request.Action, userId);
                }
            }

            if (updatedAds.Any())
            {
                await _context.SaveChangesAsync(cancellationToken);

                foreach (var ad in updatedAds)
                {
                    await IndexItemsToAzureSearch(ad, cancellationToken);
                    _logger.LogInformation("Ad {AdId} reindexed to Azure Search after {Action}.", ad.Id, request.Action);
                }
            }

            _logger.LogInformation("BulkItemsAction completed. Succeeded: {Succeeded}, Failed: {Failed}.",
                succeeded.Count, failed.Count);

            return new BulkAdActionResponseitems
            {
                Succeeded = succeeded,
                Failed = failed
            };
        }




        public async Task<BulkAdActionResponseitems> BulkCollectiblesAction(
     BulkActionRequest request,
     string userId,
     CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId cannot be null or empty.");

            var ads = await _context.Collectible
                .Where(ad => request.AdIds.Contains(ad.Id) && ad.IsActive == true)
                .ToListAsync(cancellationToken);

            if (!ads.Any())
                throw new InvalidOperationException("No collectibles found for the given IDs.");

            var succeeded = new ResultGroup
            {
                Count = 0,
                Ids = new List<long>(),
                Reason = string.Empty
            };

            var failed = new ResultGroup
            {
                Count = 0,
                Ids = new List<long>(),
                Reason = string.Empty
            };

            var updatedAds = new List<Collectibles>();

            foreach (var ad in ads)
            {
                bool shouldUpdate = false;
                string failReason = string.Empty;

                try
                {
                    switch (request.Action)
                    {
                        case BulkActionEnum.Approve:
                            if (ad.Status == AdStatus.PendingApproval)
                            {
                                ad.Status = AdStatus.Published;
                                shouldUpdate = true;
                                ad.CreatedAt = DateTime.UtcNow;
                            }
                            else failReason = $"Cannot approve ad with status '{ad.Status}'.";
                            break;

                        case BulkActionEnum.NeedChanges:
                            if (ad.Status == AdStatus.PendingApproval)
                            {
                                ad.Status = AdStatus.NeedsModification;
                                shouldUpdate = true;
                                ad.CreatedAt = DateTime.UtcNow;
                            }
                            else failReason = $"Cannot need changes ad with status '{ad.Status}'.";
                            break;

                        case BulkActionEnum.Publish:
                            if (ad.Status == AdStatus.Unpublished || ad.Status == AdStatus.PendingApproval)
                            {
                                ad.Status = AdStatus.Published;
                                shouldUpdate = true;
                                ad.CreatedAt = DateTime.UtcNow;
                            }
                            else failReason = $"Cannot publish ad with status '{ad.Status}'.";
                            break;

                        case BulkActionEnum.Unpublish:
                            if (ad.Status == AdStatus.Published)
                            {
                                ad.Status = AdStatus.Unpublished;
                                shouldUpdate = true;
                                ad.CreatedAt = DateTime.UtcNow;
                            }
                            else failReason = $"Cannot unpublish ad with status '{ad.Status}'.";
                            break;

                        case BulkActionEnum.UnPromote:
                            if (ad.IsPromoted)
                            {
                                ad.IsPromoted = false;
                                shouldUpdate = true;
                                ad.CreatedAt = DateTime.UtcNow;
                            }
                            else failReason = "Cannot unpromote an ad that is not promoted.";
                            break;

                        case BulkActionEnum.UnFeature:
                            if (ad.IsFeatured)
                            {
                                ad.IsFeatured = false;
                                shouldUpdate = true;
                                ad.CreatedAt = DateTime.UtcNow;
                            }
                            else failReason = "Cannot unfeature an ad that is not featured.";
                            break;

                        case BulkActionEnum.Promote:
                            if (!ad.IsPromoted)
                            {
                                ad.IsPromoted = true;
                                ad.PromotedExpiryDate = DateTime.UtcNow;
                                shouldUpdate = true;
                                ad.CreatedAt = DateTime.UtcNow;
                            }
                            else failReason = "Cannot promote an ad that is already promoted.";
                            break;

                        case BulkActionEnum.Feature:
                            if (!ad.IsFeatured)
                            {
                                ad.IsFeatured = true;
                                ad.FeaturedExpiryDate = DateTime.UtcNow;
                                shouldUpdate = true;
                                ad.CreatedAt = DateTime.UtcNow;
                            }
                            else failReason = "Cannot feature an ad that is already featured.";
                            break;

                        case BulkActionEnum.Remove:
                            ad.Status = AdStatus.Rejected;
                            shouldUpdate = true;
                            ad.CreatedAt = DateTime.UtcNow;
                            break;

                        case BulkActionEnum.Hold:
                            if (ad.Status == AdStatus.Draft)
                                failReason = "Cannot hold an ad that is in draft status.";
                            else if (ad.Status != AdStatus.Hold)
                                shouldUpdate = true;
                            else failReason = "Ad is already on hold.";
                            break;

                        case BulkActionEnum.Onhold:
                            if (ad.Status != AdStatus.Onhold)
                            {
                                ad.Status = AdStatus.Unpublished;
                                shouldUpdate = true;
                                ad.CreatedAt = DateTime.UtcNow;
                            }
                            else failReason = "Ad is not on hold.";
                            break;

                        default:
                            failReason = "Invalid action.";
                            break;
                    }

                    if (shouldUpdate)
                    {
                        ad.UpdatedAt = DateTime.UtcNow;
                        ad.UpdatedBy = userId;
                        updatedAds.Add(ad);

                        succeeded.Count++;
                        succeeded.Ids.Add(ad.Id);
                    }
                    else
                    {
                        failed.Count++;
                        failed.Ids.Add(ad.Id);
                        failed.Reason += $"Ad {ad.Id}: {failReason} ";
                    }
                }
                catch (Exception ex)
                {
                    failed.Count++;
                    failed.Ids.Add(ad.Id);
                    failed.Reason += $"Ad {ad.Id}: {ex.Message} ";
                }
            }

            if (updatedAds.Any())
            {
                await _context.SaveChangesAsync(cancellationToken);

                foreach (var ad in updatedAds)
                {
                    await IndexCollectiblesToAzureSearch(ad, cancellationToken);
                }
            }

            return new BulkAdActionResponseitems
            {
                Succeeded = succeeded,
                Failed = failed
            };
        }


        private string GetAdKey(long id) => $"ad-{id}";
        public async Task<TransactionListResponseDto> GetTransactionsAsync(
     TransactionFilterRequestDto request,
     CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting GetTransactionsAsync with request: {@Request}", request);

                IQueryable<TransactionDto> joined; 

                if ((int)request.SubVertical == 1)
                {
                    // ITEMS / PRELOVED / DEALS logic
                    var payments = await _Paymentcontext.Payments.ToListAsync(cancellationToken);
                    _logger.LogInformation("Loaded {Count} payments", payments.Count);

                    var subscriptions = await _subscriptioncontext.Subscriptions.ToListAsync(cancellationToken);
                    _logger.LogInformation("Loaded {Count} subscriptions", subscriptions.Count);

                    var users = await _usercontext.Users.ToListAsync(cancellationToken);
                    _logger.LogInformation("Loaded {Count} users", users.Count);

                    var publisheddate = await _context.Item.ToListAsync(cancellationToken);
                    _logger.LogInformation("Loaded {Count} items (for published date)", publisheddate.Count);

                    joined = (from p in payments
                              join s in subscriptions on p.PaymentId equals s.PaymentId
                              join u in users on s.UserId equals u.Id.ToString() into userJoin
                              from u in userJoin.DefaultIfEmpty()
                              join i in publisheddate on p.AdId equals i.Id into itemJoin
                              from i in itemJoin.DefaultIfEmpty()
                              select new TransactionDto
                              {
                                  AdId = (long)p.AdId,
                                  OrderId = p.PaymentId,
                                  Username = u?.UserName,
                                  UserId = s.UserId,
                                  Status = p.Status.ToString(),
                                  Email = u?.Email,
                                  Mobile = u?.PhoneNumber,
                                  Whatsapp = u?.PhoneNumber,
                                  Amount = p.Fee,
                                  CreationDate = s.CreatedAt,
                                  PublishedDate = i?.PublishedDate,
                                  StartDate = s.StartDate,
                                  EndDate = s.EndDate
                              }).AsQueryable();
                }
                else if ((int)request.SubVertical == 3)
                {
                    // COLLECTIBLES logic — adjust context & joins accordingly
                    var payments = await _Paymentcontext.Payments.ToListAsync(cancellationToken);
                    var subscriptions = await _subscriptioncontext.Subscriptions.ToListAsync(cancellationToken);
                    var users = await _usercontext.Users.ToListAsync(cancellationToken);
                    var publisheddate = await _context.Collectible.ToListAsync(cancellationToken);

                    joined = (from p in payments
                              join s in subscriptions on p.PaymentId equals s.PaymentId
                              join u in users on s.UserId equals u.Id.ToString() into userJoin
                              from u in userJoin.DefaultIfEmpty()
                              join i in publisheddate on p.AdId equals i.Id into itemJoin
                              from i in itemJoin.DefaultIfEmpty()
                              select new TransactionDto
                              {
                                  AdId = (long)p.AdId,
                                  OrderId = p.PaymentId,
                                  Username = u?.UserName,
                                 // ProductType = p.ProductType,
                                  UserId = s.UserId,
                                  Status = p.Status.ToString(),
                                  Email = u?.Email,
                                  Mobile = u?.PhoneNumber,
                                  Whatsapp = u?.PhoneNumber,
                                  Amount = p.Fee,
                                  CreationDate = s.CreatedAt,
                                  PublishedDate = i?.PublishedDate,
                                  StartDate = s.StartDate,
                                  EndDate = s.EndDate
                              }).AsQueryable();
                }
                else
                {
                    _logger.LogWarning("Invalid SubVertical: {SubVertical}", request.SubVertical);
                    return new TransactionListResponseDto
                    {
                        Records = new List<TransactionDto>(),
                        TotalRecords = 0,
                        CurrentPage = request.PageNumber,
                        PageSize = request.PageSize,
                        TotalPages = 0
                    };
                }

                // FILTERS 
                if (!string.IsNullOrWhiteSpace(request.Status))
                {
                    joined = joined.Where(t => t.Status.Equals(request.Status, StringComparison.OrdinalIgnoreCase));
                    _logger.LogInformation("Filtered by status: {Status}", request.Status);
                }

                if (!string.IsNullOrWhiteSpace(request.SearchText))
                {
                    var search = request.SearchText.ToLower();
                    joined = joined.Where(t =>
                        t.UserId.ToLower().Contains(search) ||
                        t.Username.ToLower().Contains(search) ||
                        t.Email.ToLower().Contains(search) ||
                        t.Mobile.ToLower().Contains(search) ||
                        t.Whatsapp.ToLower().Contains(search) ||
                        t.Status.ToLower().Contains(search) ||
                        t.AdId.ToString().Contains(search) ||
                        t.OrderId.ToString().Contains(search)
                    );
                    _logger.LogInformation("Filtered by search text: {SearchText}", request.SearchText);
                }

                //  SORTING 
                joined = request.SortBy?.ToLower() switch
                {
                    "amount" => request.SortOrder == "desc"
                        ? joined.OrderByDescending(t => t.Amount)
                        : joined.OrderBy(t => t.Amount),

                    "status" => request.SortOrder == "desc"
                        ? joined.OrderByDescending(t => t.Status)
                        : joined.OrderBy(t => t.Status),

                    "paymentmethod" => request.SortOrder == "desc"
                        ? joined.OrderByDescending(t => t.PaymentMethod)
                        : joined.OrderBy(t => t.PaymentMethod),

                    _ => request.SortOrder == "desc"
                        ? joined.OrderByDescending(t => t.CreationDate)
                        : joined.OrderBy(t => t.CreationDate),
                };

                var totalRecords = joined.Count();
                var totalPages = (int)Math.Ceiling((double)totalRecords / request.PageSize);

                var paged = joined
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                return new TransactionListResponseDto
                {
                    Records = paged,
                    TotalRecords = totalRecords,
                    CurrentPage = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = totalPages
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching joined data in GetTransactionsAsync");
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

        public async Task<PaginatedResult<DealsAdSummaryDto>> GetAllDeals(
            int? pageNumber = 1,
            int? pageSize = 12,
            string? subscriptionType = null,
            DateOnly? startDate = null,
            DateOnly? endDate = null,
            string? search = null,
            string? sortBy = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting GetAllDeals with params: Page={Page}, Size={Size}, Subscription={SubType}, Start={Start}, End={End}, Search={Search}, SortBy={SortBy}",
                    pageNumber, pageSize, subscriptionType, startDate, endDate, search, sortBy);

                var payments = await _Paymentcontext.Payments.ToListAsync(cancellationToken);
                _logger.LogInformation("Loaded {Count} payments", payments.Count);

                var subscriptions = await _subscriptioncontext.Subscriptions.ToListAsync(cancellationToken);
                _logger.LogInformation("Loaded {Count} subscriptions", subscriptions.Count);

                var users = await _usercontext.Users.ToListAsync(cancellationToken);
                _logger.LogInformation("Loaded {Count} users", users.Count);

                var deals = await _context.Deal.ToListAsync(cancellationToken);
                _logger.LogInformation("Loaded {Count} deals", deals.Count);

                var joined = (from p in payments
                              join s in subscriptions on p.PaymentId equals s.PaymentId
                              join u in users on s.UserId equals u.Id.ToString() into userJoin
                              from u in userJoin.DefaultIfEmpty()
                              join d in deals on p.AdId equals d.Id into dealJoin
                              from d in dealJoin.DefaultIfEmpty()
                              select new DealsAdSummaryDto
                              {
                                  AdId = d?.Id ?? 0,
                                  ContactNumber = d?.ContactNumber,
                                  WhatsappNumber = d?.WhatsappNumber,
                                  createdby = d?.CreatedBy,
                                  status = d?.IsActive.ToString(),
                                  StartDate = d?.StartDate,
                                  EndDate = d?.EndDate,
                                  subscriptiontype = s.ProductCode switch
                                  {
                                      "QLC-SUB-1WE-001" => "1 Week",
                                      "QLC-SUB-1MO-005" => "1 Month",
                                      "QLC-SUB-3MO-005" => "3 Months",
                                      "QLC-SUB-6MO-005" => "6 Months",
                                      _ => ""
                                  },
                                  orderid = p.PaymentId,
                                  WhatsAppLeads = "0",
                                  PhoneLeads = "0",
                                  price = p.Fee.ToString("F2"),
                                  UserName = u?.UserName,
                                  email = u?.Email
                              }).AsQueryable();

                if (!string.IsNullOrWhiteSpace(subscriptionType))
                {
                    joined = joined.Where(x => x.subscriptiontype == subscriptionType);
                    _logger.LogInformation("Filtered by subscriptionType: {SubType}", subscriptionType);
                }

                if (startDate.HasValue)
                {
                    joined = joined.Where(x =>
                        x.StartDate.HasValue &&
                        DateOnly.FromDateTime(x.StartDate.Value) >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    joined = joined.Where(x =>
                        x.EndDate.HasValue &&
                        DateOnly.FromDateTime(x.EndDate.Value) <= endDate.Value);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.ToLower();
                    joined = joined.Where(x =>
                        x.AdId.ToString().Contains(s) ||
                        (x.createdby != null && x.createdby.ToLower().Contains(s)) ||
                        (x.email != null && x.email.ToLower().Contains(s)) ||
                        (x.UserName != null && x.UserName.ToLower().Contains(s))
                    );
                }

                joined = sortBy?.ToLower() switch
                {
                    "startdate" => joined.OrderBy(x => x.StartDate),
                    "enddate" => joined.OrderBy(x => x.EndDate),
                    _ => joined.OrderByDescending(x => x.StartDate)
                };

                var totalRecords = joined.Count();
                var currentPage = pageNumber ?? 1;
                var currentSize = pageSize ?? 12;
                var totalPages = (int)Math.Ceiling((double)totalRecords / currentSize);

                var paged = joined
                    .Skip((currentPage - 1) * currentSize)
                    .Take(currentSize)
                    .ToList();

                return new PaginatedResult<DealsAdSummaryDto>
                {
                    TotalCount = totalRecords,
                    PageNumber = currentPage,
                    PageSize = currentSize,
                    Items = paged
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching deals in GetAllDeals");
                throw;
            }
        }


        public async Task<PaginatedResult<DealsViewSummaryDto>> DealsViewSummary(
            int? pageNumber = 1,
            int? pageSize = 12,
            DateOnly? startDate = null,
            DateOnly? endDate = null,
            string? search = null,
            string? sortBy = null,
            string? status = null,
            bool? isPromoted = null,
            bool? isFeatured = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting DealsViewSummary with params: Page={Page}, Size={Size}, Start={Start}, End={End}, Search={Search}, SortBy={SortBy}, Status={Status}, Promoted={Promoted}, Featured={Featured}",
                    pageNumber, pageSize, startDate, endDate, search, sortBy, status, isPromoted, isFeatured);

                var payments = await _Paymentcontext.Payments.ToListAsync(cancellationToken);
                _logger.LogInformation("Loaded {Count} payments", payments.Count);

                var subscriptions = await _subscriptioncontext.Subscriptions.ToListAsync(cancellationToken);
                _logger.LogInformation("Loaded {Count} subscriptions", subscriptions.Count);

                var users = await _usercontext.Users.ToListAsync(cancellationToken);
                _logger.LogInformation("Loaded {Count} users", users.Count);

                var deals = await _context.Deal.ToListAsync(cancellationToken);
                _logger.LogInformation("Loaded {Count} deals", deals.Count);

                var joined = (from p in payments
                              join s in subscriptions on p.PaymentId equals s.PaymentId
                              join u in users on s.UserId equals u.Id.ToString() into userJoin
                              from u in userJoin.DefaultIfEmpty()
                              join d in deals on p.AdId equals d.Id into dealJoin
                              from d in dealJoin.DefaultIfEmpty()
                              where d != null && d.IsActive
                              select new DealsViewSummaryDto
                              {
                                  AdId = d.Id,
                                  Dealtitle = d.Offertitle,
                                  subscriptiontype = s.ProductCode switch
                                  {
                                      "QLC-SUB-1WE-001" => "1 Week",
                                      "QLC-SUB-1MO-005" => "1 Month",
                                      "QLC-SUB-3MO-005" => "3 Months",
                                      "QLC-SUB-6MO-005" => "6 Months",
                                      _ => ""
                                  },
                                  DateCreated = d.CreatedAt,
                                  createdby = d.CreatedBy,
                                  ContactNumber = d.ContactNumber,
                                  WhatsappNumber = d.WhatsappNumber,
                                  StartDate = d.StartDate ?? DateTime.UtcNow,
                                  EndDate = d.EndDate ?? DateTime.UtcNow,
                                  WebClick = 0,
                                  Weburl = d.WebsiteUrl,
                                  CoverImage = d.CoverImage,
                                  Views = 0,
                                  Impression = 0,
                                  Phonelead = 0,
                                  Status = d.Status.ToString(),
                                  IsPromoted = d.IsPromoted,
                                  IsFeatured = d.IsFeatured
                              }).AsQueryable();

                if (isPromoted.HasValue)
                {
                    joined = joined.Where(x => x.IsPromoted == isPromoted.Value);
                }

                if (isFeatured.HasValue)
                {
                    joined = joined.Where(x => x.IsFeatured == isFeatured.Value);
                }

                if (!string.IsNullOrWhiteSpace(status) && int.TryParse(status, out int statusValue))
                {
                    if (Enum.IsDefined(typeof(AdStatus), statusValue))
                    {
                        var parsedStatus = (AdStatus)statusValue;
                        joined = joined.Where(x => x.Status == parsedStatus.ToString());
                    }
                }

                if (startDate.HasValue)
                {
                    joined = joined.Where(x =>
                        DateOnly.FromDateTime(x.StartDate) >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    joined = joined.Where(x =>
                        DateOnly.FromDateTime(x.EndDate) <= endDate.Value);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.ToLower();
                    joined = joined.Where(x =>
                        x.AdId.ToString().Contains(s) ||
                        (x.createdby != null && x.createdby.ToLower().Contains(s))
                    );
                }

                joined = sortBy?.ToLower() switch
                {
                    "startdate" => joined.OrderBy(x => x.StartDate),
                    "enddate" => joined.OrderBy(x => x.EndDate),
                    _ => joined.OrderByDescending(x => x.StartDate)
                };

                var totalCount = joined.Count();
                var currentPage = pageNumber ?? 1;
                var currentSize = pageSize ?? 12;

                var paged = joined
                    .Skip((currentPage - 1) * currentSize)
                    .Take(currentSize)
                    .ToList();

                return new PaginatedResult<DealsViewSummaryDto>
                {
                    TotalCount = totalCount,
                    PageNumber = currentPage,
                    PageSize = currentSize,
                    Items = paged
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching deals view summary.");
                throw new InvalidOperationException("Failed to fetch deals view summary.", ex);
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

        public async Task<BulkAdActionResponseitems> BulkPrelovedAction(
    BulkActionRequest request,
    string userId,
    CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId cannot be null or empty.");

            _logger.LogInformation("BulkPrelovedAction started by User {UserId} with Action {Action} for {Count} Ads.",
                userId, request.Action, request.AdIds?.Count);

            // Fetch all ads in batch
            var ads = await _context.Preloved
                .Where(ad => request.AdIds.Contains(ad.Id) && ad.IsActive == true)
                .ToListAsync(cancellationToken);

            if (!ads.Any())
            {
                _logger.LogWarning("No active preloved ads found for the given IDs: {Ids}", string.Join(",", request.AdIds));
                throw new InvalidOperationException("No preloved ads found for the given IDs.");
            }

            var succeeded = new ResultGroup { Count = 0, Ids = new List<long>(), Reason = string.Empty };
            var failed = new ResultGroup { Count = 0, Ids = new List<long>(), Reason = string.Empty };
            var updatedAds = new List<Preloveds>();

            foreach (var ad in ads)
            {
                bool shouldUpdate = false;
                string failReason = string.Empty;

                try
                {
                    switch (request.Action)
                    {
                        case BulkActionEnum.Approve:
                            if (ad.Status == AdStatus.PendingApproval)
                            {
                                ad.Status = AdStatus.Published;
                                shouldUpdate = true;
                                ad.CreatedAt = DateTime.UtcNow;
                                _logger.LogInformation("Preloved {AdId} approved by {UserId}.", ad.Id, userId);
                            }
                            else failReason = $"Cannot approve preloved with status '{ad.Status}'.";
                            break;

                        case BulkActionEnum.NeedChanges:
                            if (ad.Status == AdStatus.PendingApproval)
                            {
                                ad.Status = AdStatus.NeedsModification;
                                shouldUpdate = true;
                                ad.CreatedAt = DateTime.UtcNow;
                                _logger.LogInformation("Preloved {AdId} marked as NeedsModification by {UserId}.", ad.Id, userId);
                            }
                            else failReason = $"Cannot need changes preloved with status '{ad.Status}'.";
                            break;

                        case BulkActionEnum.Publish:
                            if (ad.Status == AdStatus.Unpublished || ad.Status == AdStatus.PendingApproval)
                            {
                                ad.Status = AdStatus.Published;
                                shouldUpdate = true;
                                ad.CreatedAt = DateTime.UtcNow;
                                _logger.LogInformation("Preloved {AdId} published by {UserId}.", ad.Id, userId);
                            }
                            else failReason = $"Cannot publish preloved with status '{ad.Status}'.";
                            break;

                        case BulkActionEnum.Unpublish:
                            if (ad.Status == AdStatus.Published)
                            {
                                ad.Status = AdStatus.Unpublished;
                                shouldUpdate = true;
                                ad.CreatedAt = DateTime.UtcNow;
                                _logger.LogInformation("Preloved {AdId} unpublished by {UserId}.", ad.Id, userId);
                            }
                            else failReason = $"Cannot unpublish preloved with status '{ad.Status}'.";
                            break;

                        case BulkActionEnum.UnPromote:
                            if (ad.IsPromoted)
                            {
                                ad.IsPromoted = false;
                                shouldUpdate = true;
                                ad.CreatedAt = DateTime.UtcNow;
                                _logger.LogInformation("Preloved {AdId} unpromoted by {UserId}.", ad.Id, userId);
                            }
                            else failReason = "Cannot unpromote a preloved that is not promoted.";
                            break;

                        case BulkActionEnum.UnFeature:
                            if (ad.IsFeatured)
                            {
                                ad.IsFeatured = false;
                                shouldUpdate = true;
                                ad.CreatedAt = DateTime.UtcNow;
                                _logger.LogInformation("Preloved {AdId} unfeatured by {UserId}.", ad.Id, userId);
                            }
                            else failReason = "Cannot unfeature a preloved that is not featured.";
                            break;

                        case BulkActionEnum.Promote:
                            if (!ad.IsPromoted)
                            {
                                ad.IsPromoted = true;
                                ad.PromotedExpiryDate = DateTime.UtcNow;
                                shouldUpdate = true;
                                ad.CreatedAt = DateTime.UtcNow;
                                _logger.LogInformation("Preloved {AdId} promoted by {UserId}.", ad.Id, userId);
                            }
                            else failReason = "Cannot promote a preloved that is already promoted.";
                            break;

                        case BulkActionEnum.Feature:
                            if (!ad.IsFeatured)
                            {
                                ad.IsFeatured = true;
                                ad.FeaturedExpiryDate = DateTime.UtcNow;
                                shouldUpdate = true;
                                ad.CreatedAt = DateTime.UtcNow;
                                _logger.LogInformation("Preloved {AdId} featured by {UserId}.", ad.Id, userId);
                            }
                            else failReason = "Cannot feature a preloved that is already featured.";
                            break;

                        case BulkActionEnum.Remove:
                            ad.Status = AdStatus.Rejected;
                            shouldUpdate = true;
                            ad.CreatedAt = DateTime.UtcNow;
                            _logger.LogInformation("Preloved {AdId} removed (rejected) by {UserId}.", ad.Id, userId);
                            break;

                        case BulkActionEnum.Hold:
                            if (ad.Status == AdStatus.Draft)
                                failReason = "Cannot hold a preloved that is in draft status.";
                            else if (ad.Status != AdStatus.Hold)
                            {
                                ad.Status = AdStatus.Hold;
                                shouldUpdate = true;
                                ad.CreatedAt = DateTime.UtcNow;
                                _logger.LogInformation("Preloved {AdId} placed on Hold by {UserId}.", ad.Id, userId);
                            }
                            else failReason = "Preloved is already on hold.";
                            break;

                        case BulkActionEnum.Onhold:
                            if (ad.Status != AdStatus.Onhold)
                            {
                                ad.Status = AdStatus.Unpublished;
                                shouldUpdate = true;
                                ad.CreatedAt = DateTime.UtcNow;
                                _logger.LogInformation("Preloved {AdId} set to OnHold (Unpublished) by {UserId}.", ad.Id, userId);
                            }
                            else failReason = "Preloved is not on hold.";
                            break;

                        default:
                            failReason = "Invalid action.";
                            break;
                    }

                    if (shouldUpdate)
                    {
                        ad.UpdatedAt = DateTime.UtcNow;
                        ad.UpdatedBy = userId;
                        updatedAds.Add(ad);

                        succeeded.Count++;
                        succeeded.Ids.Add(ad.Id);
                    }
                    else
                    {
                        failed.Count++;
                        failed.Ids.Add(ad.Id);
                        failed.Reason += $"Preloved {ad.Id}: {failReason} ";
                        _logger.LogWarning("Preloved {AdId} failed action {Action} by {UserId}. Reason: {Reason}",
                            ad.Id, request.Action, userId, failReason);
                    }
                }
                catch (Exception ex)
                {
                    failed.Count++;
                    failed.Ids.Add(ad.Id);
                    failed.Reason += $"Preloved {ad.Id}: {ex.Message} ";
                    _logger.LogError(ex, "Error while processing Preloved {AdId} with action {Action} by {UserId}.",
                        ad.Id, request.Action, userId);
                }
            }

            if (updatedAds.Any())
            {
                // Save to DB (you missed this part!)
                await _context.SaveChangesAsync(cancellationToken);

                // Reindex each updated ad
                foreach (var ad in updatedAds)
                {
                    await IndexPrelovedToAzureSearch(ad, cancellationToken);
                    _logger.LogInformation("Preloved {AdId} reindexed to Azure Search after {Action}.", ad.Id, request.Action);
                }
            }

            _logger.LogInformation("BulkPrelovedAction completed. Succeeded: {Succeeded}, Failed: {Failed}.",
                succeeded.Count, failed.Count);

            return new BulkAdActionResponseitems
            {
                Succeeded = succeeded,
                Failed = failed
            };
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



   
        public async Task<ClassifiedsBoItemsResponseDto> GetAllItems(GetAllSearch request, CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Starting GetAllItems (DB version) with request: {Request}",
                    System.Text.Json.JsonSerializer.Serialize(request));

                // Filter IsActive = true at the beginning
                var query = _context.Item
                    .Where(item => item.IsActive)
                    .AsQueryable();

                // Text search on Title or UserId
                if (!string.IsNullOrWhiteSpace(request.Text) && request.Text.Trim() != "*")
                {
                    var text = request.Text.Trim().ToLower();
                    query = query.Where(item =>
                        item.Title.ToLower().Contains(text) ||
                        item.UserId.ToLower().Contains(text));

                   
                }

                if (request.IsFeatured.HasValue)
                {
                    query = query.Where(item => item.IsFeatured == request.IsFeatured.Value);
                }

                if (request.IsPromoted.HasValue)
                {
                    query = query.Where(item => item.IsPromoted == request.IsPromoted.Value);
                }

                if (request.Status.HasValue)
                {
                    query = query.Where(item => item.Status == request.Status.Value);
                }

                if (request.CreatedAt.HasValue)
                {
                    var date = request.CreatedAt.Value.Date;
                    query = query.Where(item => item.CreatedAt.Date == date);
                }

                if (request.PublishedDate.HasValue)
                {
                    var date = request.PublishedDate.Value.Date;
                    query = query.Where(item => item.PublishedDate.HasValue && item.PublishedDate.Value.Date == date);
                }

                if (request.AdType.HasValue)
                {
                    query = query.Where(item => item.AdType == request.AdType.Value);
                }

                _logger.LogInformation("Applied filters. Intermediate count: {Count}", await query.CountAsync(ct));

                
                if (!string.IsNullOrWhiteSpace(request.OrderBy))
                {
                    try
                    {
                        var parts = request.OrderBy.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        var propertyName = parts[0];
                        var direction = parts.Length > 1 ? parts[1].ToLower() : "asc";

                        var param = Expression.Parameter(typeof(Items), "x");
                        var property = Expression.PropertyOrField(param, propertyName);
                        var lambda = Expression.Lambda(property, param);

                        var method = direction == "desc" ? "OrderByDescending" : "OrderBy";

                        var orderByCall = Expression.Call(
                            typeof(Queryable),
                            method,
                            new Type[] { typeof(Items), property.Type },
                            query.Expression,
                            Expression.Quote(lambda)
                        );

                        query = query.Provider.CreateQuery<Items>(orderByCall);

                        _logger.LogInformation("Applied dynamic sorting: {OrderBy}", request.OrderBy);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error applying sorting using OrderBy: {OrderBy}", request.OrderBy);
                    }
                }

                // Pagination
                int page = Math.Max(1, request.PageNumber);
                int pageSize = Math.Max(1, Math.Min(1000, request.PageSize));

                _logger.LogInformation("Pagination - Page: {Page}, PageSize: {PageSize}", page, pageSize);

                var totalCount = await query.CountAsync(ct);

                var pagedEntities = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                _logger.LogInformation("Retrieved {Count} items from DB", pagedEntities.Count);

                // Map Items  ClassifiedsItems
                var result = pagedEntities.Select(item => new Items
                {
                    Id = item.Id,
                    Title = item.Title,
                    UserId = item.UserId,
                    IsFeatured = item.IsFeatured,
                    IsPromoted = item.IsPromoted,
                    Status = item.Status,
                    CreatedAt = item.CreatedAt,
                    PublishedDate = item.PublishedDate,
                    AdType = item.AdType,
                    IsActive = item.IsActive,
                    FeaturedExpiryDate = item.FeaturedExpiryDate,
                    PromotedExpiryDate = item.PromotedExpiryDate,
                    LastRefreshedOn = item.LastRefreshedOn,
                    IsRefreshed=item.IsRefreshed,
                    SubVertical = item.SubVertical,
                    Description = item.Description,
                    Price= item.Price,
                    PriceType = item.PriceType,
                    Category = item.Category,
                    CategoryId = item.CategoryId,
                    L1Category = item.L1Category,
                    L1CategoryId = item.L1CategoryId,
                    L2CategoryId = item.L2CategoryId,
                    L2Category = item.L2Category,
                    Location= item.Location,
                    Brand=item.Brand,
                    Model = item.Model,
                    Condition= item.Condition,
                    Color = item.Color,
                    ExpiryDate = item.ExpiryDate,
                    //status=item.Status
                    UserName = item.UserName,
                    //userId
                    Latitude = item.Latitude,
                    Longitude = item.Longitude,
                    ContactNumberCountryCode = item.ContactNumberCountryCode,
                    ContactNumber = item.ContactNumber,
                    ContactEmail = item.ContactEmail,
                    WhatsAppNumber = item.WhatsAppNumber,
                    WhatsappNumberCountryCode = item.WhatsappNumberCountryCode,
                    //StreetNumber = street
                    BuildingNumber = item.BuildingNumber,
                    zone = item.zone,
                    Images= item.Images,
                    Attributes = item.Attributes,
                    CreatedBy = item.CreatedBy,
                    UpdatedAt = item.UpdatedAt,
                    UpdatedBy = item.UpdatedBy,
                    SubscriptionId = item.SubscriptionId
                }).ToList();

                _logger.LogInformation("Returning {ResultCount} items out of {TotalCount}", result.Count, totalCount);

                return new ClassifiedsBoItemsResponseDto
                {
                    TotalCount = totalCount,
                    ClassifiedsItems = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[GetAllItems] Unexpected error occurred. Request: {RequestJson}, Message: {ErrorMessage}, StackTrace: {StackTrace}, InnerException: {InnerException}",
                    System.Text.Json.JsonSerializer.Serialize(request),
                    ex.Message,
                    ex.StackTrace,
                    ex.InnerException?.ToString()
                );

                throw new Exception($"GetAllItems failed: {ex.Message}", ex);
            }
        }


        public async Task<ClassifiedsBoCollectiblesResponseDto> GetAllCollectibles(GetAllSearch request, CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Starting GetAllCollectibles (DB version) with request: {Request}",
                    System.Text.Json.JsonSerializer.Serialize(request));

                
                var query = _context.Collectible
                    .Where(c => c.IsActive)
                    .AsQueryable();

                // Text search on Title or UserId
                if (!string.IsNullOrWhiteSpace(request.Text) && request.Text.Trim() != "*")
                {
                    var text = request.Text.Trim().ToLower();
                    query = query.Where(c =>
                        c.Title.ToLower().Contains(text) ||
                        c.UserId.ToLower().Contains(text));
                }

                if (request.IsFeatured.HasValue)
                {
                    query = query.Where(c => c.IsFeatured == request.IsFeatured.Value);
                }

                if (request.IsPromoted.HasValue)
                {
                    query = query.Where(c => c.IsPromoted == request.IsPromoted.Value);
                }

                if (request.Status.HasValue)
                {
                    query = query.Where(c => c.Status == request.Status.Value);
                }

                if (request.CreatedAt.HasValue)
                {
                    var date = request.CreatedAt.Value.Date;
                    query = query.Where(c => c.CreatedAt.Date == date);
                }

                if (request.PublishedDate.HasValue)
                {
                    var date = request.PublishedDate.Value.Date;
                    query = query.Where(c => c.PublishedDate.HasValue && c.PublishedDate.Value.Date == date);
                }

                if (request.AdType.HasValue)
                {
                    query = query.Where(c => c.AdType == request.AdType.Value);
                }

                _logger.LogInformation("Applied filters. Intermediate count: {Count}", await query.CountAsync(ct));

                // Dynamic sorting
                if (!string.IsNullOrWhiteSpace(request.OrderBy))
                {
                    try
                    {
                        var parts = request.OrderBy.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        var propertyName = parts[0];
                        var direction = parts.Length > 1 ? parts[1].ToLower() : "asc";

                        var param = Expression.Parameter(typeof(Collectibles), "x");
                        var property = Expression.PropertyOrField(param, propertyName);
                        var lambda = Expression.Lambda(property, param);

                        var method = direction == "desc" ? "OrderByDescending" : "OrderBy";

                        var orderByCall = Expression.Call(
                            typeof(Queryable),
                            method,
                            new Type[] { typeof(Collectibles), property.Type },
                            query.Expression,
                            Expression.Quote(lambda)
                        );

                        query = query.Provider.CreateQuery<Collectibles>(orderByCall);

                        _logger.LogInformation("Applied dynamic sorting: {OrderBy}", request.OrderBy);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error applying sorting using OrderBy: {OrderBy}", request.OrderBy);
                    }
                }

                // Pagination
                int page = Math.Max(1, request.PageNumber);
                int pageSize = Math.Max(1, Math.Min(1000, request.PageSize));

                _logger.LogInformation("Pagination - Page: {Page}, PageSize: {PageSize}", page, pageSize);

                var totalCount = await query.CountAsync(ct);

                var pagedEntities = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                _logger.LogInformation("Retrieved {Count} collectibles from DB", pagedEntities.Count);

                
                var result = pagedEntities.Select(c => new ClassifiedsCollectibles
                {
                    Id = c.Id,
                    Title = c.Title,
                    UserId = c.UserId,
                    IsFeatured = c.IsFeatured,
                    IsPromoted = c.IsPromoted,
                    Status = c.Status,
                    CreatedAt = c.CreatedAt,
                    PublishedDate = c.PublishedDate,
                    AdType = c.AdType,
                    IsActive = c.IsActive,
                    Description = c.Description,
                    HasAuthenticityCertificate = c.HasAuthenticityCertificate,
                    AuthenticityCertificateUrl = c.AuthenticityCertificateUrl,
                    HasWarranty = c.HasWarranty,
                    IsHandmade = c.IsHandmade,
                    YearOrEra = c.YearOrEra,
                    SubVertical = c.SubVertical,
                    Price = c.Price,
                    PriceType = c.PriceType,
                    Category = c.Category,
                    L1Category = c.L1Category,
                    Location = c.Location,
                    Brand = c.Brand,
                    Model = c.Model,
                    Condition = c.Condition,
                    Color = c.Color,
                    ExpiryDate = c.ExpiryDate,
                    //Status =c.Status,
                    UserName = c.UserName,
                    FeaturedExpiryDate = c.FeaturedExpiryDate,
                    PromotedExpiryDate = c.PromotedExpiryDate
                    




                }).ToList();

                _logger.LogInformation("Returning {ResultCount} collectibles out of {TotalCount}", result.Count, totalCount);

                return new ClassifiedsBoCollectiblesResponseDto
                {
                    TotalCount = totalCount,
                    ClassifiedsCollectibles = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[GetAllCollectibles] Unexpected error occurred. Request: {RequestJson}, Message: {ErrorMessage}, StackTrace: {StackTrace}, InnerException: {InnerException}",
                    System.Text.Json.JsonSerializer.Serialize(request),
                    ex.Message,
                    ex.StackTrace,
                    ex.InnerException?.ToString()
                );

                throw new Exception($"GetAllCollectibles failed: {ex.Message}", ex);
            }
        }


        private async Task IndexItemsToAzureSearch(Items dto, CancellationToken cancellationToken)
        {
            var indexDoc = new ClassifiedsItemsIndex
            {
                Id = dto.Id.ToString(),
                SubVertical = dto.SubVertical.ToString(),
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
                //SubscriptionId = dto.SubscriptionId,
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
        private async Task IndexPrelovedToAzureSearch(Preloveds dto, CancellationToken cancellationToken)
        {
            var indexDoc = new ClassifiedsPrelovedIndex
            {
                Id = dto.Id.ToString(),
                SubscriptionId = dto.SubscriptionId.ToString(),
                SubVertical = dto.SubVertical.ToString(),
                AdType = dto.AdType.ToString(),
                Title = dto.Title,
                Description = dto.Description,
                Price = dto.Price,
                PriceType = dto.PriceType,
                CategoryId = dto.CategoryId.ToString(),
                Category = dto.Category,
                L1CategoryId = dto.L1CategoryId.ToString(),
                L1Category = dto.L1Category,
                L2CategoryId = dto.L2CategoryId.ToString(),
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
        private async Task IndexCollectiblesToAzureSearch(Collectibles dto, CancellationToken cancellationToken)
        {
            var indexDoc = new ClassifiedsCollectiblesIndex
            {
                Id = dto.Id.ToString(),
                SubVertical = dto.SubVertical.ToString(),
                //SubscriptionId = dto.SubscriptionId,
                AdType = dto.AdType.ToString(),
                Title = dto.Title,
                Description = dto.Description,
                Price = dto.Price,
                PriceType = dto.PriceType,
                //CategoryId = dto.CategoryId,
                Category = dto.Category,
               // L1CategoryId = dto.L1CategoryId,
                L1Category = dto.L1Category,
               // L2CategoryId = dto.L2CategoryId,
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

        private async Task IndexDealsToAzureSearch(Deals dto, CancellationToken cancellationToken)
        {
            var indexDoc = new ClassifiedsDealsIndex
            {
                Id = dto.Id.ToString(),
                UserId = dto.UserId,
                BusinessName = dto.BusinessName,
                BranchNames = dto.BranchNames,
                BusinessType = dto.BusinessType,
                offertitle = dto.Offertitle,
                Description = dto.Description,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                FlyerFileUrl = dto.FlyerFileUrl,
                DataFeedUrl = dto.DataFeedUrl,
                ContactNumber = dto.ContactNumber,
                WhatsappNumber = dto.WhatsappNumber,
                WebsiteUrl = dto.WebsiteUrl,
                SocialMediaLinks = dto.SocialMediaLinks,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy,
                CreatedAt = dto.CreatedAt,
                XMLlink = dto.XMLlink,
                SubscriptionId = dto.SubscriptionId,
                UpdatedAt = dto.UpdatedAt,
                UpdatedBy = dto.UpdatedBy,
                ExpiryDate = dto.ExpiryDate,
                CoverImage = dto.CoverImage,
                PromotedExpiryDate = dto.PromotedExpiryDate,
                IsPromoted = dto.IsPromoted,
                FeaturedExpiryDate = dto.FeaturedExpiryDate,
                IsFeatured = dto.IsFeatured,
            };

            var indexRequest = new CommonIndexRequest
            {
                IndexName = ConstantValues.IndexNames.ClassifiedsDealsIndex,
                ClassifiedsDealsItem = indexDoc
            };
            if (indexRequest != null)
            {
                var message = new IndexMessage
                {
                    Action = "Upsert",
                    Vertical = ConstantValues.IndexNames.ClassifiedsDealsIndex,
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

        public async Task<string> BulkDealsAction(BulkActionRequest request, string userId, CancellationToken ct)
        {
            try
            {
                var ads = await _context.Deal
                    .Where(d => request.AdIds.Contains(d.Id))
                    .ToListAsync(ct);

                if (!ads.Any())
                    throw new KeyNotFoundException("No matching ads found.");

                var updated = new List<Deals>();

                foreach (var ad in ads)
                {
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
                                throw new ConflictException($"Cannot approve ad with status '{ad.Status}'. Only 'PendingApproval' is allowed.");
                            break;

                        case BulkActionEnum.NeedChanges:
                            if (ad.Status == AdStatus.PendingApproval)
                            {
                                ad.Status = AdStatus.NeedsModification;
                                shouldUpdate = true;
                            }
                            else
                                throw new ConflictException($"Cannot need changes ad with status '{ad.Status}'. Only 'PendingApproval' is allowed.");
                            break;

                        case BulkActionEnum.Publish:
                            if (ad.Status == AdStatus.Unpublished)
                            {
                                ad.Status = AdStatus.Published;
                                shouldUpdate = true;
                            }
                            else
                                throw new ConflictException($"Cannot publish ad with status '{ad.Status}'. Only 'Unpublished' is allowed.");
                            break;

                        case BulkActionEnum.Unpublish:
                            if (ad.Status == AdStatus.Published)
                            {
                                ad.Status = AdStatus.Unpublished;
                                shouldUpdate = true;
                            }
                            else
                                throw new ConflictException($"Cannot unpublish ad with status '{ad.Status}'. Only 'Published' is allowed.");
                            break;

                        case BulkActionEnum.UnPromote:
                            if (ad.IsPromoted)
                            {
                                ad.IsPromoted = false;
                                shouldUpdate = true;
                            }
                            else
                                throw new ConflictException("Cannot unpromote an ad that is not promoted.");
                            break;

                        case BulkActionEnum.UnFeature:
                            if (ad.IsFeatured)
                            {
                                ad.IsFeatured = false;
                                shouldUpdate = true;
                            }
                            else
                                throw new ConflictException("Cannot unfeature an ad that is not featured.");
                            break;

                        case BulkActionEnum.Promote:
                            if (!ad.IsPromoted)
                            {
                                ad.IsPromoted = true;
                                shouldUpdate = true;
                            }
                            else
                                throw new ConflictException("Cannot promote an ad that is already promoted.");
                            break;

                        case BulkActionEnum.Feature:
                            if (!ad.IsFeatured)
                            {
                                ad.IsFeatured = true;
                                shouldUpdate = true;
                            }
                            else
                                throw new ConflictException("Cannot feature an ad that is already featured.");
                            break;

                        case BulkActionEnum.Remove:
                            ad.Status = AdStatus.Rejected;
                            shouldUpdate = true;
                            break;

                        case BulkActionEnum.Hold:
                            if (ad.Status == AdStatus.Draft)
                            {
                                throw new ConflictException("Cannot hold an ad that is in draft status.");
                            }
                            else if (ad.Status != AdStatus.Hold)
                            {
                                ad.Status = AdStatus.Hold;
                                shouldUpdate = true;
                            }
                            else
                            {
                                throw new ConflictException("Ad is already on hold.");
                            }
                            break;

                        case BulkActionEnum.Onhold:
                            if (ad.Status != AdStatus.Onhold)
                            {
                                ad.Status = AdStatus.Unpublished;
                                shouldUpdate = true;
                            }
                            else
                                throw new ConflictException("Ad is not on hold.");
                            break;

                        default:
                            throw new InvalidOperationException("Invalid action");
                    }

                    if (shouldUpdate)
                    {
                        ad.UpdatedAt = DateTime.UtcNow;
                        ad.UpdatedBy = userId;
                        updated.Add(ad);
                    }

                    if (updated.Any())
                    {
                        await _context.SaveChangesAsync(ct);

                        foreach (var updatedAd in updated)
                        {
                            await IndexDealsToAzureSearch(updatedAd, ct);
                        }
                    }
                }
                return "Action completed successfully";
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new ConflictException(ex.Message);
            }
            catch
            {
                throw;
            }
        }



        //public async Task<List<SubscriptionTypes>> GetSubscriptionTypes(CancellationToken cancellationToken = default)
        //{
        //    try
        //    {
        //        var getSubscriptionTypes = await _context.SubscriptionType.AsNoTracking().ToListAsync();
        //        return getSubscriptionTypes;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error while getting subscription types.");
        //        return new List<SubscriptionTypes>();
        //    }
        //}
        //public async Task<SubscriptionTypes> GetSubscriptionById(int Id, CancellationToken cancellationToken = default)
        //{
        //    try
        //    {
        //        var getSubscriptionType = await _context.SubscriptionType.AsNoTracking().Where(x => x.SubscriptionId == Id).FirstOrDefaultAsync();
        //        return getSubscriptionType ?? new SubscriptionTypes();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error while getting subscription types.");
        //        return new SubscriptionTypes();
        //    }
        //}

    }
}
