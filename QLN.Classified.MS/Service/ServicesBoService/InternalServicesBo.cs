using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.IServiceBoService;


namespace QLN.Classified.MS.Service.ServicesBoService
{
    public class InternalServicesBo : IServicesBoService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<InternalServicesBo> _logger;
        

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

                //var paymentDetails = await _dapr.GetStateAsync<GlobalP2PPaymentDetailsCollection>(
                //    StoreName,
                //    GlobalPaymentDetailsKey,
                //    cancellationToken: cancellationToken
                //) ?? new();

                var serviceAds = new List<ServiceAdSummaryDto>();

                foreach (var key in keys)
                {
                    var serviceAd = await _dapr.GetStateAsync<ServicesModel>(
                        ConstantValues.Services.StoreName,
                        key,
                        cancellationToken: cancellationToken);

                    if (serviceAd == null) continue;

                    //var matchingPayment = paymentDetails.Details.FirstOrDefault(x =>
                    //    x.UserId == serviceAd.CreatedBy && x.AddId == serviceAd.Id);

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
                        OrderId = "103"//matchingPayment?.PaymentTransactionId  
                    };

                    serviceAds.Add(summary);
                }

              
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

        public async Task<PaginatedResult<ServiceAdPaymentSummaryDto>> GetAllServiceAdPaymentSummaries(
     int? pageNumber = 1,
     int? pageSize = 12,
     string? search = null,
     string? sortBy = null, 
     CancellationToken cancellationToken = default)
        {
            var result = new List<ServiceAdPaymentSummaryDto>();

            var keys = await _dapr.GetStateAsync<List<string>>(
                ConstantValues.Services.StoreName,
                ConstantValues.Services.ServicesIndexKey,
                cancellationToken: cancellationToken) ?? new();

            //var paymentDetails = await _dapr.GetStateAsync<UserPaymentDetailsCollection>(
            //    StoreName,
            //    GlobalPaymentDetailsKey,
            //    cancellationToken: cancellationToken) ?? new();

            var matchedAds = new List<ServiceAdPaymentSummaryDto>();

            foreach (var key in keys)
            {
                var serviceAd = await _dapr.GetStateAsync<ServicesModel>(
                    ConstantValues.Services.StoreName,
                    key,
                    cancellationToken: cancellationToken);

                if (serviceAd == null) continue;

                //var matchingPayment = paymentDetails.Details.FirstOrDefault(p =>
                //    p.UserId == serviceAd.CreatedBy && p.PaymentTransactionId == serviceAd.Id);

                var defaultDate = new DateTime(2025, 4, 12);

                var dto = new ServiceAdPaymentSummaryDto
                {
                    AddId = serviceAd.Id,
                    AddTitle = serviceAd.Title,
                    UserName = serviceAd.UserName,
                    EmailAddress = serviceAd.EmailAddress,
                    Mobile = serviceAd.PhoneNumber,
                    WhatsappNumber = serviceAd.WhatsappNumber,
                    StartDate = "25/04/2025",
                    EndDate = "25/04/2025",
                    Status = serviceAd.Status,
                    OrderId = "102",//matchingPayment?.PaymentTransactionId"
                    Amount =  100,// matchingPayment?.Price
                    SubscriptionPlan = "2 Months"
                    //(matchingPayment != null &&
                    //            matchingPayment.StartDate != DateTime.MinValue &&
                    //            matchingPayment.EndDate != DateTime.MinValue)
                    //    ? $"{(matchingPayment.EndDate - matchingPayment.StartDate).TotalDays} days"
                    //    : "0 days"
                };

                if (string.IsNullOrWhiteSpace(search) ||
                    dto.AddTitle?.Contains(search, StringComparison.OrdinalIgnoreCase) == true ||
                    dto.AddId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase) == true ||
                    dto.UserName?.Contains(search, StringComparison.OrdinalIgnoreCase) == true)
                {
                    matchedAds.Add(dto);
                }
            }

           
            matchedAds = sortBy?.ToLower() switch
            {
                "startdate" => matchedAds.OrderBy(x => x.StartDate).ToList(),
                "enddate" => matchedAds.OrderBy(x => x.EndDate).ToList(),
                _ => matchedAds.OrderByDescending(x => x.StartDate).ToList() 
            };

           
            var totalCount = matchedAds.Count;
            int currentPage = pageNumber ?? 1;
            int currentSize = pageSize ?? 12;

            var paginatedItems = matchedAds
                .Skip((currentPage - 1) * currentSize)
                .Take(currentSize)
                .ToList();

            return new PaginatedResult<ServiceAdPaymentSummaryDto>
            {
                TotalCount = totalCount,
                PageNumber = currentPage,
                PageSize = currentSize,
                Items = paginatedItems
            };
        }
        public async Task<PaginatedResult<ServiceP2PAdSummaryDto>> GetAllP2PServiceBoAds(
       string? sortBy = "CreationDate",
       string? search = null,
       DateTime? fromDate = null,
       DateTime? toDate = null,
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

                //var paymentDetails = await _dapr.GetStateAsync<GlobalP2PPaymentDetailsCollection>(
                //    StoreName,
                //    GlobalPaymentDetailsKey,
                //    cancellationToken: cancellationToken
                //) ?? new();

                var serviceAds = new List<ServiceP2PAdSummaryDto>();

                foreach (var key in keys)
                {
                    var serviceAd = await _dapr.GetStateAsync<ServicesModel>(
                        ConstantValues.Services.StoreName,
                        key,
                        cancellationToken: cancellationToken);

                    if (serviceAd == null) continue;

                    //var matchingPayment = paymentDetails.Details.FirstOrDefault(x =>
                    //    x.UserId == serviceAd.CreatedBy && x.AddId == serviceAd.Id);

                    var summary = new ServiceP2PAdSummaryDto
                    {
                        Id = serviceAd.Id,
                        AdTitle = serviceAd.Title,
                        ProductType= "2 Months",
                        Email=serviceAd.EmailAddress,
                        Mobile=serviceAd.PhoneNumber,
                        Whatsapp=serviceAd.WhatsappNumber,
                        Amount = "200",
                        UserName = serviceAd.UserName,
                        Status = serviceAd.Status,
                        CreationDate = serviceAd.CreatedAt,
                        DatePublished = serviceAd.PublishedDate,
                        StartDate= "25/04/2025",
                        EndDate = "25/04/2025",
                        Views = "02/10/2025",
                        OrderId = "103"//matchingPayment?.PaymentTransactionId  
                    };

                    serviceAds.Add(summary);
                }


                if (!string.IsNullOrWhiteSpace(search))
                {
                    var lowerSearch = search.ToLowerInvariant();
                    serviceAds = serviceAds.Where(ad =>
                        (!string.IsNullOrEmpty(ad.AdTitle) && ad.AdTitle.ToLowerInvariant().Contains(lowerSearch)) ||
                        (ad.Id.ToString().ToLowerInvariant().Contains(lowerSearch)) ||
                       
                        (!string.IsNullOrEmpty(ad.UserName) && ad.UserName.ToLowerInvariant().Contains(lowerSearch))
                    ).ToList();
                }

                if (fromDate.HasValue)
                    serviceAds = serviceAds.Where(ad => ad.CreationDate >= fromDate.Value).ToList();

                if (toDate.HasValue)
                    serviceAds = serviceAds.Where(ad => ad.CreationDate <= toDate.Value).ToList();

               

                // Sort
                sortBy = sortBy?.ToLowerInvariant();
                serviceAds = sortBy switch
                {
                    "title" => serviceAds.OrderByDescending(x => x.AdTitle).ToList(),
                    "username" => serviceAds.OrderByDescending(x => x.UserName).ToList(),
                    "status" => serviceAds.OrderByDescending(x => x.Status).ToList(),
                    "published" => serviceAds.OrderByDescending(x => x.CreationDate).ToList(),
                    _ => serviceAds.OrderByDescending(x => x.CreationDate).ToList(),
                };

                // Pagination
                int totalCount = serviceAds.Count;
                var pagedItems = serviceAds
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

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
                _logger.LogError(ex, "Error retrieving all service ads");
                throw new Exception("Error retrieving service ads", ex);
            }
        }

        public async Task<PaginatedResult<ServiceSubscriptionAdSummaryDto>> GetAllSubscriptionAdsServiceBo(
                string? sortBy = "CreationDate",
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
                var keys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.Services.StoreName,
                    ConstantValues.Services.ServicesIndexKey,
                    cancellationToken: cancellationToken
                ) ?? new();

                //var paymentDetails = await _dapr.GetStateAsync<GlobalP2PPaymentDetailsCollection>(
                //    StoreName,
                //    GlobalPaymentDetailsKey,
                //    cancellationToken: cancellationToken
                //) ?? new();

                var serviceAds = new List<ServiceSubscriptionAdSummaryDto>();

                foreach (var key in keys)
                {
                    var serviceAd = await _dapr.GetStateAsync<ServicesModel>(
                        ConstantValues.Services.StoreName,
                        key,
                        cancellationToken: cancellationToken);

                    if (serviceAd == null) continue;

                    //var matchingPayment = paymentDetails.Details.FirstOrDefault(x =>
                    //    x.UserId == serviceAd.CreatedBy && x.AddId == serviceAd.Id);

                    var summary = new ServiceSubscriptionAdSummaryDto
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
                        OrderId = "103",//matchingPayment?.PaymentTransactionId ,
                        Favorites="25/04/2025"

                    };

                    serviceAds.Add(summary);
                }


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

                return new PaginatedResult<ServiceSubscriptionAdSummaryDto>
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
        public async Task<List<CompanyProfileDto>> GetCompaniesByVerticalAsync(VerticalType verticalId, SubVertical? subVerticalId, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.GetBulkStateAsync<CompanyProfileDto>(
                    storeName: ConstantValues.CompanyStoreName,
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
            var indexKey = ConstantValues.CompanyIndexKey; 
            var index = await _dapr.GetStateAsync<List<string>>(ConstantValues.CompanyStoreName, indexKey, cancellationToken: cancellationToken);
            return index ?? new List<string>();
        }


    }
}