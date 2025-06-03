namespace QLN.Web.Shared.Services.Interface
{
    public interface IEventService
    {
        /// <summary>
        /// Gets Content Events Landing Page data.
        /// </summary>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage?> GetAllEventsAsync(string? category_id = null, string? location_id = null, string? date = null);

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
    }
}
