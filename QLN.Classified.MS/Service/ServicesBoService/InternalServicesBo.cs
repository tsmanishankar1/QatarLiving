using Dapr.Client;
using Microsoft.EntityFrameworkCore;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.IServiceBoService;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.QLDbContext;
using QLN.Common.Infrastructure.Subscriptions;
using System.Linq;


namespace QLN.Classified.MS.Service.ServicesBoService
{
    public class InternalServicesBo : IServicesBoService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<InternalServicesBo> _logger;
        private readonly QLClassifiedContext _dbContext;
        private readonly QLPaymentsContext _paymentsContext;
        private readonly QLSubscriptionContext _subscriptionContext;

        public InternalServicesBo(
            DaprClient dapr,
            ILogger<InternalServicesBo> logger, QLClassifiedContext dbContext, QLPaymentsContext paymentsContext, QLSubscriptionContext subscriptionContext)
        {
            _dapr = dapr ?? throw new ArgumentNullException(nameof(dapr));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dbContext = dbContext;
            _paymentsContext = paymentsContext;
            _subscriptionContext = subscriptionContext;
        }

        public async Task<PaginatedResult<ServiceAdSummaryDto>> GetAllServiceBoAds(
       string? sortBy = "CreatedAt",
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
                const int verticalId = 4;

                var query = _dbContext.Services.AsQueryable();
                query = query.Where(ad => ad.IsActive);
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var lowerSearch = search.ToLowerInvariant();
                    query = query.Where(ad =>
                        (!string.IsNullOrEmpty(ad.Title) && ad.Title.ToLower().Contains(lowerSearch)) ||
                        ad.Id.ToString().ToLower().Contains(lowerSearch) ||
                        (!string.IsNullOrEmpty(ad.CreatedBy) && ad.CreatedBy.ToLower().Contains(lowerSearch)) ||
                        (!string.IsNullOrEmpty(ad.UserName) && ad.UserName.ToLower().Contains(lowerSearch))
                    );
                }

                if (fromDate.HasValue)
                    query = query.Where(ad => ad.CreatedAt >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(ad => ad.CreatedAt <= toDate.Value);

                if (publishedFrom.HasValue)
                    query = query.Where(ad => ad.PublishedDate >= publishedFrom.Value);

                if (publishedTo.HasValue)
                    query = query.Where(ad => ad.PublishedDate <= publishedTo.Value);

                if (status.HasValue && Enum.IsDefined(typeof(ServiceStatus), status.Value))
                {
                    var statusEnum = (ServiceStatus)status.Value;
                    query = query.Where(ad => ad.Status == statusEnum);
                }

                if (isFeatured.HasValue)
                    query = query.Where(ad => ad.IsFeatured == isFeatured.Value);

                if (isPromoted.HasValue)
                    query = query.Where(ad => ad.IsPromoted == isPromoted.Value);

                
                sortBy = (sortBy?.ToLowerInvariant() == "asc") ? "asc" : "desc";
                query = sortBy == "asc"
                    ? query.OrderBy(x => x.CreatedAt)
                    : query.OrderByDescending(x => x.CreatedAt);

                int totalCount = await query.CountAsync(cancellationToken);

                
                var pagedAds = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

              
                var adIdStrings = pagedAds.Select(ad => ad.Id).ToList();

                var matchingPayments = await _paymentsContext.Payments
                    .Where(p => (int)p.Vertical == verticalId && adIdStrings.Contains((long)p.AdId))
                    .ToListAsync(cancellationToken);

                
                var resultItems = pagedAds.Select(serviceAd =>
                {
                    var matchingPayment = matchingPayments.FirstOrDefault(p => p.AdId == serviceAd.Id);

                    return new ServiceAdSummaryDto
                    {
                        Id = serviceAd.Id,
                        UserId = serviceAd.CreatedBy,
                        AdTitle = serviceAd.Title,
                        UserName = serviceAd.UserName,
                        Category = serviceAd.CategoryName,
                        SubCategory = serviceAd.L1CategoryName,
                        Certificate = serviceAd.LicenseCertificate,
                        IsPromoted = serviceAd.IsPromoted,
                        IsFeatured = serviceAd.IsFeatured,
                        Status = serviceAd.Status,
                        CreatedAt = serviceAd.CreatedAt,
                        DatePublished = serviceAd.PublishedDate,
                        DateExpiry = serviceAd.ExpiryDate,
                        ImageUpload = serviceAd.PhotoUpload,
                        OrderId = matchingPayment?.PaymentId.ToString() ?? "N/A"
                    };
                }).ToList();

                return new PaginatedResult<ServiceAdSummaryDto>
                {
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    Items = resultItems
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all service ads from database");
                throw new Exception("Error retrieving service ads", ex);
            }
        }



        public async Task<PaginatedResult<ServiceAdPaymentSummaryDto>> GetAllServiceAdPaymentSummaries(
     int? pageNumber = 1,
     int? pageSize = 12,
     string? search = null,
     string? sortBy = "CreatedAt",
     DateTime? filterStartDate = null,
     DateTime? filterEndDate = null,
     string? subscriptionType = null,
     CancellationToken cancellationToken = default)
        {
            try
            {
                const int verticalId = 4;

                var query = _dbContext.Services
                    .Where(ad => ad.Status != ServiceStatus.Rejected && ad.IsActive);

                // Search
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var lowerSearch = search.ToLowerInvariant();
                    query = query.Where(ad =>
                        (!string.IsNullOrEmpty(ad.Title) && ad.Title.ToLower().Contains(lowerSearch)) ||
                        ad.Id.ToString().ToLower().Contains(lowerSearch) ||
                        (!string.IsNullOrEmpty(ad.UserName) && ad.UserName.ToLower().Contains(lowerSearch))
                    );
                }

                // Sorting
                sortBy = sortBy?.ToLowerInvariant();
                query = sortBy == "asc"
                    ? query.OrderBy(ad => ad.CreatedAt)
                    : query.OrderByDescending(ad => ad.CreatedAt);

                // Pagination parameters
                int totalCount = await query.CountAsync(cancellationToken);
                int currentPage = pageNumber ?? 1;
                int currentSize = pageSize ?? 12;

                // Page data
                var pagedServiceData = await query
                    .Skip((currentPage - 1) * currentSize)
                    .Take(currentSize)
                    .Select(ad => new
                    {
                        ad.Id,
                        ad.Title,
                        ad.UserName,
                        ad.EmailAddress,
                        ad.PhoneNumber,
                        ad.WhatsappNumber,
                        ad.Status,
                        ad.CreatedAt,
                        ad.CreatedBy
                    })
                    .ToListAsync(cancellationToken);

                // Extract UserIds
                var userIds = pagedServiceData
                    .Where(s => !string.IsNullOrWhiteSpace(s.CreatedBy))
                    .Select(s => s.CreatedBy)
                    .Distinct()
                    .ToList();

                // Subscriptions
                var subscriptions = new List<(Guid SubscriptionId, string UserId, int? PaymentId, DateTime StartDate, DateTime EndDate)>();
                if (userIds.Any())
                {
                    var subscriptionData = await _subscriptionContext.Subscriptions
                        .Where(s => userIds.Contains(s.UserId))
                        .Select(s => new
                        {
                            s.SubscriptionId,
                            s.UserId,
                            s.PaymentId,
                            s.StartDate,
                            s.EndDate
                        })
                        .ToListAsync(cancellationToken);

                    subscriptions = subscriptionData
                        .Select(s => (s.SubscriptionId, s.UserId, s.PaymentId, s.StartDate, s.EndDate))
                        .ToList();
                }

                // Payments
                var paymentIds = subscriptions
                    .Where(s => s.PaymentId.HasValue)
                    .Select(s => s.PaymentId.Value)
                    .Distinct()
                    .ToList();

                var payments = new List<(int PaymentId, decimal Fee, long? AdId)>();
                if (paymentIds.Any())
                {
                    var paymentData = await _paymentsContext.Payments
                        .Where(p => (int)p.Vertical == verticalId && paymentIds.Contains(p.PaymentId))
                        .Select(p => new
                        {
                            p.PaymentId,
                            p.Fee,
                            p.AdId
                        })
                        .ToListAsync(cancellationToken);

                    payments = paymentData
                        .Select(p => (p.PaymentId, p.Fee, p.AdId))
                        .ToList();
                }

                // Build DTO list
                var result = pagedServiceData.Select(serviceAd =>
                {
                    var matchingSubscription = !string.IsNullOrWhiteSpace(serviceAd.CreatedBy)
                        ? subscriptions.FirstOrDefault(s => s.UserId == serviceAd.CreatedBy)
                        : (SubscriptionId: Guid.Empty, UserId: string.Empty, PaymentId: (int?)null, StartDate: DateTime.MinValue, EndDate: DateTime.MinValue);

                    var matchingPayment = matchingSubscription.PaymentId.HasValue
                        ? payments.FirstOrDefault(p => p.PaymentId == matchingSubscription.PaymentId.Value)
                        : (PaymentId: 0, Fee: 0m, AdId: 0);

                    string subscriptionPlan = "N/A";
                    if (matchingSubscription.UserId != string.Empty)
                    {
                        var start = matchingSubscription.StartDate;
                        var end = matchingSubscription.EndDate;

                        int totalMonths = ((end.Year - start.Year) * 12) + end.Month - start.Month;
                        if (end.Day < start.Day)
                            totalMonths--;

                        if (totalMonths >= 12)
                        {
                            int years = totalMonths / 12;
                            int months = totalMonths % 12;
                            subscriptionPlan = $"{years} year{(years > 1 ? "s" : "")}" +
                                               (months > 0 ? $" and {months} month{(months > 1 ? "s" : "")}" : "");
                        }
                        else if (totalMonths < 1)
                        {
                            int weeks = (int)Math.Ceiling((end - start).TotalDays / 7);
                            subscriptionPlan = $"{weeks} week{(weeks > 1 ? "s" : "")}";
                        }
                        else
                        {
                            subscriptionPlan = $"{totalMonths} month{(totalMonths > 1 ? "s" : "")}";
                        }
                    }

                    return new ServiceAdPaymentSummaryDto
                    {
                        AddId = serviceAd.Id,
                        AddTitle = serviceAd.Title ?? string.Empty,
                        UserName = serviceAd.UserName ?? string.Empty,
                        EmailAddress = serviceAd.EmailAddress ?? string.Empty,
                        Mobile = serviceAd.PhoneNumber ?? string.Empty,
                        WhatsappNumber = serviceAd.WhatsappNumber ?? string.Empty,
                        StartDate = matchingSubscription.UserId != string.Empty ? matchingSubscription.StartDate : DateTime.MinValue,
                        EndDate = matchingSubscription.UserId != string.Empty ? matchingSubscription.EndDate : DateTime.MinValue,
                        Status = serviceAd.Status,
                        OrderId = matchingPayment.PaymentId != 0 ? matchingPayment.PaymentId.ToString() : "N/A",
                        Amount = matchingPayment.PaymentId != 0 ? matchingPayment.Fee : 0,
                        SubscriptionPlan = subscriptionPlan,
                        CreatedAt = serviceAd.CreatedAt
                    };
                }).ToList();

                // Date filter
                if (filterStartDate.HasValue && filterEndDate.HasValue)
                {
                    result = result
                        .Where(r => r.StartDate >= filterStartDate.Value && r.EndDate <= filterEndDate.Value)
                        .ToList();
                }

                // Subscription type filter (dynamic)
                if (!string.IsNullOrWhiteSpace(subscriptionType))
                {
                    var lowerType = subscriptionType.Trim().ToLowerInvariant();
                    result = result
                        .Where(r => r.SubscriptionPlan.ToLowerInvariant() == lowerType)
                        .ToList();
                }

                // Return result
                return new PaginatedResult<ServiceAdPaymentSummaryDto>
                {
                    TotalCount = result.Count,
                    PageNumber = currentPage,
                    PageSize = currentSize,
                    Items = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving service ad payment summaries from database");
                throw;
            }
        }







        public async Task<PaginatedResult<ServiceP2PAdSummaryDto>> GetAllP2PServiceBoAds(
     string? sortBy = "CreatedAt",
     string? search = null,
     DateTime? fromDate = null,
     DateTime? toDate = null,
     int pageNumber = 1,
     int pageSize = 12,
     CancellationToken cancellationToken = default)
        {
            try
            {
                const int verticalId = 4;
                var query = _dbContext.Services
                    .Where(ad => ad.IsActive)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var lowerSearch = search.ToLowerInvariant();
                    query = query.Where(ad =>
                        (!string.IsNullOrEmpty(ad.Title) && ad.Title.ToLower().Contains(lowerSearch)) ||
                        ad.Id.ToString().ToLower().Contains(lowerSearch) ||
                        (!string.IsNullOrEmpty(ad.UserName) && ad.UserName.ToLower().Contains(lowerSearch))
                    );
                }

                if (fromDate.HasValue)
                    query = query.Where(ad => ad.CreatedAt >= fromDate.Value);
                if (toDate.HasValue)
                    query = query.Where(ad => ad.CreatedAt <= toDate.Value);

                sortBy = sortBy?.ToLowerInvariant() ?? "desc";
                query = sortBy == "asc"
                    ? query.OrderBy(x => x.CreatedAt)
                    : query.OrderByDescending(x => x.CreatedAt);

                var totalCount = await query.CountAsync(cancellationToken);

                var pagedAds = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(ad => new
                    {
                        ad.Id,
                        ad.Title,
                        ad.EmailAddress,
                        ad.PhoneNumber,
                        ad.WhatsappNumber,
                        ad.UserName,
                        ad.Status,
                        ad.CreatedAt,
                        ad.PublishedDate,
                        ad.CreatedBy
                    })
                    .ToListAsync(cancellationToken);

                
                var userIds = pagedAds
                    .Where(s => !string.IsNullOrWhiteSpace(s.CreatedBy))
                    .Select(s => s.CreatedBy)
                    .Distinct()
                    .ToList();

               
                var subscriptions = new List<(Guid SubscriptionId, string UserId, int? PaymentId, DateTime StartDate, DateTime EndDate)>();
                if (userIds.Any())
                {
                    var subscriptionData = await _subscriptionContext.Subscriptions
                        .Where(s => userIds.Contains(s.UserId))
                        .Select(s => new
                        {
                            s.SubscriptionId,
                            s.UserId,
                            s.PaymentId,
                            s.StartDate,
                            s.EndDate
                        })
                        .ToListAsync(cancellationToken);

                    subscriptions = subscriptionData
                        .Select(s => (s.SubscriptionId, s.UserId, s.PaymentId, s.StartDate, s.EndDate))
                        .ToList();
                }

             
                var adIdStrings = pagedAds.Select(ad => ad.Id).ToList();
                var matchingPayments = await _paymentsContext.Payments
                    .Where(p => (int)p.Vertical == verticalId && adIdStrings.Contains((long)p.AdId))
                    .ToListAsync(cancellationToken);

                var pagedItems = pagedAds.Select(ad =>
                {
                    var matchingPayment = matchingPayments.FirstOrDefault(p => p.AdId == ad.Id);

                    var matchingSubscription = !string.IsNullOrWhiteSpace(ad.CreatedBy)
                        ? subscriptions.FirstOrDefault(s => s.UserId == ad.CreatedBy)
                        : (SubscriptionId: Guid.Empty, UserId: string.Empty, PaymentId: (int?)null, StartDate: DateTime.MinValue, EndDate: DateTime.MinValue);

                    
                    string productType = "N/A";
                    if (matchingSubscription.UserId != string.Empty)
                    {
                        var start = matchingSubscription.StartDate;
                        var end = matchingSubscription.EndDate;

                        int totalMonths = ((end.Year - start.Year) * 12) + end.Month - start.Month;
                        if (end.Day < start.Day)
                            totalMonths--;

                        if (totalMonths >= 12)
                        {
                            int years = totalMonths / 12;
                            int months = totalMonths % 12;
                            productType = $"{years} year{(years > 1 ? "s" : "")}" +
                                         (months > 0 ? $" and {months} month{(months > 1 ? "s" : "")}" : "");
                        }
                        else if (totalMonths < 1)
                        {
                            int weeks = (int)Math.Ceiling((end - start).TotalDays / 7);
                            productType = $"{weeks} week{(weeks > 1 ? "s" : "")}";
                        }
                        else
                        {
                            productType = $"{totalMonths} month{(totalMonths > 1 ? "s" : "")}";
                        }
                    }

                    return new ServiceP2PAdSummaryDto
                    {
                        Id = ad.Id,
                        AdTitle = ad.Title,
                        ProductType = productType,
                        Email = ad.EmailAddress,
                        Mobile = ad.PhoneNumber,
                        Whatsapp = ad.WhatsappNumber,
                        Amount = matchingPayment?.Fee ?? 0,
                        OrderId = matchingPayment?.PaymentId.ToString() ?? string.Empty,
                        UserName = ad.UserName,
                        Status = ad.Status,
                        CreatedAt = ad.CreatedAt,
                        DatePublished = ad.PublishedDate,
                       
                        StartDate = matchingSubscription.UserId != string.Empty
                            ? matchingSubscription.StartDate.ToString("dd/MM/yyyy")
                            : "N/A",
                        EndDate = matchingSubscription.UserId != string.Empty
                            ? matchingSubscription.EndDate.ToString("dd/MM/yyyy")
                            : "N/A",
                        Views = "02/10/2025" 
                    };
                }).ToList();

                return new PaginatedResult<ServiceP2PAdSummaryDto>
                {
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    Items = pagedItems
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all P2P service ads from database");
                throw new Exception("Error retrieving service ads", ex);
            }
        }



        public async Task<PaginatedResult<ServiceSubscriptionAdSummaryDto>> GetAllSubscriptionAdsServiceBo(
     string? sortBy = "createdAt",
     string? search = null,
     DateTime? fromDate = null,
     DateTime? toDate = null,
     DateTime? publishedFrom = null,
     DateTime? publishedTo = null,
     int pageNumber = 1,
     int pageSize = 12,
     CancellationToken cancellationToken = default)
        {
            try
            {
                const int verticalId = 4; 

                var query = _dbContext.Services
                    .Where(ad => ad.IsActive)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var lowerSearch = search.ToLowerInvariant();
                    query = query.Where(ad =>
                        (!string.IsNullOrEmpty(ad.Title) && ad.Title.ToLower().Contains(lowerSearch)) ||
                        ad.Id.ToString().ToLower().Contains(lowerSearch) ||
                        (!string.IsNullOrEmpty(ad.CreatedBy) && ad.CreatedBy.ToLower().Contains(lowerSearch)) ||
                        (!string.IsNullOrEmpty(ad.UserName) && ad.UserName.ToLower().Contains(lowerSearch))
                    );
                }

                if (fromDate.HasValue)
                    query = query.Where(ad => ad.CreatedAt >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(ad => ad.CreatedAt <= toDate.Value);

                if (publishedFrom.HasValue)
                    query = query.Where(ad => ad.PublishedDate.HasValue && ad.PublishedDate.Value >= publishedFrom.Value);

                if (publishedTo.HasValue)
                    query = query.Where(ad => ad.PublishedDate.HasValue && ad.PublishedDate.Value <= publishedTo.Value);

                sortBy = sortBy?.ToLowerInvariant();
                query = sortBy == "asc"
                    ? query.OrderBy(ad => ad.CreatedAt)
                    : query.OrderByDescending(ad => ad.CreatedAt);

                var totalCount = await query.CountAsync(cancellationToken);

                var pagedAds = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                var adIds = pagedAds.Select(ad => ad.Id).ToList();

               
                var matchingPayments = await _paymentsContext.Payments
                    .Where(p => (int)p.Vertical == verticalId && adIds.Contains((long)p.AdId))
                    .ToListAsync(cancellationToken);

                var result = pagedAds.Select(ad =>
                {
                    var matchingPayment = matchingPayments.FirstOrDefault(p => p.AdId == ad.Id);

                    return new ServiceSubscriptionAdSummaryDto
                    {
                        Id = ad.Id,
                        UserId = ad.CreatedBy,
                        AdTitle = ad.Title,
                        UserName = ad.UserName,
                        Category = ad.CategoryName,
                        SubCategory = ad.L1CategoryName,
                        IsPromoted = ad.IsPromoted,
                        IsFeatured = ad.IsFeatured,
                        Status = ad.Status,
                        CreatedAt = ad.CreatedAt,
                        DatePublished = ad.PublishedDate,
                        DateExpiry = ad.ExpiryDate,
                        ImageUpload = ad.PhotoUpload,
                        OrderId = matchingPayment?.PaymentId.ToString() ?? string.Empty,
                        Favorites = "25/04/2025" 
                    };
                }).ToList();

                return new PaginatedResult<ServiceSubscriptionAdSummaryDto>
                {
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    Items = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscription service ads from database");
                throw new Exception("Error retrieving service ads", ex);
            }
        }
        public async Task<BulkAdActionResponseitems> ModerateBulkService(BulkModerationRequest request, string? userId, CancellationToken ct)
        {
            var ads = await _dbContext.Services
                .Where(s => request.AdIds.Contains(s.Id))
                .ToListAsync(ct);
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
            var subscriptionId = ads.FirstOrDefault()?.SubscriptionId;
            if (subscriptionId == null)
                throw new InvalidOperationException("No subscription associated with these ads.");

            var subscription = await _subscriptionContext.Subscriptions
            .FirstOrDefaultAsync(sub =>
            sub.SubscriptionId == subscriptionId &&
            (int)sub.Status == (int)SubscriptionStatus.Active, ct);

            if (subscription == null)
                throw new InvalidOperationException("Active subscription not found for this service.");
            var effectiveExpiryDate = subscription.EndDate;

            var updatedAds = new List<Common.Infrastructure.Model.Services>();

            foreach (var ad in ads)
            {
                bool shouldUpdate = false;
                string failReason = string.Empty;

                try
                {
                    string actionReason = string.Empty;
                    string actionComment = request.Comments ?? string.Empty;
                    string reason = request.Reason ?? string.Empty;
                    string userid = ad.CreatedBy;
                    string username = ad.UserName;
                    switch (request.Action)
                    {
                        case BulkModerationAction.Approve:
                            if (ad.Status == ServiceStatus.PendingApproval)
                            {
                                ad.Status = ServiceStatus.Published;
                                ad.PublishedDate = DateTime.UtcNow;
                                shouldUpdate = true;
                            }
                            else failReason = $"Cannot approve ad with status '{ad.Status}'.";
                            break;

                        case BulkModerationAction.Publish:
                            if (ad.Status == ServiceStatus.Unpublished)
                            {
                                ad.Status = ServiceStatus.Published;
                                ad.PublishedDate = DateTime.UtcNow;
                                shouldUpdate = true;
                            }
                            else failReason = $"Cannot publish ad with status '{ad.Status}'.";
                            break;

                        case BulkModerationAction.Unpublish:
                            if (ad.Status == ServiceStatus.Published)
                            {
                                ad.Status = ServiceStatus.Unpublished;
                                ad.PublishedDate = null;
                                shouldUpdate = true;
                            }
                            else failReason = $"Cannot unpublish ad with status '{ad.Status}'.";
                            break;

                        case BulkModerationAction.UnPromote:
                            if (ad.IsPromoted)
                            {
                                ad.IsPromoted = false;
                                ad.PromotedExpiryDate = null;
                                shouldUpdate = true;
                            }
                            else failReason = "Cannot unpromote an ad that is not promoted.";
                            break;

                        case BulkModerationAction.UnFeature:
                            if (ad.IsFeatured)
                            {
                                ad.IsFeatured = false;
                                ad.FeaturedExpiryDate = null;
                                shouldUpdate = true;
                            }
                            else failReason = "Cannot unfeature an ad that is not featured.";
                            break;

                        case BulkModerationAction.Promote:
                            if (!ad.IsPromoted)
                            {
                                ad.IsPromoted = true;
                                ad.PromotedExpiryDate = effectiveExpiryDate;
                                shouldUpdate = true;
                            }
                            else failReason = "Cannot promote an ad that is already promoted.";
                            break;

                        case BulkModerationAction.Feature:
                            if (!ad.IsFeatured)
                            {
                                ad.IsFeatured = true;
                                ad.FeaturedExpiryDate = effectiveExpiryDate;
                                shouldUpdate = true;
                            }
                            else failReason = "Cannot feature an ad that is already featured.";
                            break;

                        case BulkModerationAction.IsRefreshed:
                            ad.IsRefreshed = true;
                            ad.LastRefreshedOn = DateTime.UtcNow;
                            ad.CreatedAt = DateTime.UtcNow;
                            ad.CreatedBy = userId;
                            shouldUpdate = true;
                            break;

                        case BulkModerationAction.Remove:
                            ad.Status = ServiceStatus.Rejected;
                            ad.IsActive = false;
                            actionReason = "Ad Removed (Rejected)";
                            reason = "Ad rejected by admin.";
                            shouldUpdate = true;
                            break;

                        case BulkModerationAction.NeedChanges:
                            if (ad.Status == ServiceStatus.PendingApproval)
                            {
                                ad.Status = ServiceStatus.NeedsModification;
                                shouldUpdate = true;
                                ad.UpdatedAt = DateTime.UtcNow;
                                actionReason = "Ad Needs Changes";
                            }
                            else
                            {
                                failReason = $"Cannot mark ad as NeedsModification with status '{ad.Status}'.";
                            }
                            break;

                        case BulkModerationAction.Hold:
                            if (ad.Status == ServiceStatus.Draft)
                                failReason = "Cannot hold an ad that is in draft status.";
                            else if (ad.Status != ServiceStatus.Hold)
                            {
                                ad.Status = ServiceStatus.Hold;
                                shouldUpdate = true;
                                ad.UpdatedAt = DateTime.UtcNow;
                                actionReason = "Ad On Hold";
                                reason = "Ad placed on hold by admin.";
                            }
                            else
                            {
                                failReason = "Ad is already on hold.";
                            }
                            break;

                        case BulkModerationAction.Onhold:
                            ad.Status = ServiceStatus.Onhold;
                            shouldUpdate = true;
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

                        if (request.Action == BulkModerationAction.Hold || request.Action == BulkModerationAction.Remove || request.Action == BulkModerationAction.NeedChanges)
                        {
                            var actionCommentEntity = new Comment
                            {
                                AdId = ad.Id,
                                Action = actionReason,
                                Reason = reason ?? string.Empty,
                                Comments = actionComment,
                                Vertical = Vertical.Services,
                                SubVertical = 0,
                                CreatedAt = DateTime.UtcNow,
                                CreatedUserId = userid,
                                CreatedUserName = username,
                                UpdatedUserId = userId,
                                UpdatedUserName = username,
                            };

                            await _dbContext.Comments.AddAsync(actionCommentEntity, ct);
                            _dbContext.Services.Update(ad);

                            await _dapr.PublishEventAsync("pubsub", "notifications-email", new NotificationEntity
                            {
                                Destinations = new List<string> { "email" },
                                Recipients = new List<RecipientDto>
                                {
                                    new RecipientDto
                                    {
                                        Name = username,
                                        Email = ad.EmailAddress
                                    }
                                },
                                Subject = $"Service '{ad.Title} was updated",
                                Plaintext = $"Hello,\n\nYour ad titled '{ad.Title}' has been updated.\n\nStatus: {ad.Status}\n\nThanks,\nQL Team",
                                Html = $@"
                                <p>Hi,</p>
                                <p>Your ad titled '<b>{ad.Title}</b>' has been updated.</p>
                                <p>Status: <b>{ad.Status}</b></p>
                                <p>Thanks,<br/>QL Team</p>"
                            }, ct);

                            succeeded.Count++;
                            succeeded.Ids.Add(ad.Id);
                        }
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
                _dbContext.Services.UpdateRange(updatedAds);
                await _dbContext.SaveChangesAsync(ct);

                foreach (var ad in updatedAds)
                {
                    var upsertRequest = await IndexServiceToAzureSearch(ad, ct);
                    if (upsertRequest != null)
                    {
                        var message = new IndexMessage
                        {
                            Action = "Upsert",
                            Vertical = ConstantValues.IndexNames.ServicesIndex,
                            UpsertRequest = upsertRequest
                        };

                        await _dapr.PublishEventAsync(
                            ConstantValues.PubSubName,
                            ConstantValues.PubSubTopics.IndexUpdates,
                            message,
                            ct
                        );
                    }
                }
            }

            return new BulkAdActionResponseitems
            {
                Succeeded = succeeded,
                Failed = failed
            };
        }
        private async Task<CommonIndexRequest> IndexServiceToAzureSearch(Common.Infrastructure.Model.Services dto, CancellationToken cancellationToken)
        {

            var indexDoc = new ServicesIndex
            {
                Id = dto.Id.ToString(),
                CategoryId = dto.CategoryId.ToString(),
                L1CategoryId = dto.L1CategoryId.ToString(),
                L2CategoryId = dto.L2CategoryId.ToString(),
                CategoryName = dto.CategoryName,
                L1CategoryName = dto.L1CategoryName,
                L2CategoryName = dto.L2CategoryName,
                Price = (double)dto.Price,
                IsPriceOnRequest = dto.IsPriceOnRequest,
                Title = dto.Title,
                Description = dto.Description,
                PhoneNumberCountryCode = dto.PhoneNumberCountryCode,
                PhoneNumber = dto.PhoneNumber,
                WhatsappNumberCountryCode = dto.WhatsappNumberCountryCode,
                WhatsappNumber = dto.WhatsappNumber,
                EmailAddress = dto.EmailAddress,
                Location = dto.Location,
                LocationId = dto.LocationId,
                StreetNumber = dto.StreetNumber,
                BuildingNumber = dto.BuildingNumber,
                LicenseCertificate = dto.LicenseCertificate,
                ZoneId = dto.ZoneId,
                SubscriptionId = dto.SubscriptionId.ToString(),
                Comments = dto.Comments,
                Longitude = (double)dto.Longitude,
                Lattitude = (double)dto.Lattitude,
                AdType = dto.AdType.ToString(),
                IsFeatured = dto.IsFeatured,
                IsPromoted = dto.IsPromoted,
                Status = dto.Status.ToString(),
                FeaturedExpiryDate = dto.FeaturedExpiryDate,
                PromotedExpiryDate = dto.PromotedExpiryDate,
                LastRefreshedOn = dto.LastRefreshedOn,
                IsRefreshed = dto.IsRefreshed,
                PublishedDate = dto.PublishedDate,
                ExpiryDate = dto.ExpiryDate,
                Availability = dto.Availability,
                Duration = dto.Duration,
                Reservation = dto.Reservation,
                UserName = dto.UserName,
                IsActive = dto.IsActive,
                CreatedBy = dto.CreatedBy,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt,
                UpdatedBy = dto.UpdatedBy,
                Slug = dto.Slug,
                Images = dto.PhotoUpload.Select(i => new ImageInfo
                {
                    Url = i.Url,
                    Order = i.Order
                }).ToList()
            };
            var indexRequest = new CommonIndexRequest
            {
                IndexName = ConstantValues.IndexNames.ServicesIndex,
                ServicesItem = indexDoc
            };
            return indexRequest;

        }
    }
}