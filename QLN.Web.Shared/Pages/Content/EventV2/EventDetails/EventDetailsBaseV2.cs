using Microsoft.AspNetCore.Components;
using QLN.Web.Shared.Components.BreadCrumb;
using Microsoft.Extensions.Logging;
using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Services.Interface;
using QLN.Common.Infrastructure.DTO_s;
using System.Net.Http.Json;

namespace QLN.Web.Shared.Pages.Content.EventV2.EventDetails
{
    public class EventDetailsBaseV2 : ComponentBase
    {
        [Inject] protected IEventService EventService { get; set; }
        [Inject] protected ISimpleMemoryCache _simpleCacheService { get; set; }
        [Inject] protected ILogger<EventDetailsBaseV2> Logger { get; set; }

        [Parameter] public string Id { get; set; }

        protected bool isLoading { get; set; } = true;
        protected bool isLoadingEventBanners { get; set; } = true;

        protected List<BannerItem> DailyHeroBanners { get; set; } = new();
        protected List<BannerItem> EventSideBanners { get; set; } = new();
        protected EventDTOV2 eventDetails { get; set; }

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
                new BreadcrumbItem { Label = "Events", Url = "/content/eventsV2" },
                new BreadcrumbItem
                {
                    Label = "Event Detail",
                    Url = $"/content/events/details/{Id}",
                    IsLast = true
                }
            };

            try
            {
                isLoading = true;

                var guid = Guid.Parse(Id);
                var response = await EventService.GetEventByIdV2Async(guid);
                if (response != null && response.IsSuccessStatusCode)
                {
                    eventDetails = await response.Content.ReadFromJsonAsync<EventDTOV2>();

                    if (!string.IsNullOrWhiteSpace(eventDetails?.EventTitle))
                    {
                        breadcrumbItems[1].Label = eventDetails.EventTitle;
                    }
                }
                else
                {
                    Logger.LogWarning($"Failed to fetch event details for slug: {Id}. StatusCode: {response?.StatusCode}");
                }

                // Load banners after event details
                await LoadBanners();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Exception in GetEventBySlugAsync for slug: {Id}");
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task LoadBanners()
        {
            isLoadingEventBanners = true;
            try
            {
                var banners = await _simpleCacheService.GetBannerAsync();
                DailyHeroBanners = banners?.ContentEventsDetailHero ?? new List<BannerItem>();
                EventSideBanners = banners?.ContentEventsDetailSide ?? new List<BannerItem>();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading banners.");
            }
            finally
            {
                isLoadingEventBanners = false;
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
