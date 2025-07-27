using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IServiceBoService;
using static QLN.Subscriptions.Actor.ActorClass.PayToPublishPaymentActor;

namespace QLN.Classified.MS.Service.ServicesBoService
{
    public class InternalServicesBo : IServicesBoService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<InternalServicesBo> _logger;
        private const string StoreName = "statestore";
        private const string GlobalPaymentDetailsKey = "paytopublish-payment-details-collection";

        public InternalServicesBo(
            DaprClient dapr,
            ILogger<InternalServicesBo> logger)
        {
            _dapr = dapr ?? throw new ArgumentNullException(nameof(dapr));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PaginatedResult<ServiceAdSummaryDto>> GetAllServiceBoAds(
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
                    ConstantValues.Services.StoreName,
                    ConstantValues.Services.ServicesIndexKey,
                    cancellationToken: cancellationToken
                ) ?? new();

                var paymentDetails = await _dapr.GetStateAsync<GlobalP2PPaymentDetailsCollection>(
                    StoreName,
                    GlobalPaymentDetailsKey,
                    cancellationToken: cancellationToken
                ) ?? new();

                var serviceAds = new List<ServiceAdSummaryDto>();

                foreach (var key in keys)
                {
                    var serviceAd = await _dapr.GetStateAsync<ServicesDto>(
                        ConstantValues.Services.StoreName,
                        key,
                        cancellationToken: cancellationToken);

                    if (serviceAd == null) continue;

                    var matchingPayment = paymentDetails.Details.FirstOrDefault(x =>
                        x.UserId == serviceAd.CreatedBy && x.AddId == serviceAd.Id);

                    var summary = new ServiceAdSummaryDto
                    {
                        Id = serviceAd.Id,
                        UserId = serviceAd.CreatedBy,
                        AdTitle = serviceAd.Title,
                        UserName = serviceAd.UserName,
                        Category = serviceAd.CategoryName,
                        SubCategory = serviceAd.L1CategoryName,
                        IsPromoted = serviceAd.IsPromoted,
                        IsFeatured = serviceAd.IsFeatured,
                        Status = serviceAd.Status,
                        CreationDate = serviceAd.CreatedAt,
                        DatePublished = serviceAd.PublishedDate,
                        DateExpiry = serviceAd.ExpiryDate,
                        ImageUpload = serviceAd.PhotoUpload,
                        PaymentTransactionId = matchingPayment?.PaymentTransactionId
                    };

                    serviceAds.Add(summary);
                }

                // Apply Filters
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var lowerSearch = search.ToLowerInvariant();
                    serviceAds = serviceAds.Where(ad =>
                        (!string.IsNullOrEmpty(ad.AdTitle) && ad.AdTitle.ToLowerInvariant().Contains(lowerSearch)) ||
                        (ad.Id.ToString().ToLowerInvariant().Contains(lowerSearch)) ||
                        (!string.IsNullOrEmpty(ad.UserId) && ad.UserId.ToLowerInvariant().Contains(lowerSearch)) ||
                        (!string.IsNullOrEmpty(ad.UserName) && ad.UserName.ToLowerInvariant().Contains(lowerSearch))
                    ).ToList();
                }

                if (fromDate.HasValue)
                    serviceAds = serviceAds.Where(ad => ad.CreationDate >= fromDate.Value).ToList();

                if (toDate.HasValue)
                    serviceAds = serviceAds.Where(ad => ad.CreationDate <= toDate.Value).ToList();

                if (publishedFrom.HasValue)
                    serviceAds = serviceAds.Where(ad => ad.DatePublished.HasValue && ad.DatePublished.Value >= publishedFrom.Value).ToList();

                if (publishedTo.HasValue)
                    serviceAds = serviceAds.Where(ad => ad.DatePublished.HasValue && ad.DatePublished.Value <= publishedTo.Value).ToList();

                if (status.HasValue && Enum.IsDefined(typeof(ServiceStatus), status.Value))
                {
                    var statusEnum = (ServiceStatus)status.Value;
                    serviceAds = serviceAds.Where(ad => ad.Status == statusEnum).ToList();
                }

                if (isFeatured.HasValue)
                    serviceAds = serviceAds.Where(ad => ad.IsFeatured == isFeatured.Value).ToList();

                if (isPromoted.HasValue)
                    serviceAds = serviceAds.Where(ad => ad.IsPromoted == isPromoted.Value).ToList();

                // Sort
                sortBy = sortBy?.ToLowerInvariant();
                serviceAds = sortBy switch
                {
                    "title" => serviceAds.OrderByDescending(x => x.AdTitle).ToList(),
                    "username" => serviceAds.OrderByDescending(x => x.UserName).ToList(),
                    "status" => serviceAds.OrderByDescending(x => x.Status).ToList(),
                    "published" => serviceAds.OrderByDescending(x => x.DatePublished).ToList(),
                    _ => serviceAds.OrderByDescending(x => x.CreationDate).ToList(),
                };

                // Pagination
                int totalCount = serviceAds.Count;
                var pagedItems = serviceAds
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return new PaginatedResult<ServiceAdSummaryDto>
                {
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    Items = pagedItems
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all service ads");
                throw new Exception("Error retrieving service ads", ex);
            }
        }




    }
}