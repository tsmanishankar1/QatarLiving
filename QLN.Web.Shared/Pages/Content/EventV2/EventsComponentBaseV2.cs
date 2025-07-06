using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Pages.Content.Community;
using QLN.Web.Shared.Services.Interface;
using System.Net.Http.Json;
using System.Text.Json;


namespace QLN.Web.Shared.Pages.Content.EventV2
{
    public class EventsComponentBaseV2 : LayoutComponentBase
    {
        [Inject] private IEventService _eventService { get; set; }
        [Inject] private ISimpleMemoryCache _simpleCacheService { get; set; }

        [Inject] private ILogger<EventsComponentBaseV2> Logger { get; set; }
         protected PaginatedEventResponse PaginatedData { get; set; } = new();
        protected List<EventCategoryV2> EventCategories { get; set; } = [];
        protected List<LocationDto.AreaDto> Areas { get; set; } = [];

        protected List<EventDTOV2> FeaturedEventsV2Data { get; set; } = [];

        protected List<BannerItem> DailyHeroBanners { get; set; } = new();
        protected bool isLoadingBanners = true;

        protected bool isLoadingEvents = true;
        protected bool isLoadingCategories = true;
        protected bool isLoadingFeatured = true;

        protected string SelectedPropertyTypeId;

        protected List<string> SelectedLocationIds { get; set; } = new();
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
                    LoadLocationsAndAreas(),
                    LoadFeaturedEventsv2(),
                    GetEventsCategoriesV2(),
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
            await GetEvents();
            //TotalEvents = totalCount;
        }

        protected async Task HandleDateChanged((string from, string to) dateRange)
        {
            _fromDate = dateRange.from;
            _toDate = dateRange.to;

            SelectedDateLabel = $"{_fromDate} to {_toDate}";
            await GetEvents();
        }

        protected async Task HandleLocationChanged(List<string> locations)
        {
            SelectedLocationIds = locations ?? new List<string>();
            //CurrentPage = 1;
            await LoadAllEvents();
            //TotalEvents = totalCount;
        }
        protected async Task HandlePageChange(int newPage)
        {
            CurrentPage = newPage;
            await GetEvents();
        }

        protected async Task HandlePageSizeChange(int newSize)
        {
            PageSize = newSize;
            CurrentPage = 1;
            await GetEvents();
        }


            private async Task LoadAllEvents()
        {
            isLoadingEvents = true;

            try
            {
                await GetEvents(CurrentPage, PageSize);
            }
            finally
            {
                isLoadingEvents = false;
                StateHasChanged();
            }
        }

       private async Task LoadFeaturedEventsv2()
        {
            isLoadingFeatured = true;

            try
            {
                var response = await _eventService.GetAllEventsV2Async();
                FeaturedEventsV2Data = await response.Content.ReadFromJsonAsync<List<EventDTOV2>>();

            }
            finally
            {
                isLoadingFeatured = false;
                StateHasChanged();
            }
        }
          private async Task<List<EventCategoryV2>> GetEventsCategoriesV2()
         {
            isLoadingCategories = true;
            try
            {
                var apiResponse = await _eventService.GetEventCategoriesV2();
                if (apiResponse.IsSuccessStatusCode)
                {
                    var data = await apiResponse.Content.ReadFromJsonAsync<List<EventCategoryV2>>() ?? [];
                    EventCategories = data;
                    return EventCategories;
                }
                else
                {
                    Logger.LogWarning("Failed to fetch EventCategories. StatusCode: {StatusCode}", apiResponse.StatusCode);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Exception in GetEventsCategories");
            }
            finally
            {
                isLoadingCategories = false;
            }

            return [];
        }
 private async Task LoadLocationsAndAreas()
{
    isLoadingCategories = true;

    try
    {
        var apiResponse = await _eventService.GetEventLocations();

        if (apiResponse.IsSuccessStatusCode)
        {
            var response = await apiResponse.Content.ReadFromJsonAsync<LocationDto.LocationListResponseDto>();

            if (response?.Locations != null)
            {
                Areas = response.Locations
                    .SelectMany(location =>
                        (location.Areas ?? new List<LocationDto.AreaDto>())
                        .Append(new LocationDto.AreaDto
                        {
                            Id = location.Id,
                            Name = location.Name,
                            Latitude = location.Latitude,
                            Longitude = location.Longitude
                        })
                    )
                    .GroupBy(area => area.Id)
                    .Select(g => g.First())
                    .ToList();
            }
            else
            {
                Areas = new List<LocationDto.AreaDto>();
            }
        }
        else
        {
            Logger.LogWarning("Failed to fetch event locations. StatusCode: {StatusCode}", apiResponse.StatusCode);
            Areas = new List<LocationDto.AreaDto>();
        }
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Error loading locations and areas.");
        Areas = new List<LocationDto.AreaDto>();
    }
    finally
    {
        isLoadingCategories = false;
        StateHasChanged();
    }
}


        private async Task LoadBanners()
        {
            isLoadingBanners = true;

            try
            {
                var banners = await _simpleCacheService.GetBannerAsync();
                DailyHeroBanners = banners?.ContentEventsHero ?? new List<BannerItem>();
            }
            finally
            {
                isLoadingBanners = false;
                StateHasChanged();
            }
        }

     private async Task GetEvents(
    int page = 1,
    int pageSize = 12,
    string search = "",
    string sortOrder = "desc"
)
{
    try
    {
        isLoadingEvents = true;

        int? categoryId = int.TryParse(SelectedPropertyTypeId, out var parsedId) ? parsedId : null;

        var apiResponse = await _eventService.GetEventsByPagination(
            page: page,
            perPage: pageSize,
            search: search ?? "",
            categoryId: categoryId,
            sortOrder: sortOrder,
            fromDate: _fromDate,
            toDate: _toDate,
            filterType: "",
            locationId: SelectedLocationIds?
                .Where(id => int.TryParse(id, out _)) // filter out non-integer strings
                .Select(int.Parse)
                .ToList(),
            freeOnly: false,
            featuredFirst: false
        );

        if (apiResponse.IsSuccessStatusCode)
        {
            var result = await apiResponse.Content.ReadFromJsonAsync<PaginatedEventResponse>();

            if (result != null)
            {
                result.Items = result.Items
                    .Where(e => e.IsActive)
                    .ToList();

                // Logger.LogInformation("GetEvents response: {Response}", JsonSerializer.Serialize(result));
            }

            PaginatedData = result ?? new PaginatedEventResponse();
        }
        else
        {
            Logger.LogWarning("GetEventsByPagination failed. StatusCode: {StatusCode}", apiResponse.StatusCode);
            PaginatedData = new PaginatedEventResponse();
        }
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Error fetching paginated events");
        PaginatedData = new PaginatedEventResponse();
    }
    finally
    {
        isLoadingEvents = false;
        StateHasChanged();
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
