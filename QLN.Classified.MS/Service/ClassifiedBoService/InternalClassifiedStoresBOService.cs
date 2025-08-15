using Dapr.Client;
using Google.Api;
using Google.Api.Gax.ResourceNames;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using QLN.Classified.MS.Utilities;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.ClassifiedsBo;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IClassifiedBoService;
using QLN.Common.Infrastructure.QLDbContext;
using QLN.Common.Infrastructure.Subscriptions;
using System.Xml.Serialization;
namespace QLN.Classified.MS.Service.ClassifiedBoService
{
    public class InternalClassifiedStoresBOService : IClassifiedStoresBOService
    {
        private readonly Dapr.Client.DaprClient _dapr;
        private readonly ILogger<IClassifiedStoresBOService> _logger;
        private readonly QLClassifiedContext _context;
        private readonly QLCompanyContext _companyContext;
        public InternalClassifiedStoresBOService(DaprClient dapr, ILogger<IClassifiedStoresBOService> logger, QLClassifiedContext context, QLCompanyContext companyContext)
        {

            _dapr = dapr;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context;
            _companyContext = companyContext;
        }

        #region Stores back office end points
        public async Task<ClassifiedBOPageResponse<ViewStoresSubscriptionDto>> getStoreSubscriptions(string? subscriptionType, string? filterDate, int? Page, int? PageSize, string? Search, CancellationToken cancellationToken = default)
        {
            try
            {
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
                var filtered = await _context.ViewStoresSubscriptions
     .AsNoTracking()
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
                (x.Status != null && Enum.GetName(typeof(Status), x.Status).ToString().ToLower().Contains(searchLower))
            )
         )
     .ToListAsync(cancellationToken);


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

                List<ViewStoresSubscriptionDto> viewStoresSubscriptionDtos = new List<ViewStoresSubscriptionDto>();
                if(paginated!=null && paginated.Count > 0)
                {
                    foreach (var item in paginated)
                    {
                        ViewStoresSubscriptionDto viewStoresSubscriptionDto=new ViewStoresSubscriptionDto();
                        viewStoresSubscriptionDto.Amount = item.Amount;
                        viewStoresSubscriptionDto.WebUrl=item.WebUrl;
                        viewStoresSubscriptionDto.OrderId=item.OrderId;
                        viewStoresSubscriptionDto.SubscriptionId = item.SubscriptionId.ToString();
                        viewStoresSubscriptionDto.CompanyId = item.CompanyId.ToString();
                        viewStoresSubscriptionDto.CompanyName = item.CompanyName;
                        viewStoresSubscriptionDto.SubscriptionType = item.SubscriptionType;
                        viewStoresSubscriptionDto.Whatsapp=item.Whatsapp;
                        viewStoresSubscriptionDto.Email=item.Email;
                        viewStoresSubscriptionDto.StartDate = item.StartDate;
                        viewStoresSubscriptionDto.EndDate = item.EndDate;
                        viewStoresSubscriptionDto.UserId = item.UserId;
                        viewStoresSubscriptionDto.UserName = item.UserName;
                        viewStoresSubscriptionDto.Mobile=item.Mobile;
                        viewStoresSubscriptionDto.Status = Enum.GetName(typeof(Status), item.Status).ToString();
                        viewStoresSubscriptionDtos.Add(viewStoresSubscriptionDto);
                    }
                }


                return new ClassifiedBOPageResponse<ViewStoresSubscriptionDto>
                {
                    Page = currentPage,
                    PerPage = itemsPerPage,
                    TotalCount = totalCount,
                    Items = viewStoresSubscriptionDtos
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch stores subscriptions.");
                throw;
            }
        }
        public async Task<string> CreateStoreSubscriptions(StoresSubscriptionDto dto, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("create store subscriptions");
            try
            {

                _context.StoresSubscriptions.Add(dto);
                await _context.SaveChangesAsync();

                return "Store Subscription Created successfully";
            }
            catch (ArgumentException ex)
            {
                throw new InvalidDataException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating stores subscriptions.");
                throw;
            }
        }
        public async Task<string> EditStoreSubscriptions(int OrderID, string Status, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("stores edit functionality initiated.");
                var subscription = await _context.StoresSubscriptions
             .FirstOrDefaultAsync(x => x.OrderId == OrderID, cancellationToken);

                if (subscription == null)
                {
                    return "Subscription not found.";
                }

                subscription.Status = Status;
                _context.StoresSubscriptions.Update(subscription);
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("stores edit functionality completed.");
                return "Subscription status updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit stores subscriptions.");
                throw;
            }
        }

        public async Task<string> GetProcessStoresXML(string Url, string? CompanyId, string? SubscriptionId, string UserName, CancellationToken cancellationToken = default)
        {
            try
            {
                List<ClassifiedStoresIndex> storesIndexList = new List<ClassifiedStoresIndex>();
                var basePath = AppDomain.CurrentDomain.BaseDirectory;
                var xsdPath = Path.Combine(basePath, "Data", "Products.XSD");
                var manager = new ProductXmlManager(xsdPath);
                StoreIndexDto storeIndexDto = new StoreIndexDto();
                string SubscriptionName = string.Empty;

                var validationErrors = manager.ValidateXml(Url);
                if (!string.IsNullOrEmpty(validationErrors))
                    return validationErrors;

                using var httpClient = new HttpClient();
                var xmlContent = await httpClient.GetStringAsync(Url);

                var serializer = new XmlSerializer(typeof(StoreFlyer));
                using var reader = new StringReader(xmlContent);
                if (serializer.Deserialize(reader) is not StoreFlyer xmlProducts ||
                    xmlProducts.Products == null ||
                    xmlProducts.Products.Count == 0)
                {
                    return "No products found in the XML.";
                }

               
                var storeFlyer = new StoreFlyers
                {
                    Products = new List<StoreProducts>()
                };
                if (!string.IsNullOrEmpty(xmlProducts.FlyerId) && Guid.TryParse(xmlProducts.FlyerId, out var flyerId))
                {
                    storeFlyer.FlyerId = flyerId;
                }
                else
                {
                    throw new Exception("No valid flyer ID found in the XML.");
                }

                
                if (Guid.TryParse(SubscriptionId, out var fallbackSubscriptionId))
                {
                    storeFlyer.SubscriptionId = fallbackSubscriptionId;
                }
                else if(Guid.TryParse(xmlProducts.SubscriptionId, out var parsedSubscriptionId))
                {
                    storeFlyer.SubscriptionId = parsedSubscriptionId;
                }
                else
                {
                    throw new Exception("No valid subscription ID found in the XML or parameter.");
                }


                if (Guid.TryParse(CompanyId, out var fallbackCompanyId))
                {
                    storeFlyer.CompanyId = fallbackCompanyId;
                }
                else if(Guid.TryParse(xmlProducts.CompanyId, out var parsedCompanyId))
                {
                    storeFlyer.CompanyId = parsedCompanyId;
                }               
                else
                {
                    throw new Exception("No valid company ID found in the XML or parameter.");
                }

                _logger.LogInformation("Flyer: {FlyerId}", xmlProducts.FlyerId);
                _logger.LogInformation("Subscription: {SubscriptionId}", xmlProducts.SubscriptionId);
                _logger.LogInformation("Company: {CompanyId}", xmlProducts.CompanyId);


                if (storeFlyer.CompanyId.HasValue && storeFlyer.CompanyId.Value!=Guid.Empty && !string.IsNullOrEmpty(storeFlyer.CompanyId.ToString()))
                {
                    var company = (_context.StoreCompanyDto.AsQueryable()).Where(x=>x.Id== storeFlyer.CompanyId).FirstOrDefault();

                    if (company != null)
                    {
                        storeIndexDto.CompanyId= company.Id.ToString();
                        storeIndexDto.CompanyName = company.CompanyName;
                        storeIndexDto.ImageUrl=company.CompanyLogo;
                        storeIndexDto.BannerUrl = company.CoverImage1;
                        storeIndexDto.ContactNumber = company.PhoneNumber;
                        storeIndexDto.Email = company.Email;
                        storeIndexDto.WebsiteUrl = company.WebsiteUrl;
                        storeIndexDto.SubscriptionId=storeFlyer.SubscriptionId.ToString();
                        storeIndexDto.Locations = company.BranchLocations;
                        storeIndexDto.StoreSlug = company.Slug;
                    }
                }


                foreach (var xmlProduct in xmlProducts.Products)
                {
                    var storeProductId = Guid.NewGuid();
                    string SlugId = storeProductId.ToString().Substring(0, 8);
                    var storeProduct = new StoreProducts
                    {
                        StoreProductId = storeProductId,
                        ProductName = xmlProduct.ProductName,
                        ProductLogo = xmlProduct.ProductLogo,
                        ProductPrice = xmlProduct.ProductPrice,
                        Currency = xmlProduct.Currency,
                        ProductSummary = xmlProduct.ProductSummary,
                        ProductDescription = xmlProduct.ProductDescription,
                        PageNumber = xmlProduct.PageNumber,
                        PageCoordinates = xmlProduct.PageCoordinates,
                        Slug = "Stores-" + xmlProduct.ProductName + "-" + SlugId,
                        Features = xmlProduct.Features?.Select(f => new ProductFeatures
                        {
                            ProductFeaturesId = Guid.NewGuid(),
                            Features = f,
                            StoreProductId = storeProductId
                        }).ToList(),
                        Images = xmlProduct.Images?.Select(img => new ProductImages
                        {
                            ProductImagesId = Guid.NewGuid(),
                            Images = img,
                            StoreProductId = storeProductId
                        }).ToList()
                    };

                    ClassifiedStoresIndex classifiedStoresIndex = new ClassifiedStoresIndex()
                    {

                        CompanyId = storeIndexDto.CompanyId.ToString(),
                        CompanyName = storeIndexDto.CompanyName,
                        SubscriptionId = storeIndexDto.SubscriptionId,
                        BannerUrl = storeIndexDto.BannerUrl,
                        ImageUrl = storeIndexDto.ImageUrl,
                        WebsiteUrl = storeIndexDto.WebsiteUrl,
                        Locations = storeIndexDto.Locations,
                        ContactNumber = storeIndexDto.ContactNumber,
                        Email = storeIndexDto.Email,
                        IsActive =true,
                        ProductId = storeProduct.StoreProductId.ToString(),
                        ProductName = storeProduct.ProductName,
                        ProductSummary = storeProduct.ProductSummary,
                        ProductLogo = storeProduct.ProductLogo,
                        ProductDescription = storeProduct.ProductDescription,
                        ProductPrice = Convert.ToDouble(storeProduct.ProductPrice),
                        Images = xmlProduct.Features,
                        Currency = storeProduct.Currency.ToString(),
                        Features = xmlProduct.Features,
                        StoreSlug= storeIndexDto.StoreSlug,
                        ProductSlug= storeProduct.Slug
                    };

                    storesIndexList.Add(classifiedStoresIndex);
                    storeFlyer.Products.Add(storeProduct);
                }

                _logger.LogInformation("ML bind done");

                await DeleteStoreFlyer(storeFlyer.FlyerId);
                _context.StoreFlyer.Add(storeFlyer);
                await _context.SaveChangesAsync();
                await IndexDealsToAzureSearch(storesIndexList, cancellationToken);
                _logger.LogInformation("Products added successfully.");
                return "created";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing store flyer XML.");
                return ex.Message;
            }


        }
        #endregion

        #region Reuseable utility methods
        public async Task DeleteStoreFlyer(Guid FlyerId)
        {
            var flyers = await _context.StoreFlyer
                                .Where(p => p.FlyerId == FlyerId)
                                .Include(p => p.Products)
                                .ToListAsync();
            if (flyers.Any())
            {
                _context.StoreFlyer.RemoveRange(flyers);
                await _context.SaveChangesAsync();
            }

        }

        private async Task IndexDealsToAzureSearch(List<ClassifiedStoresIndex> storesList, CancellationToken cancellationToken)
        {
            foreach (ClassifiedStoresIndex dto in storesList)
            {
                var indexDoc = new ClassifiedStoresIndex
                {
                    ProductId = dto.ProductId,
                    CompanyId = dto.CompanyId,
                    SubscriptionId = dto.SubscriptionId,
                    CompanyName = dto.CompanyName,
                    ImageUrl = dto.ImageUrl,
                    BannerUrl = dto.BannerUrl,
                    WebsiteUrl = dto.WebsiteUrl,
                    Locations = dto.Locations,
                    ProductName = dto.ProductName,
                    ProductLogo = dto.ProductLogo,
                    ProductPrice = dto.ProductPrice,
                    Currency = dto.Currency,
                    ProductSummary = dto.ProductSummary,
                    ProductDescription = dto.ProductDescription,
                    Features = dto.Features,
                    Images = dto.Images,
                    IsActive = dto.IsActive,
                    StoreSlug=dto.StoreSlug,
                    ProductSlug=dto.ProductSlug
                };
                var indexRequest = new CommonIndexRequest
                {
                    IndexName = ConstantValues.IndexNames.ClassifiedStoresIndex,
                    ClassifiedStores = indexDoc
                };
                if (indexRequest != null)
                {
                    var message = new IndexMessage
                    {
                        Action = "Upsert",
                        Vertical = ConstantValues.IndexNames.ClassifiedStoresIndex,
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
        }
        #endregion

        #region Testing End Points
        public async Task<string> GetTestXMLValidation(CancellationToken cancellationToken = default)
        {
            try
            {
                string result = string.Empty;
                string errors = string.Empty;
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string xmlPath = Path.Combine(basePath, "Data", "Products.xml");
                string xsdPath = Path.Combine(basePath, "Data", "Products.XSD");
                var manager = new ProductXmlManager(xsdPath);
                errors = manager.ValidateXml(xmlPath);
                if (string.IsNullOrEmpty(errors))
                {
                    result = "Valid XML";
                    return result;
                }
                else
                {
                    return errors;
                }


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting subscription types.");
                return ex.Message;
            }
        }
        #endregion

    }
}
