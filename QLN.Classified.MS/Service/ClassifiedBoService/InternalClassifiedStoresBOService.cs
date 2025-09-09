using Dapr.Client;
using Google.Api;
using Google.Api.Gax.ResourceNames;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Npgsql;
using QLN.Classified.MS.Utilities;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.ClassifiedsBo;
using QLN.Common.DTOs;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IClassifiedBoService;
using QLN.Common.Infrastructure.QLDbContext;
using QLN.Common.Infrastructure.Subscriptions;
using QLN.Common.Infrastructure.Utilities;
using System.Diagnostics;
using System.Xml.Serialization;
using static System.Net.Mime.MediaTypeNames;
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
                        viewStoresSubscriptionDto.Status = Enum.GetName(typeof(SubscriptionStatus), item.Status).ToString();
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

        
        public async Task<string> GetProcessStoresCSVOld(string Url, string CsvPlatform, string? CompanyId, string? SubscriptionId,
           string? UserId,string Domain, CancellationToken cancellationToken = default)
        {
            try
            {
                List<ClassifiedStoresIndex> storesIndexList = new List<ClassifiedStoresIndex>();
                StoreIndexDto storeIndexDto = new StoreIndexDto();
                var allProducts = new List<StoreProducts>();
                var products = await GenericCSVReader.ReadCsv<ShopifyProduct>(Url);
                string FileName = StoresMapper.GetFileNameFromUrl(Url);
                StoreFlyers storeFlyers = new StoreFlyers();
                Guid StoreFlyersId = Guid.NewGuid();
                Guid FlyerId = Guid.NewGuid();
                if (_context.StoreFlyer.Where(x => x.SubscriptionId.ToString() == SubscriptionId && x.CompanyId.ToString() == CompanyId).Any())
                {
                    if (Guid.TryParse(SubscriptionId, out var subId) && Guid.TryParse(CompanyId, out var compId))
                    {
                        StoreFlyersId = _context.StoreFlyer.Where(x => x.SubscriptionId.ToString() == SubscriptionId && x.CompanyId.ToString() == CompanyId).FirstOrDefault().StoreFlyersId;

                        FlyerId = _context.StoreFlyer
                    .Where(x => x.SubscriptionId == subId && x.CompanyId == compId)
                    .Select(x => x.FlyerId)
                    .FirstOrDefault()
                    ?? Guid.Empty;

                    }
                    storeFlyers.StoreFlyersId = StoreFlyersId;
                    storeFlyers.FlyerId = FlyerId;
                    storeFlyers.CompanyId = Guid.Parse(CompanyId);
                    storeFlyers.SubscriptionId = Guid.Parse(SubscriptionId);
                    if (string.IsNullOrEmpty(FileName))
                    {
                        storeFlyers.FileName = string.Empty;
                    }
                    else
                    {
                        storeFlyers.FileName = FileName;
                    }
                }
                else
                {
                    storeFlyers.StoreFlyersId = StoreFlyersId;
                    storeFlyers.FlyerId = FlyerId;
                    storeFlyers.CompanyId = Guid.Parse(CompanyId);
                    storeFlyers.SubscriptionId = Guid.Parse(SubscriptionId);
                    if (string.IsNullOrEmpty(FileName))
                    {
                        storeFlyers.FileName = string.Empty;
                    }
                    else
                    {
                        storeFlyers.FileName = FileName;
                    }
                  
                }
                if (storeFlyers.CompanyId.HasValue && storeFlyers.CompanyId.Value != Guid.Empty && !string.IsNullOrEmpty(storeFlyers.CompanyId.ToString()))
                {
                    var company = _context.StoreCompanyDto.AsQueryable().Where(x => x.Id == storeFlyers.CompanyId).FirstOrDefault();

                    if (company != null)
                    {
                                            await _context.Database.ExecuteSqlRawAsync(
                        "CALL updatecompanystoresurl(@companyId, @storesUrl, @importType);",
                        new[]
                        {
                            new NpgsqlParameter("companyId", company.Id),
                            new NpgsqlParameter("storesUrl", Domain),
                            new NpgsqlParameter("importType", CsvPlatform)
                        });
                        storeIndexDto.CompanyId = company.Id.ToString();
                        storeIndexDto.CompanyName = company.CompanyName;
                        storeIndexDto.ImageUrl = company.CompanyLogo;
                        storeIndexDto.BannerUrl = company.CoverImage1;
                        storeIndexDto.ContactNumber = company.PhoneNumber.ToString();
                        storeIndexDto.Email = company.Email.ToString();
                        storeIndexDto.WebsiteUrl = company.WebsiteUrl;
                        storeIndexDto.SubscriptionId = storeFlyers.SubscriptionId.ToString();
                        storeIndexDto.Locations = company.BranchLocations;
                        storeIndexDto.StoreSlug = company.Slug;
                    }
                }

                foreach (var product in products)
                    {
                        StoreProducts storeProducts = StoresMapper.MapShopifyToStore(product, StoreFlyersId);
                        //if (storeFlyers.Products == null)
                        //{
                        //    storeFlyers.Products = new List<StoreProducts>();
                        //}
                        //storeProducts.FlyerId = storeFlyers.StoreFlyersId;
                        //storeProducts.StoreFlyer = storeFlyers;
                        //// storeFlyers.Products.Add(storeProducts);
                        allProducts.Add(storeProducts);

                    ClassifiedStoresIndex classifiedStoresIndex = new ClassifiedStoresIndex()
                    {

                        CompanyId = storeIndexDto.CompanyId.ToString(),
                        CompanyName = storeIndexDto.CompanyName,
                        SubscriptionId = storeIndexDto.SubscriptionId,
                        BannerUrl = storeIndexDto.BannerUrl,
                        ImageUrl = storeIndexDto.ImageUrl,
                        WebsiteUrl = storeIndexDto.WebsiteUrl,
                        Locations = storeIndexDto.Locations,
                        ContactNumber = storeIndexDto.ContactNumber.ToString(),
                        Email = storeIndexDto.Email.ToString(),
                        IsActive = true,
                        ProductId = storeProducts.StoreProductId.ToString(),
                        ProductName = storeProducts.ProductName,
                        ProductSummary = storeProducts.ProductSummary,
                        ProductLogo = storeProducts.ProductLogo,
                        ProductDescription = storeProducts.ProductDescription,
                        ProductPrice = Convert.ToDouble(storeProducts.ProductPrice),
                        Images = storeProducts.Images
    .Select(img => img.Images)
    .ToList(),
                        Currency = storeProducts.Currency.ToString(),
                        Features = storeProducts.Features.Select(x => x.Features).ToList(),
                        StoreSlug = storeIndexDto.StoreSlug,
                        ProductSlug = storeProducts.Slug,
                        ProductCategory = storeProducts.Category,
                        ProductUrl = Domain + "products/" + storeProducts.ProductBarcode

                    };
                        storesIndexList.Add(classifiedStoresIndex);
                }
           
                await DeleteStoreFlyer((Guid)storeFlyers.FlyerId);
                storeFlyers.Products = new List<StoreProducts>();
                _context.StoreFlyer.Add(storeFlyers);
                var sw = Stopwatch.StartNew();
                await _context.SaveChangesAsync();
                sw.Stop();
                Console.WriteLine($"SaveChangesAsync took {sw.Elapsed.TotalSeconds} seconds");

                foreach (var chunk in allProducts.Chunk(50))
                {
                    foreach (var product in chunk)
                    {
                        product.FlyerId = storeFlyers.StoreFlyersId;
                        product.StoreFlyer = storeFlyers;
                    }

                    _context.StoreProduct.AddRange(chunk);
                    await _context.SaveChangesAsync();
                }
                await IndexStoresToAzureSearch(storesIndexList, cancellationToken);
              
                return "created";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //_logger.LogError(ex, "Error while processing stores csv file.");
                return ex.Message;
            }
          
        }

        public async Task<string> GetProcessStoresCSV(string Url, string CsvPlatform, string? CompanyId, string? SubscriptionId,
          string? UserId, string Domain, CancellationToken cancellationToken = default)
        {
            try
            {
                List<ClassifiedStoresIndex> storesIndexList = new List<ClassifiedStoresIndex>();
                StoreIndexDto storeIndexDto = new StoreIndexDto();
                var allProducts = new List<StoreProducts>();
                Type productType = CsvPlatform switch
                {
                    "Shopify" => typeof(ShopifyProduct),
                    "WooCommerce" => typeof(WooCommerceProduct),
                    _ => throw new ArgumentException("Unsupported CsvPlatform")
                };

                var readCsvMethod = typeof(GenericCSVReader)
                    .GetMethod("ReadCsv")
                    ?.MakeGenericMethod(productType);

                if (readCsvMethod == null)
                {
                    throw new InvalidOperationException("Could not find ReadCsv method");
                }

                var readTask = (Task)readCsvMethod.Invoke(null, new object[] { Url });
                await readTask.ConfigureAwait(false);

                var resultProperty = readTask.GetType().GetProperty("Result");
                var rawProducts = resultProperty?.GetValue(readTask);


                string FileName = StoresMapper.GetFileNameFromUrl(Url);
                StoreFlyers storeFlyers = new StoreFlyers();
                Guid StoreFlyersId = Guid.NewGuid();
                Guid FlyerId = Guid.NewGuid();
                if (_context.StoreFlyer.Where(x => x.SubscriptionId.ToString() == SubscriptionId && x.CompanyId.ToString() == CompanyId).Any())
                {
                    if (Guid.TryParse(SubscriptionId, out var subId) && Guid.TryParse(CompanyId, out var compId))
                    {
                        StoreFlyersId = _context.StoreFlyer.Where(x => x.SubscriptionId.ToString() == SubscriptionId && x.CompanyId.ToString() == CompanyId).FirstOrDefault().StoreFlyersId;

                        FlyerId = _context.StoreFlyer
                    .Where(x => x.SubscriptionId == subId && x.CompanyId == compId)
                    .Select(x => x.FlyerId)
                    .FirstOrDefault()
                    ?? Guid.Empty;

                    }
                    storeFlyers.StoreFlyersId = StoreFlyersId;
                    storeFlyers.FlyerId = FlyerId;
                    storeFlyers.CompanyId = Guid.Parse(CompanyId);
                    storeFlyers.SubscriptionId = Guid.Parse(SubscriptionId);
                    if (string.IsNullOrEmpty(FileName))
                    {
                        storeFlyers.FileName = string.Empty;
                    }
                    else
                    {
                        storeFlyers.FileName = FileName;
                    }
                }
                else
                {
                    storeFlyers.StoreFlyersId = StoreFlyersId;
                    storeFlyers.FlyerId = FlyerId;
                    storeFlyers.CompanyId = Guid.Parse(CompanyId);
                    storeFlyers.SubscriptionId = Guid.Parse(SubscriptionId);
                    if (string.IsNullOrEmpty(FileName))
                    {
                        storeFlyers.FileName = string.Empty;
                    }
                    else
                    {
                        storeFlyers.FileName = FileName;
                    }

                }
                if (storeFlyers.CompanyId.HasValue && storeFlyers.CompanyId.Value != Guid.Empty && !string.IsNullOrEmpty(storeFlyers.CompanyId.ToString()))
                {
                    var company = _context.StoreCompanyDto.AsQueryable().Where(x => x.Id == storeFlyers.CompanyId).FirstOrDefault();

                    if (company != null)
                    {
                        await _context.Database.ExecuteSqlRawAsync(
    "CALL updatecompanystoresurl(@companyId, @storesUrl, @importType);",
    new[]
    {
                            new NpgsqlParameter("companyId", company.Id),
                            new NpgsqlParameter("storesUrl", Domain),
                            new NpgsqlParameter("importType", CsvPlatform)
    });
                        storeIndexDto.CompanyId = company.Id.ToString();
                        storeIndexDto.CompanyName = company.CompanyName;
                        storeIndexDto.ImageUrl = company.CompanyLogo;
                        storeIndexDto.BannerUrl = company.CoverImage1;
                        storeIndexDto.ContactNumber = company.PhoneNumber.ToString();
                        storeIndexDto.Email = company.Email.ToString();
                        storeIndexDto.WebsiteUrl = company.WebsiteUrl;
                        storeIndexDto.SubscriptionId = storeFlyers.SubscriptionId.ToString();
                        storeIndexDto.Locations = company.BranchLocations;
                        storeIndexDto.StoreSlug = company.Slug;
                    }
                }

                foreach (var rawProduct in (IEnumerable<object>)rawProducts)
                {
                    StoreProducts storeProducts = CsvPlatform switch
                    {
                        "Shopify" => StoresMapper.MapShopifyToStore((ShopifyProduct)rawProduct, StoreFlyersId),
                        "WooCommerce" => StoresMapper.MapWooCommerceToStore((WooCommerceProduct)rawProduct, StoreFlyersId),
                        _ => throw new InvalidOperationException("Unsupported platform")
                    };
                    allProducts.Add(storeProducts);

                    ClassifiedStoresIndex classifiedStoresIndex = new ClassifiedStoresIndex()
                    {

                        CompanyId = storeIndexDto.CompanyId.ToString(),
                        CompanyName = storeIndexDto.CompanyName,
                        SubscriptionId = storeIndexDto.SubscriptionId,
                        BannerUrl = storeIndexDto.BannerUrl,
                        ImageUrl = storeIndexDto.ImageUrl,
                        WebsiteUrl = storeIndexDto.WebsiteUrl,
                        Locations = storeIndexDto.Locations,
                        ContactNumber = storeIndexDto.ContactNumber.ToString(),
                        Email = storeIndexDto.Email.ToString(),
                        IsActive = true,
                        ProductId = storeProducts.StoreProductId.ToString(),
                        ProductName = storeProducts.ProductName,
                        ProductSummary = storeProducts.ProductSummary,
                        ProductLogo = storeProducts.ProductLogo,
                        ProductDescription = storeProducts.ProductDescription,
                        ProductPrice = Convert.ToDouble(storeProducts.ProductPrice),
                        Images = storeProducts.Images
    .Select(img => img.Images)
    .ToList(),
                        Currency = storeProducts.Currency.ToString(),
                        Features = storeProducts.Features.Select(x => x.Features).ToList(),
                        StoreSlug = storeIndexDto.StoreSlug,
                        ProductSlug = storeProducts.Slug,
                        ProductCategory = storeProducts.Category,
                        ProductUrl = Domain + "products/" + storeProducts.ProductBarcode

                    };
                    storesIndexList.Add(classifiedStoresIndex);
                }

                await DeleteStoreFlyer((Guid)storeFlyers.FlyerId);
                storeFlyers.Products = new List<StoreProducts>();
                _context.StoreFlyer.Add(storeFlyers);
                var sw = Stopwatch.StartNew();
                await _context.SaveChangesAsync();
                sw.Stop();
                Console.WriteLine($"SaveChangesAsync took {sw.Elapsed.TotalSeconds} seconds");

                foreach (var chunk in allProducts.Chunk(50))
                {
                    foreach (var product in chunk)
                    {
                        product.FlyerId = storeFlyers.StoreFlyersId;
                        product.StoreFlyer = storeFlyers;
                    }

                    _context.StoreProduct.AddRange(chunk);
                    await _context.SaveChangesAsync();
                }
                await IndexStoresToAzureSearch(storesIndexList, cancellationToken);

                return "created";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //_logger.LogError(ex, "Error while processing stores csv file.");
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

        private async Task IndexStoresToAzureSearch(List<ClassifiedStoresIndex> storesList, CancellationToken cancellationToken)
        {
            foreach (ClassifiedStoresIndex dto in storesList)
            {
             
                var indexDoc = new ClassifiedStoresIndex
                {
                    ContactNumber=dto.ContactNumber,
                    Email=dto.Email,
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
                    ProductSlug=dto.ProductSlug,
                     ProductCategory=dto.ProductCategory,
                     ProductUrl=dto.ProductUrl
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
      
        #endregion

    }
}
