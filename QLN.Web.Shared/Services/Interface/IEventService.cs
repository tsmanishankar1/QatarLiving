namespace QLN.Web.Shared.Services.Interface
{
    public interface IEventService
    {
        /// <summary>
        /// Gets Content Events Landing Page data.
        /// </summary>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage?> GetAllEventsAsync();

        /// <summary>
        /// Gets Content Events by slug.
        /// </summary>
        /// <param name="eventSlug">EventSlug</param>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage?> GetEventBySlugAsync(string eventSlug);

        /// <summary>
        /// Gets Content Events Categories and Locationsg.
        /// </summary>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage?> GetEventCategAndLoc();
    }
}
