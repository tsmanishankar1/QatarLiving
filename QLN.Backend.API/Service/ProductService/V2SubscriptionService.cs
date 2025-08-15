using Dapr.Actors.Client;
using Dapr.Actors;
using QLN.Common.Infrastructure.IService.IProductService;
using QLN.Common.Infrastructure.QLDbContext;
using QLN.Common.Infrastructure.Subscriptions;
using Microsoft.EntityFrameworkCore;
using QLN.Common.Infrastructure.Model;
using QLN.Common.DTO_s.Subscription;
using QLN.Common.DTO_s.Payments;
using QLN.Common.DTO_s;

namespace QLN.Backend.API.Service.ProductService
{
    public class V2SubscriptionService : IV2SubscriptionService
    {
        private readonly ILogger<V2SubscriptionService> _logger;
        private readonly IActorProxyFactory _actorProxyFactory;
        private readonly QLSubscriptionContext _context;

        public V2SubscriptionService(
            ILogger<V2SubscriptionService> logger,
            IActorProxyFactory actorProxyFactory,
            QLSubscriptionContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _actorProxyFactory = actorProxyFactory;
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        #region Actor Proxy Helpers

        private IV2SubscriptionActor GetV2SubscriptionActorProxy(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("V2 Actor ID cannot be empty", nameof(id));

            return _actorProxyFactory.CreateActorProxy<IV2SubscriptionActor>(
                new ActorId(id.ToString()),
                "V2SubscriptionActor");
        }

        private IV2UserAddonActor GetV2AddonActorProxy(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("V2 Addon Actor ID cannot be empty", nameof(id));

            return _actorProxyFactory.CreateActorProxy<IV2UserAddonActor>(
                new ActorId(id.ToString()),
                "V2UserAddonActor");
        }

        #endregion

        #region Subscription Operations

        public async Task<V2SubscriptionGroupResponseDto> GetSubscriptionsByVerticalAsync(
            int verticalTypeId,string userid,
            CancellationToken cancellationToken = default)
        {
            var resultList = new List<V2SubscriptionResponseDto>();
            var vertical = (Vertical)verticalTypeId;

            var dbSubscriptions = await _context.Subscriptions
                .Where(s => (int)s.Vertical == verticalTypeId &&
                            s.Status == SubscriptionStatus.Active &&
                            s.EndDate > DateTime.UtcNow)
                .ToListAsync(cancellationToken);

            foreach (var dbSub in dbSubscriptions)
            {
                try
                {
                    var actor = GetV2SubscriptionActorProxy(dbSub.SubscriptionId);
                    var actorData = await actor.GetDataAsync(cancellationToken);

                    if (actorData != null)
                    {
                        resultList.Add(MapToResponseDto(actorData));
                    }
                    else
                    {
                        var v2Data = MapDbToV2Dto(dbSub);
                        await actor.FastSetDataAsync(v2Data, cancellationToken);
                        resultList.Add(MapToResponseDto(v2Data));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting subscription data for ID: {Id}", dbSub.SubscriptionId);
                    var v2Data = MapDbToV2Dto(dbSub);
                    resultList.Add(MapToResponseDto(v2Data));
                }
            }

            return new V2SubscriptionGroupResponseDto
            {
                VerticalTypeId = verticalTypeId,
                VerticalName = vertical.ToString(),
                Subscriptions = resultList,
                TotalCount = resultList.Count,
                Version = "V2"
            };
        }

        public async Task<Guid> PurchaseSubscriptionAsync(V2SubscriptionPurchaseRequestDto request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductCode == request.ProductCode && p.IsActive, cancellationToken);

                if (product == null)
                    throw new InvalidOperationException($"Product with code {request.ProductCode} not found or inactive");

                if (product.ProductType != ProductType.SUBSCRIPTION && product.ProductType != ProductType.PUBLISH)
                    throw new InvalidOperationException($"Product {request.ProductCode} is not a subscription or P2P product");

                var subscriptionId = Guid.NewGuid();

                var dbSubscription = new Subscription
                {
                    SubscriptionId = subscriptionId,
                    ProductCode = product.ProductCode,
                    ProductName = product.ProductName,
                    UserId = request.UserId,
                    AdId = request.AdId ?? null,
                    CompanyId = request.CompanyId,
                    PaymentId = request.PaymentId,
                    Vertical = product.Vertical,
                    SubVertical = product.SubVertical,             
                    Quota = BuildSubscriptionQuotaFromProduct(product),
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.Add(GetDurationFromProduct(product)),
                    Status = SubscriptionStatus.PaymentPending,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Subscriptions.Add(dbSubscription);
                await _context.SaveChangesAsync(cancellationToken);

                var v2Dto = new V2SubscriptionDto
                {
                    Id = subscriptionId,
                    ProductCode = product.ProductCode,
                    ProductName = product.ProductName,
                    UserId = request.UserId,
                    CompanyId = request.CompanyId,
                    PaymentId = request.PaymentId,
                    Vertical = product.Vertical,                    
                    SubVertical = product.SubVertical,              
                    Price = product.Price,
                    Currency = product.Currency,
                    Quota = dbSubscription.Quota,
                    StartDate = dbSubscription.StartDate,
                    EndDate = dbSubscription.EndDate,
                    StatusId = V2Status.Active,
                    lastUpdated = DateTime.UtcNow,
                    Version = "V2"
                };

                var actor = GetV2SubscriptionActorProxy(subscriptionId);
                var actorResult = await actor.FastSetDataAsync(v2Dto, cancellationToken);
                if (!actorResult) throw new Exception("Failed to save subscription to actor");

                await transaction.CommitAsync(cancellationToken);
                return subscriptionId;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to purchase V2 subscription");
                throw;
            }
        }

        public async Task<List<V2SubscriptionResponseDto>> GetUserSubscriptionsAsync(
            Vertical? vertical,
            SubVertical? subVertical,
            string userId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting V2 subscriptions for user: {UserId}", userId);

            var query = _context.Subscriptions
                .Where(s => s.UserId == userId && s.Status != SubscriptionStatus.Expired);

            if (vertical.HasValue)
            {
                query = query.Where(s => s.Vertical == vertical.Value);
            }

            if (subVertical.HasValue)
            {
                query = query.Where(s => s.SubVertical == subVertical.Value);
            }

            var dbSubscriptions = await query.ToListAsync(cancellationToken);

            var subscriptions = new List<V2SubscriptionResponseDto>();

            foreach (var dbSub in dbSubscriptions)
            {
                try
                {
                    var actor = GetV2SubscriptionActorProxy(dbSub.SubscriptionId);
                    var actorData = await actor.GetDataAsync(cancellationToken);

                    if (actorData != null)
                    {
                        subscriptions.Add(MapToResponseDto(actorData));
                    }
                    else
                    {
                        var v2Data = MapDbToV2Dto(dbSub);
                        await actor.FastSetDataAsync(v2Data, cancellationToken);
                        subscriptions.Add(MapToResponseDto(v2Data));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting subscription {Id}, using DB data", dbSub.SubscriptionId);
                    var v2Data = MapDbToV2Dto(dbSub);
                    subscriptions.Add(MapToResponseDto(v2Data));
                }
            }

            return subscriptions;
        }

        public async Task<List<V2SubscriptionResponseDto>> GetAllActiveSubscriptionsAsync(string userid, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting all active V2 subscriptions");

            var dbSubscriptions = await _context.Subscriptions
                .Where(s => s.Status == SubscriptionStatus.Active && s.EndDate > DateTime.UtcNow)
                .ToListAsync(cancellationToken);

            var subscriptions = new List<V2SubscriptionResponseDto>();

            foreach (var dbSub in dbSubscriptions)
            {
                try
                {
                    var actor = GetV2SubscriptionActorProxy(dbSub.SubscriptionId);
                    var isActive = await actor.IsActiveAsync(cancellationToken);

                    if (isActive)
                    {
                        var actorData = await actor.GetDataAsync(cancellationToken);
                        if (actorData != null)
                        {
                            subscriptions.Add(MapToResponseDto(actorData));
                        }
                        else
                        {
                            var v2Data = MapDbToV2Dto(dbSub);
                            await actor.FastSetDataAsync(v2Data, cancellationToken);
                            subscriptions.Add(MapToResponseDto(v2Data));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting subscription {Id}, using DB data", dbSub.SubscriptionId);
                    var v2Data = MapDbToV2Dto(dbSub);
                    subscriptions.Add(MapToResponseDto(v2Data));
                }
            }

            return subscriptions;
        }

        public async Task<bool> CancelSubscriptionAsync(Guid subscriptionId, string userid ,CancellationToken cancellationToken = default)
        {
            if (subscriptionId == Guid.Empty)
                throw new ArgumentException("V2 Subscription ID cannot be empty", nameof(subscriptionId));

            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var dbSubscription = await _context.Subscriptions
                    .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId, cancellationToken);

                if (dbSubscription == null) return false;

                dbSubscription.Status = SubscriptionStatus.Cancelled;
                dbSubscription.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                var actor = GetV2SubscriptionActorProxy(subscriptionId);
                var existingData = await actor.GetDataAsync(cancellationToken);

                if (existingData != null)
                {
                    existingData.StatusId = V2Status.Cancelled;
                    existingData.lastUpdated = DateTime.UtcNow;
                    await actor.FastSetDataAsync(existingData, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("V2 Subscription with ID {Id} cancelled.", subscriptionId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error cancelling V2 subscription with ID: {Id}", subscriptionId);
                throw;
            }
        }

        public async Task<bool> ValidateSubscriptionUsageAsync(Guid subscriptionId, string quotaType, int requestedAmount, CancellationToken cancellationToken = default)
        {
            try
            {
                
                var actor = GetV2SubscriptionActorProxy(subscriptionId);
                return await actor.ValidateUsageAsync(quotaType, requestedAmount, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating V2 subscription usage for {SubscriptionId}", subscriptionId);
                return false;
            }
        }

        public async Task<bool> RecordSubscriptionUsageAsync(Guid subscriptionId,string quotaType, int amount, CancellationToken cancellationToken = default)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                
                var actor = GetV2SubscriptionActorProxy(subscriptionId);
                var actorResult = await actor.RecordUsageAsync(quotaType, amount, cancellationToken);
                if (!actorResult)
                {
                    _logger.LogWarning("Actor failed to record usage for subscription {SubscriptionId}", subscriptionId);
                    return false;
                }

                var dbSubscription = await _context.Subscriptions
                    .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId, cancellationToken);

                if (dbSubscription != null)
                {
                    var ok = dbSubscription.Quota.RecordUsage(
     MapQuotaTypeToAction(quotaType),
     amount
 );
                    if (ok)
                    {
                        dbSubscription.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync(cancellationToken);
                    }
                }

                await transaction.CommitAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error recording V2 subscription usage for {SubscriptionId}", subscriptionId);
                return false;
            }
        }

        #endregion

        #region Addon Operations

        public async Task<Guid> PurchaseAddonAsync(V2UserAddonPurchaseRequestDto request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductCode == request.ProductCode && p.IsActive, cancellationToken);

                if (product == null)
                    throw new InvalidOperationException($"Addon product with code {request.ProductCode} not found or inactive");

                var addonTypes = new[] { ProductType.ADDON_COMBO, ProductType.ADDON_FEATURE, ProductType.ADDON_REFRESH };
                if (!addonTypes.Contains(product.ProductType))
                    throw new InvalidOperationException($"Product {request.ProductCode} is not an addon product");

                var addonId = Guid.NewGuid();

                var dbAddon = new UserAddOn
                {
                    UserAddOnId = addonId,
                    ProductCode = product.ProductCode,
                    ProductName = product.ProductName,
                    UserId = request.UserId,
                    CompanyId = request.CompanyId,
                    SubscriptionId = request.SubscriptionId,
                    PaymentId = request.PaymentId,
                    Vertical = product.Vertical,
                    SubVertical = product.SubVertical,             
                    Quota = BuildAddonQuotaFromProduct(product),
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.Add(GetDurationFromProduct(product)),
                    Status = SubscriptionStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserAddOns.Add(dbAddon);
                await _context.SaveChangesAsync(cancellationToken);

                var v2AddonDto = new V2UserAddonDto
                {
                    Id = addonId,
                    ProductCode = product.ProductCode,
                    ProductName = product.ProductName,
                    UserId = request.UserId,
                    CompanyId = request.CompanyId,
                    SubscriptionId = request.SubscriptionId,
                    PaymentId = request.PaymentId,
                    Vertical = product.Vertical,                 
                    SubVertical = product.SubVertical,           
                    Price = product.Price,
                    Currency = product.Currency,
                    Quota = dbAddon.Quota,
                    StartDate = dbAddon.StartDate,
                    EndDate = dbAddon.EndDate,
                    StatusId = V2Status.Active,
                    lastUpdated = DateTime.UtcNow,
                    Version = "V2"
                };

                var actor = GetV2AddonActorProxy(addonId);
                var actorResult = await actor.FastSetDataAsync(v2AddonDto, cancellationToken);
                if (!actorResult) throw new Exception("Failed to save addon to actor");

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("V2 Addon purchased successfully: {Id} for user: {UserId}", addonId, request.UserId);
                return addonId;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Failed to purchase V2 addon");
                throw;
            }
        }

        public async Task<bool> ValidateAddonUsageAsync(Guid addonId,string quotaType, int requestedAmount, CancellationToken cancellationToken = default)
        {
            try
            {
                var actor = GetV2AddonActorProxy(addonId);
                return await actor.ValidateUsageAsync(quotaType, requestedAmount, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating V2 addon usage for {AddonId}", addonId);
                return false;
            }
        }

        public async Task<bool> RecordAddonUsageAsync(Guid addonId, string quotaType, int amount, CancellationToken cancellationToken = default)
        {
            try
            {
                var actor = GetV2AddonActorProxy(addonId);
                return await actor.RecordUsageAsync(quotaType, amount, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording V2 addon usage for {AddonId}", addonId);
                return false;
            }
        }

        public async Task<List<V2UserAddonResponseDto>> GetUserAddonsAsync(string userId, CancellationToken cancellationToken = default)
        {
            var dbAddons = await _context.UserAddOns
                .Where(a => a.UserId == userId && a.Status != SubscriptionStatus.Expired)
                .ToListAsync(cancellationToken);

            var userAddons = new List<V2UserAddonResponseDto>();

            foreach (var dbAddon in dbAddons)
            {
                try
                {
                    var actor = GetV2AddonActorProxy(dbAddon.UserAddOnId);
                    var actorData = await actor.GetDataAsync(cancellationToken);

                    if (actorData != null)
                    {
                        userAddons.Add(MapAddonToResponseDto(actorData));
                    }
                    else
                    {
                        var v2AddonData = MapDbAddonToV2Dto(dbAddon);
                        await actor.FastSetDataAsync(v2AddonData, cancellationToken);
                        userAddons.Add(MapAddonToResponseDto(v2AddonData));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting V2 addon data for ID: {Id}", dbAddon.UserAddOnId);
                }
            }

            return userAddons;
        }

        #endregion

        #region Expiration Management

        public async Task<List<Guid>> GetExpiredSubscriptionsAsync(CancellationToken cancellationToken = default)
        {
            var expiredSubscriptions = await _context.Subscriptions
                .Where(s => s.Status == SubscriptionStatus.Active && s.EndDate <= DateTime.UtcNow)
                .Select(s => s.SubscriptionId)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} expired subscriptions", expiredSubscriptions.Count);
            return expiredSubscriptions;
        }

        public async Task<bool> MarkSubscriptionAsExpiredAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var dbSubscription = await _context.Subscriptions
                    .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId, cancellationToken);

                if (dbSubscription == null) return false;

                dbSubscription.Status = SubscriptionStatus.Expired;
                dbSubscription.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                var actor = GetV2SubscriptionActorProxy(subscriptionId);
                var existingData = await actor.GetDataAsync(cancellationToken);

                if (existingData != null)
                {
                    existingData.StatusId = V2Status.Expired;
                    existingData.lastUpdated = DateTime.UtcNow;
                    await actor.FastSetDataAsync(existingData, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Subscription {Id} marked as expired", subscriptionId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error marking subscription {Id} as expired", subscriptionId);
                return false;
            }
        }

        public async Task<List<Guid>> GetExpiredAddonsAsync(CancellationToken cancellationToken = default)
        {
            var expiredAddons = await _context.UserAddOns
                .Where(a => a.Status == SubscriptionStatus.Active && a.EndDate <= DateTime.UtcNow)
                .Select(a => a.UserAddOnId)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} expired addons", expiredAddons.Count);
            return expiredAddons;
        }

        public async Task<bool> MarkAddonAsExpiredAsync(Guid addonId, CancellationToken cancellationToken = default)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var dbAddon = await _context.UserAddOns
                    .FirstOrDefaultAsync(a => a.UserAddOnId == addonId, cancellationToken);

                if (dbAddon == null) return false;

                dbAddon.Status = SubscriptionStatus.Expired;
                dbAddon.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                var actor = GetV2AddonActorProxy(addonId);
                var existingData = await actor.GetDataAsync(cancellationToken);

                if (existingData != null)
                {
                    existingData.StatusId = V2Status.Expired;
                    existingData.lastUpdated = DateTime.UtcNow;
                    await actor.FastSetDataAsync(existingData, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Addon {Id} marked as expired", addonId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error marking addon {Id} as expired", addonId);
                return false;
            }
        }

        #endregion

        #region Advanced Operations

        public async Task<V2PaginatedResponseDto<V2SubscriptionResponseDto>> GetSubscriptionsAsync(
            V2SubscriptionFilterDto filter,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Subscriptions.AsQueryable();

            if (!string.IsNullOrEmpty(filter.UserId))
                query = query.Where(s => s.UserId == filter.UserId);

            if (filter.CompanyId.HasValue)
                query = query.Where(s => s.CompanyId == filter.CompanyId);

            
            query = query.Where(s => s.Vertical == filter.Vertical);

            if (filter.SubVertical.HasValue)
                query = query.Where(s => s.SubVertical == filter.SubVertical);

            if (filter.StatusId.HasValue)
                query = query.Where(s => (int)s.Status == (int)MapToDbStatus(filter.StatusId.Value));

            if (filter.StartDateFrom.HasValue)
                query = query.Where(s => s.StartDate >= filter.StartDateFrom);

            if (filter.StartDateTo.HasValue)
                query = query.Where(s => s.StartDate <= filter.StartDateTo);

            if (filter.EndDateFrom.HasValue)
                query = query.Where(s => s.EndDate >= filter.EndDateFrom);

            if (filter.EndDateTo.HasValue)
                query = query.Where(s => s.EndDate <= filter.EndDateTo);

            if (filter.IsActive.HasValue && filter.IsActive.Value)
                query = query.Where(s => s.Status == SubscriptionStatus.Active && s.EndDate > DateTime.UtcNow);

            if (filter.IsExpired.HasValue && filter.IsExpired.Value)
                query = query.Where(s => s.Status == SubscriptionStatus.Expired || s.EndDate <= DateTime.UtcNow);

            var totalCount = await query.CountAsync(cancellationToken);
            var totalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize);

            var subscriptions = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync(cancellationToken);

            var responseData = new List<V2SubscriptionResponseDto>();
            foreach (var dbSub in subscriptions)
            {
                var v2Data = MapDbToV2Dto(dbSub);
                responseData.Add(MapToResponseDto(v2Data));
            }

            return new V2PaginatedResponseDto<V2SubscriptionResponseDto>
            {
                Data = responseData,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = filter.Page < totalPages,
                HasPreviousPage = filter.Page > 1,
                Version = "V2"
            };
        }

        public async Task<V2SubscriptionResponseDto?> GetSubscriptionByIdAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var actor = GetV2SubscriptionActorProxy(subscriptionId);
                var actorData = await actor.GetDataAsync(cancellationToken);

                if (actorData != null)
                    return MapToResponseDto(actorData);

                var dbSubscription = await _context.Subscriptions
                    .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId, cancellationToken);

                if (dbSubscription != null)
                {
                    var v2Data = MapDbToV2Dto(dbSubscription);
                    await actor.FastSetDataAsync(v2Data, cancellationToken);
                    return MapToResponseDto(v2Data);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription by ID: {SubscriptionId}", subscriptionId);
                return null;
            }
        }

        public async Task<V2UserAddonResponseDto?> GetAddonByIdAsync(Guid addonId, CancellationToken cancellationToken = default)
        {
            try
            {
                var actor = GetV2AddonActorProxy(addonId);
                var actorData = await actor.GetDataAsync(cancellationToken);

                if (actorData != null)
                    return MapAddonToResponseDto(actorData);

                var dbAddon = await _context.UserAddOns
                    .FirstOrDefaultAsync(a => a.UserAddOnId == addonId, cancellationToken);

                if (dbAddon != null)
                {
                    var v2Data = MapDbAddonToV2Dto(dbAddon);
                    await actor.FastSetDataAsync(v2Data, cancellationToken);
                    return MapAddonToResponseDto(v2Data);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting addon by ID: {AddonId}", addonId);
                return null;
            }
        }

        public async Task<bool> ExtendSubscriptionAsync(Guid subscriptionId, TimeSpan additionalDuration, CancellationToken cancellationToken = default)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var dbSubscription = await _context.Subscriptions
                    .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId, cancellationToken);

                if (dbSubscription == null) return false;

                dbSubscription.EndDate = dbSubscription.EndDate.Add(additionalDuration);
                dbSubscription.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                var actor = GetV2SubscriptionActorProxy(subscriptionId);
                var actorData = await actor.GetDataAsync(cancellationToken);

                if (actorData != null)
                {
                    actorData.EndDate = dbSubscription.EndDate;
                    actorData.lastUpdated = DateTime.UtcNow;
                    await actor.FastSetDataAsync(actorData, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Subscription {Id} extended by {Duration}", subscriptionId, additionalDuration);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error extending subscription {Id}", subscriptionId);
                return false;
            }
        }

        public async Task<bool> RefillSubscriptionQuotaAsync(Guid subscriptionId, string quotaType, decimal amount, CancellationToken cancellationToken = default)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var dbSubscription = await _context.Subscriptions
                    .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId, cancellationToken);

                if (dbSubscription == null) return false;

                var qty = (int)Math.Ceiling(amount);
                switch ((quotaType ?? string.Empty).ToLower())
                {
                    case V2QuotaTypes.AdsBudget: dbSubscription.Quota.TotalAdsAllowed += qty; break;
                    case V2QuotaTypes.PromoteBudget: dbSubscription.Quota.TotalPromotionsAllowed += qty; break;
                    case V2QuotaTypes.FeatureBudget: dbSubscription.Quota.TotalFeaturesAllowed += qty; break;
                    case V2QuotaTypes.RefreshBudget: dbSubscription.Quota.DailyRefreshesAllowed += qty; break;
                    case V2QuotaTypes.SocialMediaPosts: dbSubscription.Quota.SocialMediaPostsAllowed += qty; break;
                    default:
                        _logger.LogWarning("Unknown quotaType '{QuotaType}' in refill; no-op", quotaType);
                        break;
                }

                dbSubscription.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                var actor = GetV2SubscriptionActorProxy(subscriptionId);
                var actorData = await actor.GetDataAsync(cancellationToken);
                if (actorData != null)
                {
                    actorData.Quota = dbSubscription.Quota;
                    actorData.lastUpdated = DateTime.UtcNow;
                    await actor.FastSetDataAsync(actorData, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation("Subscription {Id} quota {QuotaType} refilled by {Amount}", subscriptionId, quotaType, amount);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error refilling subscription {Id} quota", subscriptionId);
                return false;
            }
        }

        #endregion

        #region Helper Methods

        private V2SubscriptionDto MapDbToV2Dto(Subscription dbSub)
        {
            return new V2SubscriptionDto
            {
                Id = dbSub.SubscriptionId,
                ProductCode = dbSub.ProductCode,
                ProductName = "Subscription",
                UserId = dbSub.UserId,
                CompanyId = dbSub.CompanyId,
                PaymentId = dbSub.PaymentId,
                Vertical = dbSub.Vertical,              // 
                SubVertical = dbSub.SubVertical,        // 
                Price = 0,
                Currency = "QAR",
                Quota = dbSub.Quota,
                StartDate = dbSub.StartDate,
                EndDate = dbSub.EndDate,
                StatusId = MapFromDbStatus(dbSub.Status),
                lastUpdated = dbSub.UpdatedAt ?? dbSub.CreatedAt,
                Version = "V2"
            };
        }

        private V2UserAddonDto MapDbAddonToV2Dto(UserAddOn dbAddon)
        {
            return new V2UserAddonDto
            {
                Id = dbAddon.UserAddOnId,
                ProductCode = dbAddon.ProductCode,
                ProductName = "Addon",
                UserId = dbAddon.UserId ?? string.Empty,
                CompanyId = dbAddon.CompanyId,
                SubscriptionId = dbAddon.SubscriptionId,
                PaymentId = dbAddon.PaymentId,
                Vertical = dbAddon.Vertical,            
                SubVertical = dbAddon.SubVertical,       
                Price = 0,
                Currency = "QAR",
                Quota = dbAddon.Quota,
                StartDate = dbAddon.StartDate,
                EndDate = dbAddon.EndDate,
                StatusId = MapFromDbStatus(dbAddon.Status),
                lastUpdated = dbAddon.UpdatedAt ?? dbAddon.CreatedAt,
                Version = "V2"
            };
        }

        private V2SubscriptionResponseDto MapToResponseDto(V2SubscriptionDto v2Data)
        {
            var isActive = v2Data.StatusId == V2Status.Active && v2Data.EndDate > DateTime.UtcNow;
            var daysRemaining = isActive ? (int)(v2Data.EndDate - DateTime.UtcNow).TotalDays : 0;

            return new V2SubscriptionResponseDto
            {
                Id = v2Data.Id,
                ProductCode = v2Data.ProductCode,
                ProductName = v2Data.ProductName,
                UserId = v2Data.UserId,
                VerticalName = v2Data.Vertical.ToString(),
                Vertical = v2Data.Vertical,                 
                SubVertical = v2Data.SubVertical,           
                Price = v2Data.Price,
                Currency = v2Data.Currency,
                Quota = v2Data.Quota,
                StartDate = v2Data.StartDate,
                EndDate = v2Data.EndDate,
                StatusId = v2Data.StatusId,
                StatusName = v2Data.StatusId.ToString(),
                IsActive = isActive,
                DaysRemaining = daysRemaining,
                Version = "V2"
            };
        }

        private V2UserAddonResponseDto MapAddonToResponseDto(V2UserAddonDto v2Data)
        {
            var isActive = v2Data.StatusId == V2Status.Active && v2Data.EndDate > DateTime.UtcNow;
            var daysRemaining = isActive ? (int)(v2Data.EndDate - DateTime.UtcNow).TotalDays : 0;

            return new V2UserAddonResponseDto
            {
                Id = v2Data.Id,
                ProductCode = v2Data.ProductCode,
                ProductName = v2Data.ProductName,
                UserId = v2Data.UserId,
                SubscriptionId = v2Data.SubscriptionId,
                VerticalName = v2Data.Vertical.ToString(),
                Vertical = v2Data.Vertical,                 
                SubVertical = v2Data.SubVertical,           
                Price = v2Data.Price,
                Currency = v2Data.Currency,
                Quota = v2Data.Quota,
                StartDate = v2Data.StartDate,
                EndDate = v2Data.EndDate,
                StatusId = v2Data.StatusId,
                StatusName = v2Data.StatusId.ToString(),
                IsActive = isActive,
                DaysRemaining = daysRemaining,
                Version = "V2"
            };
        }

        private SubscriptionQuota BuildSubscriptionQuotaFromProduct(Product product)
        {
            var c = product.Constraints ?? new ProductConstraints();

            var q = new SubscriptionQuota
            {
                Vertical = product.Vertical.ToString(),
                Scope = c.Scope ?? "All",

                TotalAdsAllowed = c.AdsBudget ?? 0,
                TotalPromotionsAllowed = c.PromotedBudget ?? 0,
                TotalFeaturesAllowed = c.FeaturedBudget ?? 0,
                DailyRefreshesAllowed = c.RefreshBudgetPerDay ?? 0,
                RefreshesPerAdAllowed = c.RefreshBudgetPerAd ?? 1,
                SocialMediaPostsAllowed = 0,

                CanPublishAds = !(c.PayToPublish ?? false) || (c.AdsBudget ?? 0) > 0,
                CanPromoteAds = (c.PayToPromote ?? false) || (c.PromotedBudget ?? 0) > 0,
                CanFeatureAds = (c.PayToFeature ?? false) || (c.FeaturedBudget ?? 0) > 0,
                CanRefreshAds = (c.RefreshBudgetPerDay ?? 0) > 0,
                CanPostSocialMedia = false,

                RefreshInterval = product.ProductType== ProductType.FREE ?"Every 72 Hours": "Every Hour",
                RefreshIntervalHours = product.ProductType == ProductType.FREE ? 72: 1
            };

            return q;
        }

        private SubscriptionQuota BuildAddonQuotaFromProduct(Product product)
        {
            var c = product.Constraints ?? new ProductConstraints();

            var q = new SubscriptionQuota
            {
                Vertical = product.Vertical.ToString(),
                Scope = c.Scope ?? "All",
                CanPublishAds = false,
                CanPromoteAds = false,
                CanFeatureAds = false,
                CanRefreshAds = false,
                CanPostSocialMedia = false,
                RefreshInterval = "Every 72 Hours",
                RefreshIntervalHours = 72
            };

            if (c.PayToPublish == true) { q.CanPublishAds = true; q.TotalAdsAllowed += 1; }
            if (c.PayToPromote == true) { q.CanPromoteAds = true; q.TotalPromotionsAllowed += 1; }
            if (c.PayToFeature == true) { q.CanFeatureAds = true; q.TotalFeaturesAllowed += 1; }

            if ((c.RefreshBudgetPerDay ?? 0) > 0)
            {
                q.CanRefreshAds = true;
            }

            if (c.PromotedBudget.HasValue)
            {
                q.CanPromoteAds = true;
                q.TotalPromotionsAllowed += c.PromotedBudget.Value;
            }
            if (c.FeaturedBudget.HasValue)
            {
                q.CanFeatureAds = true;
                q.TotalFeaturesAllowed += c.FeaturedBudget.Value;
            }
            if (c.AdsBudget.HasValue)
            {
                q.CanPublishAds = true;
                q.TotalAdsAllowed += c.AdsBudget.Value;
            }
            if (c.RefreshBudgetPerAd.HasValue)
            {
                q.CanRefreshAds = true;
                q.RefreshesPerAdAllowed = Math.Max(q.RefreshesPerAdAllowed, c.RefreshBudgetPerAd.Value);
            }

            return q;
        }

        private static string MapQuotaTypeToAction(string quotaType) =>
            (quotaType ?? string.Empty).ToLower() switch
            {
                V2QuotaTypes.AdsBudget => ActionTypes.Publish,
                V2QuotaTypes.PromoteBudget => ActionTypes.Promote,
                V2QuotaTypes.FeatureBudget => ActionTypes.Feature,
                V2QuotaTypes.RefreshBudget => ActionTypes.Refresh,
                V2QuotaTypes.SocialMediaPosts => ActionTypes.SocialMediaPost,
                _ => quotaType?.ToLower() ?? string.Empty
            };

        private TimeSpan GetDurationFromProduct(Product product)
        {
            
            if (product.Constraints?.Duration.HasValue == true)
            {
                return product.Constraints.Duration.Value;
            }

            return product.ProductType switch
            {
                ProductType.SUBSCRIPTION => TimeSpan.FromDays(30),
                ProductType.ADDON_COMBO => TimeSpan.FromDays(30),
                ProductType.ADDON_FEATURE => TimeSpan.FromDays(7),
                ProductType.ADDON_REFRESH => TimeSpan.FromDays(30),
                _ => TimeSpan.FromDays(30)
            };
        }

        private SubscriptionStatus MapToDbStatus(V2Status v2Status) =>
            v2Status switch
            {
                V2Status.Active => SubscriptionStatus.Active,
                V2Status.Expired => SubscriptionStatus.Expired,
                V2Status.Cancelled => SubscriptionStatus.Cancelled,
                V2Status.Suspended => SubscriptionStatus.Suspended,
                V2Status.PaymentPending => SubscriptionStatus.PaymentPending,
                _ => SubscriptionStatus.PaymentPending
            };

        private V2Status MapFromDbStatus(SubscriptionStatus dbStatus) =>
            dbStatus switch
            {
                SubscriptionStatus.Active => V2Status.Active,
                SubscriptionStatus.Expired => V2Status.Expired,
                SubscriptionStatus.Cancelled => V2Status.Cancelled,
                SubscriptionStatus.Suspended => V2Status.Suspended,
                SubscriptionStatus.PaymentPending => V2Status.PaymentPending,
                _ => V2Status.PaymentPending
            };

        #endregion
    }
}
