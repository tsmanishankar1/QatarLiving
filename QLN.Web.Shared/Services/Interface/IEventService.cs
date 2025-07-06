namespace QLN.Web.Shared.Services.Interface
{
    public interface IEventService
    {
        /// <summary>
        /// Gets Content Events Landing Page data.
        /// </summary>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage?> GetAllEventsAsync(string? category_id = null, string? location_id = null, string? from = null, string? to = null, int? page = 1, int? page_size = 20, string? order = "desc");

        /// <summary>
        /// Gets Content Event by slug.
        /// </summary>
        /// <param name="eventSlug">Event slug</param>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage?> GetEventBySlugAsync(string eventSlug);

        /// <summary>
        /// Gets Event Categories and Locations.
        /// </summary>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage?> GetEventCategAndLoc();

        /// <summary>
        /// Gets Featured Events for Events Landing Page.
        /// </summary>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage?> GetFeaturedEventsAsync();

        /// <summary>
        /// Gets Featured Events for Events Landing Page.
        /// </summary>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage?> GetBannerAsync();

        Task<HttpResponseMessage?> GetAllEventsV2Async(bool? isFeatured = null);
        Task<HttpResponseMessage?> GetEventByIdV2Async(Guid id);
        Task<HttpResponseMessage> GetEventLocations();
        Task<HttpResponseMessage> GetEventCategoriesV2();
        Task<HttpResponseMessage> GetEventsByPagination(
    int page,
    int perPage,
    string? search = null,
    int? categoryId = null,
    string? sortOrder = null,
    string? fromDate = null,
    string? toDate = null,
    string? filterType = null,
    List<int>? locationId = null,
    bool? freeOnly = null,
    bool? featuredFirst = null,
    int? status = null
);


        
    }
}
