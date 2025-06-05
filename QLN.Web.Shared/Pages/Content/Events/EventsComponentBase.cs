using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Pages.Content.Community;
using QLN.Web.Shared.Services.Interface;
using System.Net.Http.Json;
using static QLN.Web.Shared.Components.EventListCard.EventListCardBase;

namespace QLN.Web.Shared.Pages.Content.Events
{
    public class EventsComponentBase : LayoutComponentBase
    {
        [Inject] private IEventService _eventService { get; set; }
        [Inject] private ILogger<EventsComponentBase> Logger { get; set; }

        protected ContentEventsResponse ListOfEvents { get; set; } = new ContentEventsResponse();
        protected List<EventCategory> EventCategories { get; set; } = [];
        protected List<Area> Areas { get; set; } = [];
        protected List<ContentEvent> FeaturedEventData { get; set; } = [];

        protected List<BannerItem> DailyHeroBanners { get; set; } = new();
        protected bool isLoadingBanners = true;

        protected bool isLoadingEvents = true;
        protected bool isLoadingCategories = true;
        protected bool isLoadingFeatured = true;

        protected string SelectedPropertyTypeId;

        protected string SelectedLocationId;
        protected string SelectedDateLabel;
        private string _fromDate;
        private string _toDate;
        protected int CurrentPage { get; set; } = 1;
        protected int PageSize { get; set; } = 12;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;

            try
            {
                // Start these tasks in parallel as we always want these to show ASAP
                await Task.WhenAll(
                    LoadCategories(),
                    LoadFeaturedEvents(),
                    LoadBanners()
                );

                // Then load events as events could be a lot of them and it takes longer to fetch
                await LoadAllEvents();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnInitializedAsync error.");
            }
        }

        protected async Task HandleCategoryChanged(string category)
        {
            SelectedPropertyTypeId = category;
            //CurrentPage = 1;
            await LoadAllEvents();
            //TotalEvents = totalCount;
        }

        protected async Task HandleDateChanged((string from, string to) dateRange)
        {
            _fromDate = dateRange.from;
            _toDate = dateRange.to;

            SelectedDateLabel = $"{_fromDate} to {_toDate}";
            await LoadAllEvents();
        }

        protected async Task HandleLocationChanged(string location)
        {
            SelectedLocationId = location;
            //CurrentPage = 1;
            await LoadAllEvents();
            //TotalEvents = totalCount;
        }
        protected async Task HandlePageChange(int newPage)
        {
            CurrentPage = newPage;
            await LoadAllEvents();
        }

        protected async Task HandlePageSizeChange(int newSize)
        {
            PageSize = newSize;
            CurrentPage = 1;
            await LoadAllEvents();
        }


        private async Task LoadAllEvents()
        {
            isLoadingEvents = true;

            try
            {
                ListOfEvents = await GetAllEvents(CurrentPage, PageSize) ?? new ContentEventsResponse();
            }
            finally
            {
                isLoadingEvents = false;
                StateHasChanged();
            }
        }

        private async Task LoadCategories()
        {
            isLoadingCategories = true;

            try
            {
                var response = await _eventService.GetEventCategAndLoc();

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var data = await response.Content.ReadFromJsonAsync<CategoriesResponse>();

                    EventCategories = data?.EventCategories ?? new List<EventCategory>();

                    // Extract all unique areas from locations
                    Areas = data?.Locations?
                        .SelectMany(loc => loc.Areas)
                        .GroupBy(a => a.Id)
                        .Select(g => g.First())
                        .ToList() ?? new List<Area>();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "LoadCategories error.");
                EventCategories = new();
                Areas = new();
            }
            finally
            {
                isLoadingCategories = false;
                StateHasChanged();
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
                FeaturedEventData = featuredContent?.QlnEvents?.QlnEventsFeaturedEvents?.Items;
            }
            finally
            {
                isLoadingFeatured = false;
                StateHasChanged();
            }
        }
        private async Task LoadBanners()
        {
            isLoadingBanners = true;

            try
            {
                var banners = await FetchBannerData();
                DailyHeroBanners = banners?.ContentEventsHero ?? new List<BannerItem>();
            }
            finally
            {
                isLoadingBanners = false;
                StateHasChanged();
            }
        }

        protected async Task<ContentEventsResponse> GetAllEvents(int currentPage, int pageSize)
        {
            try
            {
                var response = await _eventService.GetAllEventsAsync(
                    category_id: SelectedPropertyTypeId,
                    location_id: SelectedLocationId,
                    from: _fromDate,
                    to: _toDate,
                    page: currentPage,
                    page_size: pageSize
                );

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


        private async Task<QlnFeaturedEventsPageResponse?> FetchFeaturedEventsData()
        {
            try
            {
                var response = await _eventService.GetFeaturedEventsAsync();
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return await response.Content.ReadFromJsonAsync<QlnFeaturedEventsPageResponse>();
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
