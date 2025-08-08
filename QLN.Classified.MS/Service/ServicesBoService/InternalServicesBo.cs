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

        public InternalServicesBo(
            DaprClient dapr,
            ILogger<InternalServicesBo> logger, QLClassifiedContext dbContext, QLPaymentsContext paymentsContext)
        {
            _dapr = dapr ?? throw new ArgumentNullException(nameof(dapr));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dbContext = dbContext;
            _paymentsContext = paymentsContext;
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

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var lowerSearch = search.ToLowerInvariant();
                    query = query.Where(ad =>
                        (!string.IsNullOrEmpty(ad.Title) && ad.Title.ToLower().Contains(lowerSearch)) ||
                        ad.Id.ToString().ToLower().Contains(lowerSearch) ||
                        (!string.IsNullOrEmpty(ad.UserName) && ad.UserName.ToLower().Contains(lowerSearch))
                    );
                }

                if (!string.IsNullOrWhiteSpace(subscriptionType))
                {
                    query = query.Where(ad => "2 Months".Equals(subscriptionType, StringComparison.OrdinalIgnoreCase));
                }

                DateTime staticStart = DateTime.ParseExact("25/04/2025", "dd/MM/yyyy", null);
                DateTime staticEnd = DateTime.ParseExact("25/07/2025", "dd/MM/yyyy", null);

                if (filterStartDate.HasValue && staticEnd < filterStartDate.Value)
                    return new PaginatedResult<ServiceAdPaymentSummaryDto>();

                if (filterEndDate.HasValue && staticStart > filterEndDate.Value)
                    return new PaginatedResult<ServiceAdPaymentSummaryDto>();

                sortBy = sortBy?.ToLowerInvariant();
                query = sortBy == "asc"
                    ? query.OrderBy(ad => ad.CreatedAt)
                    : query.OrderByDescending(ad => ad.CreatedAt);

                int totalCount = await query.CountAsync(cancellationToken);
                int currentPage = pageNumber ?? 1;
                int currentSize = pageSize ?? 12;

                var pagedServiceIds = await query
                    .Skip((currentPage - 1) * currentSize)
                    .Take(currentSize)
                    .Select(ad => ad.Id)
                    .ToListAsync(cancellationToken);

                var services = await _dbContext.Services
                    .Where(s => pagedServiceIds.Contains(s.Id))
                    .ToListAsync(cancellationToken);

                var stringServiceIds = pagedServiceIds.Select(id => id.ToString()).ToList();

                var payments = await _paymentsContext.Payments
                    .Where(p => (int)p.Vertical == verticalId && stringServiceIds.Contains(p.AdId))
                    .ToListAsync(cancellationToken);

                var result = services.Select(serviceAd =>
                {
                    var matchingPayment = payments.FirstOrDefault(p => p.AdId == serviceAd.Id.ToString());

                    return new ServiceAdPaymentSummaryDto
                    {
                        AddId = serviceAd.Id,
                        AddTitle = serviceAd.Title,
                        UserName = serviceAd.UserName,
                        EmailAddress = serviceAd.EmailAddress,
                        Mobile = serviceAd.PhoneNumber,
                        WhatsappNumber = serviceAd.WhatsappNumber,
                        StartDate = "25/04/2025",     // static value, adjust as needed
                        EndDate = "25/07/2025",       // static value, adjust as needed
                        Status = serviceAd.Status,
                        OrderId = matchingPayment?.PaymentId.ToString() ?? "N/A",
                        Amount = matchingPayment?.Fee ?? 0,
                        SubscriptionPlan = "2 Months",
                        CreatedAt = serviceAd.CreatedAt
                    };
                }).ToList();

                return new PaginatedResult<ServiceAdPaymentSummaryDto>
                {
                    TotalCount = totalCount,
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
                    .ToListAsync(cancellationToken);

                var adIdStrings = pagedAds.Select(ad => ad.Id.ToString()).ToList();

                var matchingPayments = await _paymentsContext.Payments
                    .Where(p => (int)p.Vertical == verticalId && adIdStrings.Contains(p.AdId))
                    .ToListAsync(cancellationToken);

                var pagedItems = pagedAds.Select(ad =>
                {
                    var matchingPayment = matchingPayments.FirstOrDefault(p => p.AdId == ad.Id.ToString());

                    return new ServiceP2PAdSummaryDto
                    {
                        Id = ad.Id,
                        AdTitle = ad.Title,
                        ProductType = "2 Months",
                        Email = ad.EmailAddress,
                        Mobile = ad.PhoneNumber,
                        Whatsapp = ad.WhatsappNumber,
                        Amount = matchingPayment?.Fee ?? 0,
                        OrderId = matchingPayment?.PaymentId.ToString() ?? string.Empty,
                        UserName = ad.UserName,
                        Status = ad.Status,
                        CreatedAt = ad.CreatedAt,
                        DatePublished = ad.PublishedDate,
                        StartDate = "25/04/2025",
                        EndDate = "25/04/2025",
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
                const int verticalId = 4; // 👈 Treat vertical as constant for service

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

                // 👇 Use constant vertical ID to filter payments
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