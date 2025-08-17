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
        private readonly QLSubscriptionContext _context; // Only for read operations and bulk queries

        public V2SubscriptionService(
            ILogger<V2SubscriptionService> logger,
            IActorProxyFactory actorProxyFactory,
            QLSubscriptionContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _actorProxyFactory = actorProxyFactory ?? throw new ArgumentNullException(nameof(actorProxyFactory));
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
            int verticalTypeId,
            string userid,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting V2 subscriptions by vertical {VerticalTypeId} for user {UserId}", verticalTypeId, userid);

            var resultList = new List<V2SubscriptionResponseDto>();
            var vertical = (Vertical)verticalTypeId;

            // Read from DB to get subscription IDs for the user and vertical
            var subscriptionIds = await _context.Subscriptions
                .Where(s => (int)s.Vertical == verticalTypeId &&
                            s.UserId == userid &&
                            s.Status == SubscriptionStatus.Active &&
                            s.EndDate > DateTime.UtcNow)
                .Select(s => s.SubscriptionId)
                .ToListAsync(cancellationToken);

            // Get data from actors for each subscription
            foreach (var subscriptionId in subscriptionIds)
            {
                try
                {
                    var actor = GetV2SubscriptionActorProxy(subscriptionId);
                    var actorData = await actor.GetDataAsync(cancellationToken);

                    if (actorData != null)
                    {
                        resultList.Add(MapToResponseDto(actorData));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting subscription data for ID: {Id}", subscriptionId);
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

            _logger.LogInformation("Purchasing V2 subscription for user {UserId} with product {ProductCode}", request.UserId, request.ProductCode);

            var subscriptionId = Guid.NewGuid();
            var actor = GetV2SubscriptionActorProxy(subscriptionId);

            // Actor handles all DB operations, transactions, and event publishing
            var success = await actor.CreateSubscriptionAsync(request, cancellationToken);
            if (!success)
            {
                throw new InvalidOperationException("Failed to create subscription via actor");
            }

            _logger.LogInformation("V2 Subscription created successfully: {SubscriptionId}", subscriptionId);
            return subscriptionId;
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

            var subscriptionIds = await query.Select(s => s.SubscriptionId).ToListAsync(cancellationToken);

            var subscriptions = new List<V2SubscriptionResponseDto>();

            foreach (var subscriptionId in subscriptionIds)
            {
                try
                {
                    var actor = GetV2SubscriptionActorProxy(subscriptionId);
                    var actorData = await actor.GetDataAsync(cancellationToken);

                    if (actorData != null)
                    {
                        subscriptions.Add(MapToResponseDto(actorData));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting subscription {Id}", subscriptionId);
                }
            }

            return subscriptions;
        }

        public async Task<List<V2SubscriptionResponseDto>> GetAllActiveSubscriptionsAsync(string userid, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting all active V2 subscriptions for user: {UserId}", userid);

            var subscriptionIds = await _context.Subscriptions
                .Where(s => s.UserId == userid &&
                           s.Status == SubscriptionStatus.Active &&
                           s.EndDate > DateTime.UtcNow)
                .Select(s => s.SubscriptionId)
                .ToListAsync(cancellationToken);

            var subscriptions = new List<V2SubscriptionResponseDto>();

            foreach (var subscriptionId in subscriptionIds)
            {
                try
                {
                    var actor = GetV2SubscriptionActorProxy(subscriptionId);
                    var isActive = await actor.IsActiveAsync(cancellationToken);

                    if (isActive)
                    {
                        var actorData = await actor.GetDataAsync(cancellationToken);
                        if (actorData != null)
                        {
                            subscriptions.Add(MapToResponseDto(actorData));
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting subscription {Id}", subscriptionId);
                }
            }

            return subscriptions;
        }

        public async Task<bool> CancelSubscriptionAsync(Guid subscriptionId, string userid, CancellationToken cancellationToken = default)
        {
            if (subscriptionId == Guid.Empty)
                throw new ArgumentException("V2 Subscription ID cannot be empty", nameof(subscriptionId));

            _logger.LogInformation("Cancelling V2 subscription {SubscriptionId} for user {UserId}", subscriptionId, userid);

            try
            {
                var actor = GetV2SubscriptionActorProxy(subscriptionId);
                // Actor handles DB update, state change, and event publishing
                var result = await actor.CancelSubscriptionAsync(userid, cancellationToken);

                if (result)
                {
                    _logger.LogInformation("V2 Subscription with ID {Id} cancelled for user {UserId}.", subscriptionId, userid);
                }
                else
                {
                    _logger.LogWarning("Failed to cancel V2 subscription {SubscriptionId} for user {UserId}", subscriptionId, userid);
                }

                return result;
            }
            catch (Exception ex)
            {
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

        public async Task<bool> RecordSubscriptionUsageAsync(Guid subscriptionId, string quotaType, int amount, CancellationToken cancellationToken = default)
        {
            try
            {
                var actor = GetV2SubscriptionActorProxy(subscriptionId);
                // Actor handles both actor state update and DB persistence
                return await actor.RecordUsageAsync(quotaType, amount, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording V2 subscription usage for {SubscriptionId}", subscriptionId);
                return false;
            }
        }

        #endregion

        #region Addon Operations

        public async Task<Guid> PurchaseAddonAsync(V2UserAddonPurchaseRequestDto request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            _logger.LogInformation("Purchasing V2 addon for user {UserId} with product {ProductCode}", request.UserId, request.ProductCode);

            var addonId = Guid.NewGuid();
            var actor = GetV2AddonActorProxy(addonId);

            // Actor handles all DB operations, transactions, and event publishing
            var success = await actor.CreateAddonAsync(request, cancellationToken);
            if (!success)
            {
                throw new InvalidOperationException("Failed to create addon via actor");
            }

            _logger.LogInformation("V2 Addon purchased successfully: {Id} for user: {UserId}", addonId, request.UserId);
            return addonId;
        }

        public async Task<bool> ValidateAddonUsageAsync(Guid addonId, string quotaType, int requestedAmount, CancellationToken cancellationToken = default)
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
                // Actor handles both actor state update and DB persistence
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
            _logger.LogInformation("Getting V2 addons for user: {UserId}", userId);

            var addonIds = await _context.UserAddOns
                .Where(a => a.UserId == userId && a.Status != SubscriptionStatus.Expired)
                .Select(a => a.UserAddOnId)
                .ToListAsync(cancellationToken);

            var userAddons = new List<V2UserAddonResponseDto>();

            foreach (var addonId in addonIds)
            {
                try
                {
                    var actor = GetV2AddonActorProxy(addonId);
                    var actorData = await actor.GetDataAsync(cancellationToken);

                    if (actorData != null)
                    {
                        userAddons.Add(MapAddonToResponseDto(actorData));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting V2 addon data for ID: {Id}", addonId);
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
            try
            {
                var actor = GetV2SubscriptionActorProxy(subscriptionId);
                // Actor handles DB update, state change, and event publishing
                var result = await actor.MarkAsExpiredAsync(cancellationToken);

                if (result)
                {
                    _logger.LogInformation("Subscription {Id} marked as expired", subscriptionId);
                }

                return result;
            }
            catch (Exception ex)
            {
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
            try
            {
                var actor = GetV2AddonActorProxy(addonId);
                // Actor handles DB update, state change, and event publishing
                var result = await actor.MarkAsExpiredAsync(cancellationToken);

                if (result)
                {
                    _logger.LogInformation("Addon {Id} marked as expired", addonId);
                }

                return result;
            }
            catch (Exception ex)
            {
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

            // Apply filters
            if (!string.IsNullOrEmpty(filter.UserId))
                query = query.Where(s => s.UserId == filter.UserId);

            if (filter.CompanyId.HasValue)
                query = query.Where(s => s.CompanyId == filter.CompanyId);

            query = query.Where(s => s.Vertical == filter.Vertical);

            if (filter.SubVertical.HasValue)
                query = query.Where(s => s.SubVertical == filter.SubVertical);

            if (filter.StatusId.HasValue)
                query = query.Where(s => s.Status == filter.StatusId.Value);

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

            var subscriptionIds = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(s => s.SubscriptionId)
                .ToListAsync(cancellationToken);

            var responseData = new List<V2SubscriptionResponseDto>();
            foreach (var subscriptionId in subscriptionIds)
            {
                try
                {
                    var actor = GetV2SubscriptionActorProxy(subscriptionId);
                    var actorData = await actor.GetDataAsync(cancellationToken);
                    if (actorData != null)
                    {
                        responseData.Add(MapToResponseDto(actorData));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting subscription {Id} for pagination", subscriptionId);
                }
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

                return actorData != null ? MapToResponseDto(actorData) : null;
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

                return actorData != null ? MapAddonToResponseDto(actorData) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting addon by ID: {AddonId}", addonId);
                return null;
            }
        }

        public async Task<bool> ExtendSubscriptionAsync(Guid subscriptionId, TimeSpan additionalDuration, CancellationToken cancellationToken = default)
        {
            try
            {
                var actor = GetV2SubscriptionActorProxy(subscriptionId);
                // Actor handles DB update and state sync
                var result = await actor.ExtendSubscriptionAsync(additionalDuration, cancellationToken);

                if (result)
                {
                    _logger.LogInformation("Subscription {Id} extended by {Duration}", subscriptionId, additionalDuration);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extending subscription {Id}", subscriptionId);
                return false;
            }
        }

        public async Task<bool> RefillSubscriptionQuotaAsync(Guid subscriptionId, string quotaType, decimal amount, CancellationToken cancellationToken = default)
        {
            try
            {
                var actor = GetV2SubscriptionActorProxy(subscriptionId);
                // Actor handles DB update and state sync
                var result = await actor.RefillQuotaAsync(quotaType, amount, cancellationToken);

                if (result)
                {
                    _logger.LogInformation("Subscription {Id} quota {QuotaType} refilled by {Amount}", subscriptionId, quotaType, amount);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refilling subscription {Id} quota", subscriptionId);
                return false;
            }
        }

        #endregion

        #region Helper Methods

        private V2SubscriptionResponseDto MapToResponseDto(V2SubscriptionDto v2Data)
        {
            var isActive = v2Data.StatusId == SubscriptionStatus.Active && v2Data.EndDate > DateTime.UtcNow;
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
            var isActive = v2Data.StatusId == SubscriptionStatus.Active && v2Data.EndDate > DateTime.UtcNow;
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
        #endregion
        #region Admin Subscription Operations

        public async Task<bool> UpdateSubscriptionStatusAsync(Guid subscriptionId, SubscriptionStatus newStatus, CancellationToken cancellationToken = default)
        {
            try
            {
                var actor = GetV2SubscriptionActorProxy(subscriptionId);
                // Actor handles DB update and state sync
                var result = await actor.UpdateStatusAsync(newStatus, cancellationToken);

                if (result)
                {
                    _logger.LogInformation("Subscription {Id} status updated to {Status} by admin", subscriptionId, newStatus);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subscription {Id} status", subscriptionId);
                return false;
            }
        }

        public async Task<bool> UpdateSubscriptionEndDateAsync(Guid subscriptionId, DateTime newEndDate, CancellationToken cancellationToken = default)
        {
            try
            {
                var actor = GetV2SubscriptionActorProxy(subscriptionId);
                // Actor handles DB update and state sync
                var result = await actor.UpdateEndDateAsync(newEndDate, cancellationToken);

                if (result)
                {
                    _logger.LogInformation("Subscription {Id} end date updated to {EndDate} by admin", subscriptionId, newEndDate);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subscription {Id} end date", subscriptionId);
                return false;
            }
        }

        public async Task<bool> AdminCancelSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
        {
            try
            {
                var actor = GetV2SubscriptionActorProxy(subscriptionId);
                // Admin cancel doesn't require user ID validation
                var result = await actor.AdminCancelSubscriptionAsync(cancellationToken);

                if (result)
                {
                    _logger.LogInformation("Subscription {Id} cancelled by admin", subscriptionId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error admin cancelling subscription {Id}", subscriptionId);
                return false;
            }
        }

        #endregion

        #region Admin Addon Operations

        public async Task<bool> UpdateAddonStatusAsync(Guid addonId, SubscriptionStatus newStatus, CancellationToken cancellationToken = default)
        {
            try
            {
                var actor = GetV2AddonActorProxy(addonId);
                // Actor handles DB update and state sync
                var result = await actor.UpdateStatusAsync(newStatus, cancellationToken);

                if (result)
                {
                    _logger.LogInformation("Addon {Id} status updated to {Status} by admin", addonId, newStatus);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating addon {Id} status", addonId);
                return false;
            }
        }

        public async Task<bool> UpdateAddonEndDateAsync(Guid addonId, DateTime newEndDate, CancellationToken cancellationToken = default)
        {
            try
            {
                var actor = GetV2AddonActorProxy(addonId);
                // Actor handles DB update and state sync
                var result = await actor.UpdateEndDateAsync(newEndDate, cancellationToken);

                if (result)
                {
                    _logger.LogInformation("Addon {Id} end date updated to {EndDate} by admin", addonId, newEndDate);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating addon {Id} end date", addonId);
                return false;
            }
        }

        public async Task<bool> ExtendAddonAsync(Guid addonId, TimeSpan additionalDuration, CancellationToken cancellationToken = default)
        {
            try
            {
                var actor = GetV2AddonActorProxy(addonId);
                // Actor handles DB update and state sync
                var result = await actor.ExtendAddonAsync(additionalDuration, cancellationToken);

                if (result)
                {
                    _logger.LogInformation("Addon {Id} extended by {Duration}", addonId, additionalDuration);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extending addon {Id}", addonId);
                return false;
            }
        }

        public async Task<bool> RefillAddonQuotaAsync(Guid addonId, string quotaType, decimal amount, CancellationToken cancellationToken = default)
        {
            try
            {
                var actor = GetV2AddonActorProxy(addonId);
                // Actor handles DB update and state sync
                var result = await actor.RefillQuotaAsync(quotaType, amount, cancellationToken);

                if (result)
                {
                    _logger.LogInformation("Addon {Id} quota {QuotaType} refilled by {Amount}", addonId, quotaType, amount);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refilling addon {Id} quota", addonId);
                return false;
            }
        }

        public async Task<bool> AdminCancelAddonAsync(Guid addonId, CancellationToken cancellationToken = default)
        {
            try
            {
                var actor = GetV2AddonActorProxy(addonId);
                // Admin cancel doesn't require user ID validation
                var result = await actor.AdminCancelAddonAsync(cancellationToken);

                if (result)
                {
                    _logger.LogInformation("Addon {Id} cancelled by admin", addonId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error admin cancelling addon {Id}", addonId);
                return false;
            }
        }

        public async Task<bool> CancelAddonAsync(Guid addonId, string userId, CancellationToken cancellationToken = default)
        {
            if (addonId == Guid.Empty)
                throw new ArgumentException("V2 Addon ID cannot be empty", nameof(addonId));

            _logger.LogInformation("Cancelling V2 addon {AddonId} for user {UserId}", addonId, userId);

            try
            {
                var actor = GetV2AddonActorProxy(addonId);
                // Actor handles DB update, state change, and event publishing
                var result = await actor.CancelAddonAsync(userId, cancellationToken);

                if (result)
                {
                    _logger.LogInformation("V2 Addon with ID {Id} cancelled for user {UserId}.", addonId, userId);
                }
                else
                {
                    _logger.LogWarning("Failed to cancel V2 addon {AddonId} for user {UserId}", addonId, userId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling V2 addon with ID: {Id}", addonId);
                throw;
            }
        }

        #endregion
    }
}
