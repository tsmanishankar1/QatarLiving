using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Services.Interface;
using System.Net.Http.Json;

namespace QLN.Web.Shared.Pages.Content.Events
{
    public class EventsComponentBase : LayoutComponentBase
    {
        [Inject] private IEventService _eventService { get; set; }
        [Inject] private ILogger<EventsComponentBase> Logger { get; set; }

        protected ContentEventsResponse ListOfEvents { get; set; } = new ContentEventsResponse();
        protected List<EventCategory> EventCategories { get; set; } = [];
        protected List<ContentEvent> FeaturedEventData { get; set; } = [];

        protected List<BannerItem> DailyHeroBanners { get; set; } = new();
        protected bool isLoadingBanners = true;

        protected bool isLoadingEvents = true;
        protected bool isLoadingCategories = true;
        protected bool isLoadingFeatured = true;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                // Start tasks in parallel
                var allEventsTask = LoadAllEvents();
                var categoriesTask = LoadCategories();
                var featuredTask = LoadFeaturedEvents();
                    var bannersTask = LoadBanners();

                await Task.WhenAll(allEventsTask, categoriesTask, featuredTask, bannersTask);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnInitializedAsync error.");
            }
        }

        private async Task LoadAllEvents()
        {
            isLoadingEvents = true;
            try
            {
                ListOfEvents = await GetAllEvents() ?? new ContentEventsResponse();
            }
            finally
            {
                isLoadingEvents = false;
            }
        }

        private async Task LoadCategories()
        {
            isLoadingCategories = true;
            try
            {
                EventCategories = await GetEventCategories() ?? [];
            }
            finally
            {
                isLoadingCategories = false;
            }
        }

        private async Task LoadFeaturedEvents()
        {
            isLoadingFeatured = true;
            try
            {
                // Fetch the full response object
                var featuredContent = await FetchFeaturedEventsData();

                // Extract only the Featured Events items list and assign
                FeaturedEventData = featuredContent?.ContentsDaily?.DailyFeaturedEvents?.Items ?? new List<ContentEvent>();
            }
            finally
            {
                isLoadingFeatured = false;
            }
        }
        private async Task LoadBanners()
        {
            isLoadingBanners = true;
            try
            {
                var banners = await FetchBannerData();
                DailyHeroBanners = banners?.ContentDailyHero ?? new List<BannerItem>();
            }
            finally
            {
                isLoadingBanners = false;
            }
        }

        protected async Task<ContentEventsResponse> GetAllEvents()
        {
            try
            {
                var response = await _eventService.GetAllEventsAsync();
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return await response.Content.ReadFromJsonAsync<ContentEventsResponse>() ?? new ContentEventsResponse();
                }
                return new ContentEventsResponse();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetAllEvents error.");
                return new ContentEventsResponse();
            }
        }

        protected async Task<List<EventCategory>> GetEventCategories()
        {
            try
            {
                var response = await _eventService.GetEventCategAndLoc();
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var data = await response.Content.ReadFromJsonAsync<CategoriesResponse>();
                    return data?.EventCategories ?? [];
                }
                return [];
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetEventCategories error.");
                return [];
            }
        }

        private async Task<ContentsDailyPageResponse?> FetchFeaturedEventsData()
        {
            try
            {
                var response = await _eventService.GetFeaturedEventsAsync();
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return await response.Content.ReadFromJsonAsync<ContentsDailyPageResponse>();
                }
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "FetchFeaturedEventsData error.");
                return null;
            }
        }

private async Task<BannerResponse?> FetchBannerData()
{
    try
    {
        var response = await _eventService.GetBannerAsync();
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
