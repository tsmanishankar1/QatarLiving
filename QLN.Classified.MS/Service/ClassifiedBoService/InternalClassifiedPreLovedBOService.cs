using Dapr.Client;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using QLN.Classified.MS.Utilities;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.ClassifiedsBo;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IClassifiedBoService;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.QLDbContext;
using QLN.Common.Infrastructure.Subscriptions;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Xml.Serialization;
namespace QLN.Classified.MS.Service.ClassifiedBoService
{
    public class InternalClassifiedPreLovedBOService : IClassifiedPreLovedBOService
    {
        private readonly Dapr.Client.DaprClient _dapr;
        private readonly ILogger<IClassifiedPreLovedBOService> _logger;
        private readonly QLClassifiedContext _context;
        private readonly QLCompanyContext _companyContext;
        private readonly QLPaymentsContext _paymentContext;
        private readonly QLSubscriptionContext _subscriptionContext;
        public InternalClassifiedPreLovedBOService(DaprClient dapr, ILogger<IClassifiedPreLovedBOService> logger, QLClassifiedContext context, QLCompanyContext companyContext, QLPaymentsContext paymentContext, QLSubscriptionContext subscriptionContext)
        {

            _dapr = dapr;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context;
            _companyContext = companyContext;
            _paymentContext = paymentContext;
            _subscriptionContext=subscriptionContext;
        }

        #region PreLoved back office end points
        public async Task<ClassifiedBOPageResponse<PrelovedViewSubscriptionsDto>> ViewPreLovedSubscriptions(string? subscriptionType, string? filterDate, int? Page, int? PageSize, string? Search, string? SortBy, string? SortOrder, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Preloved internal services reached");
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
                string searchLower = Search?.ToLower();

              
                var subscriptionList = await _subscriptionContext.Subscriptions
    .AsNoTracking()
    .ToListAsync(cancellationToken);

                var subscriptionIds = subscriptionList.Select(s => s.SubscriptionId).ToList();
               
                var prelovedList = await _context.Preloved
    .Where(p => subscriptionIds.Contains((Guid)p.SubscriptionId))
    .ToListAsync(cancellationToken);
                _logger.LogInformation("prelovedList:"+ prelovedList.Count().ToString());
                var companyIds = subscriptionList.Select(s => s.CompanyId).ToList();
                var companiesList = await _companyContext.Companies
    .Where(c => companyIds.Contains(c.Id))
    .ToListAsync(cancellationToken);
                _logger.LogInformation("companiesList:" + companiesList.Count().ToString());
                var paymentIds = subscriptionList.Select(s => s.PaymentId).ToList();
                var paymentsList = await _paymentContext.Payments
    .Where(p => paymentIds.Contains(p.PaymentId))
    .ToListAsync(cancellationToken);

                _logger.LogInformation("paymentsList:" + paymentsList.Count().ToString());

                var filtered = (from preloved in prelovedList
                                join subscription in subscriptionList on preloved.SubscriptionId equals subscription.SubscriptionId
                                join company in companiesList on subscription.CompanyId equals company.Id
                                join payment in paymentsList on subscription.PaymentId equals payment.PaymentId
                                select new PrelovedViewSubscriptionsDto
                                {
                                    AdId = preloved.Id,
                                    OrderId = payment.PaymentId,
                                    SubscriptionType = SubscriptionDictionary.GetDescription(subscription.ProductCode),
                                    UserName = company.CompanyName,
                                    Email = company.Email,
                                    Mobile = company.PhoneNumber,
                                    Whatsapp = company.WhatsAppNumber,
                                    WebUrl = company.WebsiteUrl,
                                    Amount = payment.Fee,
                                    Status = Enum.GetName(typeof(Status), subscription.Status).ToString(),
                                    StartDate = subscription.StartDate,
                                    EndDate = subscription.EndDate
                                })
                .Where(x =>
                    (string.IsNullOrEmpty(subscriptionType) || x.SubscriptionType == subscriptionType) &&
                    x.StartDate >= dateThreshold &&
                    x.StartDate <= filterDateParsed &&
                    (
                        string.IsNullOrEmpty(searchLower) ||
                        x.AdId.ToString().Contains(searchLower) ||
                        x.OrderId.ToString().Contains(searchLower) ||
                        (x.UserName != null && x.UserName.ToLower().Contains(searchLower)) ||
                        (x.Email != null && x.Email.ToLower().Contains(searchLower)) ||
                        (x.Mobile != null && x.Mobile.ToLower().Contains(searchLower)) ||
                        (x.Status != null && x.Status.ToLower().Contains(searchLower))
                    )
                )
                .ToList();

                filtered = SortBy?.ToLower() switch
                {
                    "startdate" => SortOrder?.ToLower() == "desc"
                    ? filtered.OrderByDescending(t => t.StartDate).ToList()
                    : filtered.OrderBy(t => t.StartDate).ToList(),

                    "status" => SortOrder?.ToLower() == "desc"
                        ? filtered.OrderByDescending(t => t.Status).ToList()
                        : filtered.OrderBy(t => t.Status).ToList(),

                    _ => filtered.OrderBy(t => t.StartDate).ToList()
                };


                _logger.LogInformation("Preloved DB data retrieved");

                int currentPage = Math.Max(1, Page ?? 1);
                int itemsPerPage = Math.Max(1, Math.Min(100, PageSize ?? 12));
                int totalCount = filtered.Count;
                int totalPages = (int)Math.Ceiling((double)totalCount / itemsPerPage);

                if (currentPage > totalPages && totalPages > 0)
                    currentPage = totalPages;

                var paginated = filtered
                    .Skip((currentPage - 1) * itemsPerPage)
                    .Take(itemsPerPage)
                    .ToList();
                _logger.LogInformation("Preloved view subscription process completed");
                return new ClassifiedBOPageResponse<PrelovedViewSubscriptionsDto>
                {
                    Page = currentPage,
                    PerPage = itemsPerPage,
                    TotalCount = totalCount,
                    Items = paginated
                };
               
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch stores subscriptions.");
                throw;
            }
        }

        public async Task<ClassifiedBOPageResponse<PreLovedViewP2PDto>> ViewPreLovedP2PSubscriptions(string? Status, string? createdDate, string? publishedDate, int? Page, int? PageSize, string? Search, string? SortBy, string? SortOrder, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Preloved P2P internal services reached");
                
                DateTime? createdDateParsed = DateTime.TryParse(createdDate, out var tempCreatedDate) ? tempCreatedDate : (DateTime?)null;
                DateTime? publishedDateParsed = DateTime.TryParse(publishedDate, out var tempPublishedDate) ? tempPublishedDate : (DateTime?)null;


                string searchLower = Search?.ToLower();

                var subscriptionList = await _subscriptionContext.Subscriptions
   .AsNoTracking()
   .ToListAsync(cancellationToken); ;

                var subscriptionIds = subscriptionList.Select(s => s.SubscriptionId).ToList();

                var prelovedList = await _context.Preloved
           .Where(p => subscriptionIds.Contains((Guid)p.SubscriptionId))
           .Select(p => new
           {
               p.Id,
               p.SubscriptionId,
               p.AdType,
               p.Title,
               p.UserId,
               p.PublishedDate,
               p.ExpiryDate,
               p.Status,
               p.CreatedAt,
               p.Category,
               p.L1Category,
               p.Brand,
               p.UserName,
               p.Images
           })
           .ToListAsync(cancellationToken);
                _logger.LogInformation("prelovedList:" + prelovedList.Count().ToString());
                var companyIds = subscriptionList.Select(s => s.CompanyId).ToList();
                var companiesList = await _companyContext.Companies
    .Where(c => companyIds.Contains(c.Id))
    .ToListAsync(cancellationToken);
                _logger.LogInformation("companiesList:" + companiesList.Count().ToString());
                var paymentIds = subscriptionList.Select(s => s.PaymentId).ToList();
                var paymentsList = await _paymentContext.Payments
                 .Where(p => paymentIds.Contains(p.PaymentId))
                 .Select(p => new
                 {
                     p.PaymentId,
                     p.TransactionId,
                     p.AdId,
                     p.Status,
                     p.Fee
                 })
                 .ToListAsync(cancellationToken);


                _logger.LogInformation("paymentsList:" + paymentsList.Count().ToString());

                var filtered = (from preloved in prelovedList
                                join subscription in subscriptionList on preloved.SubscriptionId equals subscription.SubscriptionId
                                join company in companiesList on subscription.CompanyId equals company.Id
                                join payment in paymentsList on subscription.PaymentId equals payment.PaymentId
                                select new PreLovedViewP2PDto
                                {
                                    AdId = preloved.Id,
                                    OrderId = payment.PaymentId,
                                     AdTitle = preloved.Title,
                                      AdType = Enum.GetName(typeof(AdTypeEnum), preloved.AdType).ToString(),
                                     Brand= preloved.Brand,
                                      Category= preloved.Category,
                                       CreatedDate=preloved.CreatedAt,
                                     ExpiryDate=preloved.ExpiryDate?? new DateTime(1000, 1, 1),
                                      PublishedDate= preloved.PublishedDate ?? new DateTime(1000, 1, 1),
                                    ImageUrl = !string.IsNullOrWhiteSpace(preloved.Images?.FirstOrDefault()?.Url)
                               ? preloved.Images.First().Url
                               : string.Empty,
                                    UserName = company.CompanyName,
                                    Status = Enum.GetName(typeof(AdStatus), preloved.Status).ToString()
                                })
                .Where(x =>
                    (
                    (string.IsNullOrEmpty(Status) || x.Status == Status) &&
                   (!createdDateParsed.HasValue || x.CreatedDate >= createdDateParsed.Value) &&
                    (!publishedDateParsed.HasValue || x.PublishedDate >= publishedDateParsed.Value) &&
                    (
                        string.IsNullOrEmpty(searchLower) ||
                        x.OrderId.ToString().Contains(searchLower) ||
                        x.AdId.ToString().Contains(searchLower) ||          
                        (x.UserName != null && x.UserName.ToLower().Contains(searchLower)) ||
                        (x.AdTitle != null && x.AdTitle.ToLower().Contains(searchLower)) ||
                        (x.Category != null && x.Category.ToLower().Contains(searchLower)) ||
                          (x.SubCategory != null && x.SubCategory.ToLower().Contains(searchLower)) ||
                            (x.Brand != null && x.Brand.ToLower().Contains(searchLower)) ||
                            (x.Status != null && x.Status.ToLower().Contains(searchLower))
                    ))
                )
                .ToList();

                filtered = SortBy?.ToLower() switch
                {
                    "createddate" => SortOrder?.ToLower() == "desc"
                    ? filtered.OrderByDescending(t => t.CreatedDate).ToList()
                    : filtered.OrderBy(t => t.CreatedDate).ToList(),

                    "status" => SortOrder?.ToLower() == "desc"
                        ? filtered.OrderByDescending(t => t.Status).ToList()
                        : filtered.OrderBy(t => t.Status).ToList(),
                    "publisheddate" => SortOrder?.ToLower() == "desc"
                       ? filtered.OrderByDescending(t => t.PublishedDate).ToList()
                       : filtered.OrderBy(t => t.PublishedDate).ToList(),

                    _ => filtered.OrderBy(t => t.CreatedDate).ToList()
                };

                _logger.LogInformation("Preloved P2P DB data retrieved");

                int currentPage = Math.Max(1, Page ?? 1);
                int itemsPerPage = Math.Max(1, Math.Min(100, PageSize ?? 12));
                int totalCount = filtered.Count;
                int totalPages = (int)Math.Ceiling((double)totalCount / itemsPerPage);

                if (currentPage > totalPages && totalPages > 0)
                    currentPage = totalPages;

                var paginated = filtered
                    .Skip((currentPage - 1) * itemsPerPage)
                    .Take(itemsPerPage)
                    .ToList();
                _logger.LogInformation("Preloved P2P subscription process completed");
                return new ClassifiedBOPageResponse<PreLovedViewP2PDto>
                {
                    Page = currentPage,
                    PerPage = itemsPerPage,
                    TotalCount = totalCount,
                    Items = paginated
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch stores subscriptions.");
                throw;
            }
        }


        public async Task<ClassifiedBOPageResponse<PreLovedViewP2PTransactionDto>> ViewPreLovedP2PTransactions(string? createdDate, string? publishedDate, int? Page, int? PageSize, string? Search, string? SortBy, string? SortOrder, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Preloved P2P transactions services reached");

                DateTime? createdDateParsed = DateTime.TryParse(createdDate, out var tempCreatedDate) ? tempCreatedDate : (DateTime?)null;
                DateTime? publishedDateParsed = DateTime.TryParse(publishedDate, out var tempPublishedDate) ? tempPublishedDate : (DateTime?)null;


                string searchLower = Search?.ToLower();

                var prelovedList = await _context.Preloved.ToListAsync(cancellationToken);
                var subscriptionList = await _subscriptionContext.Subscriptions.ToListAsync(cancellationToken);
                var companyList = await _companyContext.Companies.ToListAsync(cancellationToken);
                var paymentList = await _paymentContext.Payments.ToListAsync(cancellationToken);



                var filtered = (
    from preloved in prelovedList
    join subscription in subscriptionList on preloved.SubscriptionId equals subscription.SubscriptionId
    join company in companyList on subscription.CompanyId equals company.Id
    join payment in paymentList on subscription.PaymentId equals payment.PaymentId
    select new PreLovedViewP2PTransactionDto
    {
        AdId = preloved.Id,
        OrderId = payment.PaymentId,
        SubscriptionType = subscription.ProductName,
        UserName = company.CompanyName,
        Email = company.Email,
        Mobile = company.PhoneNumber,
        Whatsapp = company.WhatsAppNumber,
        Amount = payment.Fee,
        StartDate = subscription.StartDate,
        EndDate = subscription.EndDate,
        CreateDate = preloved.CreatedAt,
        PublishedDate = preloved.PublishedDate ?? DateTime.MinValue,
        Status = Enum.GetName(typeof(Status), subscription.Status) ?? "Unknown"
    })
    .Where(x =>
        x.SubscriptionType =="Preloved 1 Month- P2 Publish" &&
        (!createdDateParsed.HasValue || x.CreateDate >= createdDateParsed.Value) &&
        (!publishedDateParsed.HasValue || x.PublishedDate >= publishedDateParsed.Value) &&
        (
            string.IsNullOrEmpty(searchLower) ||
            x.OrderId.ToString().Contains(searchLower) ||
            x.AdId.ToString().Contains(searchLower) ||
            (!string.IsNullOrEmpty(x.UserName) && x.UserName.ToLower().Contains(searchLower)) ||
            (!string.IsNullOrEmpty(x.Email) && x.Email.ToLower().Contains(searchLower)) ||
            (!string.IsNullOrEmpty(x.Mobile) && x.Mobile.ToLower().Contains(searchLower)) ||
            (!string.IsNullOrEmpty(x.Status) && x.Status.ToLower().Contains(searchLower))
        )
    )
    .ToList();

                filtered = SortBy?.ToLower() switch
                {
                    "createdate" => SortOrder?.ToLower() == "desc"
                    ? filtered.OrderByDescending(t => t.CreateDate).ToList()
                    : filtered.OrderBy(t => t.CreateDate).ToList(),

                    "status" => SortOrder?.ToLower() == "desc"
                        ? filtered.OrderByDescending(t => t.Status).ToList()
                        : filtered.OrderBy(t => t.Status).ToList(),
                    "publisheddate" => SortOrder?.ToLower() == "desc"
                       ? filtered.OrderByDescending(t => t.PublishedDate).ToList()
                       : filtered.OrderBy(t => t.PublishedDate).ToList(),

                    _ => filtered.OrderBy(t => t.CreateDate).ToList()
                };

                _logger.LogInformation("Preloved P2P DB data retrieved");

                int currentPage = Math.Max(1, Page ?? 1);
                int itemsPerPage = Math.Max(1, Math.Min(100, PageSize ?? 12));
                int totalCount = filtered.Count;
                int totalPages = (int)Math.Ceiling((double)totalCount / itemsPerPage);

                if (currentPage > totalPages && totalPages > 0)
                    currentPage = totalPages;

                var paginated = filtered
                    .Skip((currentPage - 1) * itemsPerPage)
                    .Take(itemsPerPage)
                    .ToList();
                _logger.LogInformation("Preloved P2P transactions process completed");
                return new ClassifiedBOPageResponse<PreLovedViewP2PTransactionDto>
                {
                    Page = currentPage,
                    PerPage = itemsPerPage,
                    TotalCount = totalCount,
                    Items = paginated
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch P2P transactions.");
                throw;
            }
        }

        public async Task<string> BulkEditP2PSubscriptions(BulkEditPreLovedP2PDto dto, string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("P2P edit functionality initiated.");

                var ads = await _context.Preloved
                .Where(ad => dto.AdIds.Contains(ad.Id) && ad.IsActive == true)
                .ToListAsync(cancellationToken);
                var updatedAds = new List<Preloveds>();
                foreach (var ad in ads)
                {
                    bool shouldUpdate = false;
                    switch (dto.AdStatus)
                    {
                        case BulkActionEnum.Approve:
                            if (ad.Status == AdStatus.PendingApproval)
                            {
                                ad.Status = AdStatus.Published;
                                shouldUpdate = true;
                                ad.UpdatedAt = DateTime.UtcNow;
                            }
                            else
                            {
                                throw new InvalidOperationException($"Cannot approve preloved ad with status '{ad.Status}'. Only 'PendingApproval' is allowed.");
                            }
                            break;

                        //case BulkActionEnum.NeedChanges:
                        //    if (ad.Status == AdStatus.PendingApproval)
                        //    {
                        //        ad.Status = AdStatus.NeedsModification;
                        //        shouldUpdate = true;
                        //        ad.UpdatedAt = DateTime.UtcNow;
                        //    }
                        //    else
                        //    {
                        //        throw new InvalidOperationException($"Cannot need changes ad with status '{ad.Status}'. Only 'PendingApproval' is allowed.");
                        //    }
                        //    break;

                        case BulkActionEnum.Publish:
                            if (ad.Status == AdStatus.Unpublished)
                            {
                                ad.Status = AdStatus.Published;
                                shouldUpdate = true;
                                ad.UpdatedAt = DateTime.UtcNow;
                            }
                            else
                            {
                                throw new InvalidOperationException($"Cannot publish preloved ad with status '{ad.Status}'. Only 'Unpublished' is allowed.");
                            }
                            break;

                        case BulkActionEnum.Unpublish:
                            if (ad.Status == AdStatus.Published)
                            {
                                ad.Status = AdStatus.Unpublished;
                                shouldUpdate = true;
                                ad.UpdatedAt = DateTime.UtcNow;
                            }
                            else
                            {
                                throw new InvalidOperationException($"Cannot unpublish preloved ad with status '{ad.Status}'. Only 'Published' is allowed.");
                            }
                            break;

                        case BulkActionEnum.UnPromote:
                            if (ad.IsPromoted)
                            {
                                ad.IsPromoted = false;
                                shouldUpdate = true;
                                ad.UpdatedAt = DateTime.UtcNow;

                            }
                            else
                            {
                                throw new InvalidOperationException("Cannot unpromote an preloved ad that is not promoted.");
                            }
                            break;

                        case BulkActionEnum.UnFeature:
                            if (ad.IsFeatured)
                            {
                                ad.IsFeatured = false;
                                shouldUpdate = true;
                                ad.UpdatedAt = DateTime.UtcNow;
                            }
                            else
                            {
                                throw new InvalidOperationException("Cannot unfeature an preloved ad that is not featured.");
                            }
                            break;

                        case BulkActionEnum.Promote:
                            if (!ad.IsPromoted)
                            {
                                ad.IsPromoted = true;
                                shouldUpdate = true;
                                ad.UpdatedAt = DateTime.UtcNow;
                                ad.PromotedExpiryDate = DateTime.UtcNow;
                            }
                            else
                            {
                                throw new InvalidOperationException("Cannot promote an preloved ad that is already promoted.");
                            }
                            break;

                        case BulkActionEnum.Feature:
                            if (!ad.IsFeatured)
                            {
                                ad.IsFeatured = true;
                                shouldUpdate = true;
                                ad.UpdatedAt = DateTime.UtcNow;
                                ad.FeaturedExpiryDate = DateTime.UtcNow;
                            }
                            else
                            {
                                throw new InvalidOperationException("Cannot feature an preloved ad that is already featured.");
                            }
                            break;

                        case BulkActionEnum.Remove:
                            ad.Status = AdStatus.Rejected;
                            shouldUpdate = true;
                            break;

                        case BulkActionEnum.Hold:
                            if (ad.Status == AdStatus.Draft)
                            {
                                throw new InvalidOperationException("Cannot hold an preloved ad that is in draft status.");
                            }
                            else if (ad.Status != AdStatus.Hold)
                            {
                                ad.Status = AdStatus.Hold;
                                shouldUpdate = true;
                            }
                            else
                            {
                                throw new InvalidOperationException("preloved Ad is already on hold.");
                            }
                            break;

                        case BulkActionEnum.Onhold:
                            if (ad.Status != AdStatus.Onhold)
                            {
                                ad.Status = AdStatus.Unpublished;
                                shouldUpdate = true;
                            }
                            else
                            {
                                throw new InvalidOperationException("Ad is not on hold.");
                            }
                            break;

                        default:
                            throw new InvalidOperationException("Invalid action");
                    }

                    if (shouldUpdate)
                    {
                        ad.UpdatedAt = DateTime.UtcNow;
                        ad.UpdatedBy = userId;
                        updatedAds.Add(ad);
                    }
                }
                

                if (updatedAds.Any())
                {
                    await _context.SaveChangesAsync(cancellationToken);

                   
                }

                _logger.LogInformation("Preloved P2P edit functionality completed.");
                return "Preloved P2P status updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to bulk edit functionality in P2P status.");
                throw;
            }
        }

        #endregion


    }
}
