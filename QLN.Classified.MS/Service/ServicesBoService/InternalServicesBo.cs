using Dapr.Client;
using Microsoft.EntityFrameworkCore;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.IServiceBoService;
using QLN.Common.Infrastructure.QLDbContext;
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

              
                var adIdStrings = pagedAds.Select(ad => ad.Id.ToString()).ToList();

                var matchingPayments = await _paymentsContext.Payments
                    .Where(p => (int)p.Vertical == verticalId && adIdStrings.Contains(p.AdId))
                    .ToListAsync(cancellationToken);

                
                var resultItems = pagedAds.Select(serviceAd =>
                {
                    var matchingPayment = matchingPayments.FirstOrDefault(p => p.AdId == serviceAd.Id.ToString());

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

                var payments = new List<(int PaymentId, decimal Fee, string AdId)>();
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
                        : (PaymentId: 0, Fee: 0m, AdId: string.Empty);

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

             
                var adIdStrings = pagedAds.Select(ad => ad.Id.ToString()).ToList();
                var matchingPayments = await _paymentsContext.Payments
                    .Where(p => (int)p.Vertical == verticalId && adIdStrings.Contains(p.AdId))
                    .ToListAsync(cancellationToken);

                var pagedItems = pagedAds.Select(ad =>
                {
                    var matchingPayment = matchingPayments.FirstOrDefault(p => p.AdId == ad.Id.ToString());

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

                var adIds = pagedAds.Select(ad => ad.Id.ToString()).ToList();

               
                var matchingPayments = await _paymentsContext.Payments
                    .Where(p => (int)p.Vertical == verticalId && adIds.Contains(p.AdId))
                    .ToListAsync(cancellationToken);

                var result = pagedAds.Select(ad =>
                {
                    var matchingPayment = matchingPayments.FirstOrDefault(p => p.AdId == ad.Id.ToString());

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



        public async Task<List<CompanyProfileDto>> GetCompaniesByVerticalAsync(VerticalType verticalId, SubVertical? subVerticalId, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.GetBulkStateAsync<CompanyProfileDto>(
                    storeName: ConstantValues.Company.CompanyStoreName,
                    keys: await GetAllCompanyIdsAsync(cancellationToken), 
                    parallelism: 10,
                    metadata: null,
                    cancellationToken: cancellationToken);

                var filtered = result
                    .Where(entry =>  entry.Value != null)
                    .Select(entry => entry.Value!)
                    .Where(company => company.Vertical == verticalId &&
                        (subVerticalId == null || company.SubVertical == subVerticalId) &&
                        company.IsActive)
                    .ToList();

                return filtered;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving companies for vertical '{VerticalId}' and subvertical '{SubVerticalId}'", verticalId, subVerticalId);
                throw;
            }
        }

        private async Task<IReadOnlyList<string>> GetAllCompanyIdsAsync(CancellationToken cancellationToken)
        {
            var indexKey = ConstantValues.Company.CompanyIndexKey; 
            var index = await _dapr.GetStateAsync<List<string>>(ConstantValues.Company.CompanyStoreName, indexKey, cancellationToken: cancellationToken);
            return index ?? new List<string>();
        }


    }
}