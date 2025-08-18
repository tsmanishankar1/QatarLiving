using Dapr;
using Dapr.Client;
using Dapr.Client.Autogen.Grpc.v1;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<ClassifiedService> _logger;
        private readonly QLClassifiedContext _context;
        private readonly IWebHostEnvironment _env;
        public ClassifiedFoService(ILogger<ClassifiedService> logger, IWebHostEnvironment env, QLClassifiedContext context)
            
        {
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
    }
}
