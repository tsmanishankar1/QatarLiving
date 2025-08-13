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
                        dashboardHeaderDto.CompanyId = item.CompanyId.ToString();
                        dashboardHeaderDto.CompanyName = item.CompanyName;
                        dashboardHeaderDto.UserId = item.UserId.ToString();
                        dashboardHeaderDto.UserName = item.UserName;
                        dashboardHeaderDto.Status = Enum.GetName(typeof(Status), item.Status);
                        dashboardHeaderDto.StartDate=item.StartDate;
                        dashboardHeaderDto.EndDate=item.EndDate;
                        dashboardHeaderDto.XMLFeed = item.XMLFeed;
                        dashboardHeaderDto.UploadFeed=item.UploadFeed;
                        dashboardHeaderDto.CompanyLogo = item.CompanyLogo;
                        dashboardHeaderDto.SubscriptionId=item.SubscriptionId.ToString();
                        dashboardHeaderDto.SubscriptionType = item.SubscriptionType;

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

                if (query != null)
                {
                    foreach (var item in query)
                    {
                        StoresDashboardSummaryDto dashboardSummaryDto = new StoresDashboardSummaryDto();
                        dashboardSummaryDto.CompanyId = item.CompanyId.ToString();
                        dashboardSummaryDto.CompanyName = item.CompanyName;
                        dashboardSummaryDto.Inventory = item.ProductCount;
                      
                        dashboardSummaryDto.SubscriptionId = item.SubscriptionId.ToString();
                        dashboardSummaryDto.SubscriptionType = item.SubscriptionType;

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
