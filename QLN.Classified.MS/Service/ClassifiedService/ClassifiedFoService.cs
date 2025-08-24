using Dapr;
using Dapr.Client;
using Dapr.Client.Autogen.Grpc.v1;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Logging;
using Npgsql;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Classifieds;
using QLN.Common.DTO_s.ClassifiedsBo;
using QLN.Common.DTO_s.ClassifiedsFo;
using QLN.Common.DTO_s.Company;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService;
using QLN.Common.Infrastructure.IService.ISearchService;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.QLDbContext;
using QLN.Common.Infrastructure.Service.FileStorage;
using QLN.Common.Infrastructure.Subscriptions;
using QLN.Common.Infrastructure.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using static Dapr.Client.Autogen.Grpc.v1.Dapr;
using static QLN.Common.DTO_s.ClassifiedsIndex;
namespace QLN.Classified.MS.Service
{
    public class ClassifiedFoService:IClassifiedsFoService
    {
        private readonly Dapr.Client.DaprClient _dapr;
        private readonly ILogger<ClassifiedService> _logger;
        private readonly QLClassifiedContext _context;
        private readonly IWebHostEnvironment _env;
        public ClassifiedFoService(Dapr.Client.DaprClient dapr, ILogger<ClassifiedService> logger, IWebHostEnvironment env, QLClassifiedContext context)
            
        {
            _dapr = dapr;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _env = env;
            _context = context;           
        }

        public  async Task<List<StoresDashboardHeaderDto>> GetStoresDashboardHeader(string? UserId, string? CompanyId,CancellationToken cancellationToken = default)
        {
            try
            {
                List<StoresDashboardHeaderDto> storesDashboardHeaderDtos = new List<StoresDashboardHeaderDto>();
                var query = _context.StoresDashboardHeaderItems.AsQueryable();

                if (query != null)
                {
                    foreach (var item in query)
                    {
                        StoresDashboardHeaderDto dashboardHeaderDto = new StoresDashboardHeaderDto();
                        dashboardHeaderDto.CompanyId = item?.CompanyId?.ToString() ?? string.Empty;
                        dashboardHeaderDto.CompanyName = item?.CompanyName ?? string.Empty;
                        dashboardHeaderDto.UserId = item?.UserId ?? string.Empty;
                        dashboardHeaderDto.UserName = item?.UserName ?? string.Empty;
                        dashboardHeaderDto.Status = Enum.GetName(typeof(SubscriptionStatus), item.Status);
                        dashboardHeaderDto.CompanyVerificationStatus = Enum.GetName(typeof(VerifiedStatus), item.CompanyVerificationStatus);
                        dashboardHeaderDto.StartDate=item.StartDate;
                        dashboardHeaderDto.EndDate=item.EndDate;
                        dashboardHeaderDto.XMLFeed = item?.XMLFeed??string.Empty;
                        dashboardHeaderDto.UploadFeed=item?.UploadFeed??string.Empty;
                        dashboardHeaderDto.CompanyLogo = item?.CompanyLogo?? string.Empty;
                        dashboardHeaderDto.SubscriptionId=item?.SubscriptionId.ToString()??string.Empty;
                        dashboardHeaderDto.SubscriptionType = item?.SubscriptionType ?? string.Empty;

                        storesDashboardHeaderDtos.Add(dashboardHeaderDto);
                    }
                }

                if (!string.IsNullOrEmpty(UserId))
                    storesDashboardHeaderDtos = storesDashboardHeaderDtos.Where(x => x.UserId == UserId).ToList();

                if (!string.IsNullOrEmpty(CompanyId))
                    storesDashboardHeaderDtos = storesDashboardHeaderDtos.Where(x => x.CompanyId == CompanyId).ToList();

                return storesDashboardHeaderDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving Stores Dashboard Header.");
                throw new InvalidOperationException("An unexpected error occurred while retrieving the dashboard header.", ex);
            }
        }

        public async Task<List<StoresDashboardSummaryDto>> GetStoresDashboardSummary(string? CompanyId, string? SubscriptionId, CancellationToken cancellationToken = default)
        {
            try
            {
                List<StoresDashboardSummaryDto> storesDashboardSummaryDtos = new List<StoresDashboardSummaryDto>();
                var query = _context.StoresDashboardSummaryItems.AsQueryable();
                List<StoreSubscriptionQuota> storeSubscriptionQuotaDtos = new List<StoreSubscriptionQuota>();
                var Quotas = _context.StoreSubscriptionQuotaDtos.AsQueryable();
                if (Quotas.Any())
                {
                    foreach(var item in Quotas)
                    {
                        int Totalcount = 0;
                        int Product = 0;
                        var json = item.QuotaJson;
                        if (json != null) {
                            var quotaUsage = JsonSerializer.Deserialize<QuotaUsageSummary>(json, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });
                            if (quotaUsage != null)
                            {
                                Totalcount=quotaUsage.TotalAdsAllowed;
                                Product = quotaUsage.AdsUsed;
                            }
                        }

                        storeSubscriptionQuotaDtos.Add(new StoreSubscriptionQuota()
                        {
                            TotalInventory = Totalcount,
                            Inventory = Product,
                            SubscriptionId = item.SubscriptionId
                        });
                    }
                }

                if (query.Any())
                {
                    foreach (var item in query)
                    {
                        StoresDashboardSummaryDto dashboardSummaryDto = new StoresDashboardSummaryDto();
                        dashboardSummaryDto.CompanyId = item?.CompanyId?.ToString() ?? string.Empty;
                        dashboardSummaryDto.CompanyName = item?.CompanyName ?? string.Empty;
                        dashboardSummaryDto.Inventory = item?.ProductCount ?? 0;

                      
                        dashboardSummaryDto.SubscriptionId = item?.SubscriptionId?.ToString() ?? string.Empty;
                        dashboardSummaryDto.SubscriptionType = item?.SubscriptionType ?? string.Empty;

                        dashboardSummaryDto.InventoryTotal = storeSubscriptionQuotaDtos.Where(x => x.SubscriptionId.ToString() == dashboardSummaryDto.SubscriptionId).FirstOrDefault().TotalInventory;


                       storesDashboardSummaryDtos.Add(dashboardSummaryDto);
                    }
                }

                if (!string.IsNullOrEmpty(SubscriptionId))
                    storesDashboardSummaryDtos = storesDashboardSummaryDtos.Where(x => x.SubscriptionId == SubscriptionId).ToList();

                if (!string.IsNullOrEmpty(CompanyId))
                    storesDashboardSummaryDtos = storesDashboardSummaryDtos.Where(x => x.CompanyId == CompanyId).ToList();

                return storesDashboardSummaryDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving Stores Dashboard Summary.");
                throw new InvalidOperationException("An unexpected error occurred while retrieving the dashboard summary.", ex);
            }
        }

        public async Task<string> GetFOProcessStoresCSV(string Url, string CsvPlatform, string? CompanyId, string? SubscriptionId,
          string? UserId, string Domain, CancellationToken cancellationToken = default)
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
                        ProductUrl = Domain + "Products/" + storeProducts.ProductBarcode

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
                    ContactNumber = dto.ContactNumber,
                    Email = dto.Email,
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
                    StoreSlug = dto.StoreSlug,
                    ProductSlug = dto.ProductSlug,
                    ProductCategory = dto.ProductCategory,
                    ProductUrl = dto.ProductUrl
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
    }
}
