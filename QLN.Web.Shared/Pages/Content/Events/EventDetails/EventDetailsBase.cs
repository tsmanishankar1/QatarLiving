using Microsoft.AspNetCore.Components;
using QLN.Web.Shared.Components.BreadCrumb;
using Microsoft.Extensions.Logging;
using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Services.Interface;
using QLN.Common.Infrastructure.DTO_s;
using System.Net.Http.Json;

namespace QLN.Web.Shared.Pages.Content.Events.EventDetails
{
    public class EventDetailsBase : ComponentBase
    {
        [Inject] protected IEventService EventService { get; set; }
        [Inject] protected ILogger<EventDetailsBase> Logger { get; set; }

        [Parameter] public string slug { get; set; }

        protected bool isLoading { get; set; } = true;
        protected bool isLoadingBanners { get; set; } = false;

        protected List<BannerItem> DailyHeroBanners { get; set; } = new();
        protected ContentEvent eventDetails { get; set; }

        protected List<string> carouselImages = new()
        {
            "/images/banner_image.svg",
            "/images/banner_image.svg",
            "/images/banner_image.svg"
        };

        protected List<BreadcrumbItem> breadcrumbItems = new();

        protected override async Task OnInitializedAsync()
        {
            // Initialize breadcrumbs
            breadcrumbItems = new()
            {
                new BreadcrumbItem { Label = "Events", Url = "/content/events" },
                new BreadcrumbItem
                {
                    Label = "Event Detail",
                    Url = $"/events/details/{slug}",
                    IsLast = true
                }
            };

            try
            {
                isLoading = true;

                var response = await EventService.GetEventBySlugAsync(slug);

                if (response != null && response.IsSuccessStatusCode)
                {
                    eventDetails = await response.Content.ReadFromJsonAsync<ContentEvent>();

                    if (!string.IsNullOrWhiteSpace(eventDetails?.Title))
                    {
                        breadcrumbItems[1].Label = eventDetails.Title;
                    }
                }
                else
                {
                    Logger.LogWarning($"Failed to fetch event details for slug: {slug}. StatusCode: {response?.StatusCode}");
                }

                // Load banners after event details
                await LoadBanners();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Exception in GetEventBySlugAsync for slug: {slug}");
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task LoadBanners()
        {
            isLoadingBanners = true;
            try
            {
                var banners = await FetchBannerData();
                DailyHeroBanners = banners?.DailyHero ?? new List<BannerItem>();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading banners.");
            }
            finally
            {
                isLoadingBanners = false;
            }
        }

        private async Task<BannerResponse?> FetchBannerData()
        {
            try
            {
                var response = await EventService.GetBannerAsync();
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return await response.Content.ReadFromJsonAsync<BannerResponse>();
                }
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "FetchBannerData error.");
                return null;
            }
        }
    }
}
