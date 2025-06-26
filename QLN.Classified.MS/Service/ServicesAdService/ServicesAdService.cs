
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService;
using System.Text.Json;

namespace QLN.Classified.MS.Service.ServicesAdService
{
    public class ServicesAdService : IServicesService
    {
        private readonly string jsonPath = Path.Combine("ServicesMockData", "serviceDashboardMock.json");


        private async Task<List<ServiceAd>> ReadAllServiceAdsFromFile()
        {
            try
            {
                var basePath = AppDomain.CurrentDomain.BaseDirectory;
                var jsonString = await File.ReadAllTextAsync(jsonPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<ServiceAd>>(jsonString, options) ?? new();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading serviceDashboardMock.json: {ex}");
                return new List<ServiceAd>();
            }
        }

        public async Task<ServiceDashboardWithAdsDto> GetDashboardAndAds(string userId, CancellationToken cancellationToken = default)
        {
            var allAds = await ReadAllServiceAdsFromFile();
            var userAds = allAds.Where(ad => ad.UserId == userId).ToList();

            // Dashboard calculation
            var publishedCount = userAds.Count(ad => ad.Status == AdStatus.Published);
            var promotedCount = userAds.Count(ad => ad.IsPromoted == true);
            var featuredCount = userAds.Count(ad => ad.IsFeatured == true);
            var refreshCount = userAds.Count(ad => ad.RefreshExpiry != null);
            var totalImpressions = userAds.Sum(ad => ad.Impressions ?? 0);
            var totalViews = userAds.Sum(ad => ad.Views ?? 0);
            var totalWhatsappClicks = userAds.Sum(ad => ad.WhatsAppClicks ?? 0);
            var totalCalls = userAds.Sum(ad => ad.Calls ?? 0);

            var adWithRefresh = userAds.Where(ad => ad.RefreshExpiry != null)
                .OrderByDescending(ad => ad.RefreshExpiry)
                .FirstOrDefault();

            var dashboard = new ServiceDashboardDto
            {
                PublishedAds = publishedCount,
                PromotedAds = promotedCount,
                FeaturedAds = featuredCount,
                Refreshes = refreshCount,
                Impressions = totalImpressions,
                Views = totalViews,
                WhatsAppClicks = totalWhatsappClicks,
                Calls = totalCalls,
                RemainingRefreshes = adWithRefresh?.RemainingRefreshes ?? 0,
                TotalAllowedRefreshes = adWithRefresh?.TotalAllowedRefreshes ?? 0,
                RefreshExpiry = adWithRefresh?.RefreshExpiry
            };

            return new ServiceDashboardWithAdsDto
            {
                Dashboard = dashboard,
                PublishedAds = userAds.Where(ad => ad.Status == AdStatus.Published).OrderByDescending(ad => ad.CreatedDate).ToList(),
                UnpublishedAds = userAds.Where(ad => ad.Status == AdStatus.Unpublished).OrderByDescending(ad => ad.CreatedDate).ToList()
            };
        }
    }
}
