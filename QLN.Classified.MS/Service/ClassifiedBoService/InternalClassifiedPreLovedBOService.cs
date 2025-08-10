using Dapr.Client;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using QLN.Classified.MS.Utilities;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.ClassifiedsBo;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IClassifiedBoService;
using QLN.Common.Infrastructure.QLDbContext;
using System.ComponentModel.DataAnnotations;
using QLN.Common.Infrastructure.Subscriptions;
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
        public InternalClassifiedPreLovedBOService(DaprClient dapr, ILogger<IClassifiedPreLovedBOService> logger, QLClassifiedContext context, QLCompanyContext companyContext, QLPaymentsContext paymentContext)
        {

            _dapr = dapr;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context;
            _companyContext = companyContext;
            _paymentContext = paymentContext;
        }

        #region PreLoved back office end points
        public async Task<ClassifiedBOPageResponse<PrelovedViewSubscriptionsDto>> ViewPreLovedSubscriptions(string? subscriptionType, string? filterDate, int? Page, int? PageSize, string? Search, CancellationToken cancellationToken = default)
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

                var subscriptionObj = new SubscriptionMockDto
                {                   
                    SubscriptionId = Guid.Parse("b7d9a2c4-cc3f-4f0e-a8e7-9c3d7c7d1f22"),
                    ProductCode = SubscriptionDictionary.GetDescription("QLC-SUB-6MO-006"),
                    UserId = "8353026",
                    CompanyId = Guid.Parse("a8d9a6e9-97f6-4bd0-b31b-fc7b802ea47d"),
                    PaymentId = 2,
                    Vertical = "4",
                    StartDate = DateTime.Parse("2025-08-08T14:16:03.246962Z"),
                    EndDate = DateTime.Parse("2025-09-07T14:16:03.344516Z"),
                    Status = Enum.GetName(typeof(Status), 1).ToString(),
                    CreatedAt = DateTime.Parse("2025-08-08T14:16:07.914818Z"),
                    UpdatedAt = null,
                    MetaData = "{}"
                };
                var subscriptionP2PObj = new SubscriptionMockDto
                {                    
                    SubscriptionId = Guid.Parse("145250e1-c660-48df-85b7-9e6c8923267f"),
                    ProductCode = SubscriptionDictionary.GetDescription("QLC-P2P-PUB-006"),
                    UserId = "8353026",
                    CompanyId = Guid.Parse("a8d9a6e9-97f6-4bd0-b31b-fc7b802ea47d"),
                    PaymentId = 2,
                    Vertical = "4",
                    StartDate = DateTime.Parse("2025-08-08T14:16:03.246962Z"),
                    EndDate = DateTime.Parse("2025-09-07T14:16:03.344516Z"),
                    Status = Enum.GetName(typeof(Status), 1).ToString(),
                    CreatedAt = DateTime.Parse("2025-08-08T14:16:07.914818Z"),
                    UpdatedAt = null,
                    MetaData = "{}"
                };

                List<SubscriptionMockDto> subscriptionList = new List<SubscriptionMockDto>();
                subscriptionList.Add(subscriptionObj);
                subscriptionList.Add(subscriptionP2PObj);

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
                                    SubscriptionType = subscription.ProductCode,
                                    UserName = company.CompanyName,
                                    Email = company.Email,
                                    Mobile = company.PhoneNumber,
                                    Whatsapp = company.WhatsAppNumber,
                                    WebUrl = company.WebsiteUrl,
                                    Amount = payment.Fee,
                                    Status = subscription.Status,
                                    StartDate = subscription.StartDate,
                                    EndDate = subscription.EndDate
                                })
                .Where(x =>
                    (string.IsNullOrEmpty(subscriptionType) || x.SubscriptionType == subscriptionType) &&
                    x.StartDate >= dateThreshold &&
                    x.StartDate <= filterDateParsed &&
                    (
                        string.IsNullOrEmpty(searchLower) ||
                        x.OrderId.ToString().Contains(searchLower) ||
                        (x.UserName != null && x.UserName.ToLower().Contains(searchLower)) ||
                        (x.Email != null && x.Email.ToLower().Contains(searchLower)) ||
                        (x.Mobile != null && x.Mobile.ToLower().Contains(searchLower)) ||
                        (x.Status != null && x.Status.ToLower().Contains(searchLower))
                    )
                )
                .ToList();


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

        public async Task<ClassifiedBOPageResponse<PreLovedViewP2PDto>> ViewPreLovedP2PSubscriptions(string? createdDate, string? publishedDate, int? Page, int? PageSize, string? Search, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Preloved P2P internal services reached");
                //DateTime createdDateParsed, publishedDateParsed;
                //try
                //{
                //    if (string.IsNullOrEmpty(createdDate))
                //    {
                //        createdDateParsed = DateTime.UtcNow;
                //    }
                //    else if (!DateTime.TryParse(createdDate, out createdDateParsed))
                //    {
                //        _logger.LogWarning("Invalid filterDate format provided: {FilterDate}. Using current UTC date instead.", createdDate);
                //        createdDateParsed = DateTime.UtcNow;
                //    }
                //}
                //catch (FormatException formatEx)
                //{
                //    _logger.LogError(formatEx, "Failed to parse filterDate. Value: {FilterDate}", createdDate);
                //    throw;
                //}
                //try
                //{
                //    if (string.IsNullOrEmpty(publishedDate))
                //    {
                //        publishedDateParsed = DateTime.UtcNow;
                //    }
                //    else if (!DateTime.TryParse(publishedDate, out publishedDateParsed))
                //    {
                //        _logger.LogWarning("Invalid filterDate format provided: {FilterDate}. Using current UTC date instead.", publishedDate);
                //        publishedDateParsed = DateTime.UtcNow;
                //    }
                //}
                //catch (FormatException formatEx)
                //{
                //    _logger.LogError(formatEx, "Failed to parse filterDate. Value: {FilterDate}", publishedDate);
                //    throw;
                //}

                DateTime? createdDateParsed = DateTime.TryParse(createdDate, out var tempCreatedDate) ? tempCreatedDate : (DateTime?)null;
                DateTime? publishedDateParsed = DateTime.TryParse(publishedDate, out var tempPublishedDate) ? tempPublishedDate : (DateTime?)null;


                string searchLower = Search?.ToLower();

                var subscriptionObj = new SubscriptionMockDto
                {
                    SubscriptionId = Guid.Parse("b7d9a2c4-cc3f-4f0e-a8e7-9c3d7c7d1f22"),
                    ProductCode = SubscriptionDictionary.GetDescription("QLC-SUB-6MO-006"),
                    UserId = "8353026",
                    CompanyId = Guid.Parse("a8d9a6e9-97f6-4bd0-b31b-fc7b802ea47d"),
                    PaymentId = 2,
                    Vertical = "4",
                    StartDate = DateTime.Parse("2025-08-08T14:16:03.246962Z"),
                    EndDate = DateTime.Parse("2025-09-07T14:16:03.344516Z"),
                    Status = Enum.GetName(typeof(Status), 1).ToString(),
                    CreatedAt = DateTime.Parse("2025-08-08T14:16:07.914818Z"),
                    UpdatedAt = null,
                    MetaData = "{}"
                };
                var subscriptionP2PObj = new SubscriptionMockDto
                {
                    SubscriptionId = Guid.Parse("145250e1-c660-48df-85b7-9e6c8923267f"),
                    ProductCode = SubscriptionDictionary.GetDescription("QLC-P2P-PUB-006"),
                    UserId = "8353026",
                    CompanyId = Guid.Parse("a8d9a6e9-97f6-4bd0-b31b-fc7b802ea47d"),
                    PaymentId = 2,
                    Vertical = "4",
                    StartDate = DateTime.Parse("2025-08-08T14:16:03.246962Z"),
                    EndDate = DateTime.Parse("2025-09-07T14:16:03.344516Z"),
                    Status = Enum.GetName(typeof(Status), 1).ToString(),
                    CreatedAt = DateTime.Parse("2025-08-08T14:16:07.914818Z"),
                    UpdatedAt = null,
                    MetaData = "{}"
                };

                List<SubscriptionMockDto> subscriptionList = new List<SubscriptionMockDto>();
                subscriptionList.Add(subscriptionObj);
                subscriptionList.Add(subscriptionP2PObj);

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
               p.UserName
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
                                      AdType= preloved.AdType.ToString(),
                                     Brand= preloved.Brand,
                                      Category= preloved.Category,
                                       CreatedDate=preloved.CreatedAt,
                                     ExpiryDate=preloved.ExpiryDate?? new DateTime(1000, 1, 1),
                                      PublishedDate= preloved.PublishedDate ?? new DateTime(1000, 1, 1),
                                   
                                    UserName = company.CompanyName,
                                    Status = Enum.GetName(typeof(Status), preloved.Status).ToString()
                                })
                .Where(x =>
                    (
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

        #endregion

       
    }
}
