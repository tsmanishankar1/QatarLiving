namespace QLN.Web.Shared.Services.Interface
{
    public interface IContentService
    {
        /// <summary>
        /// Gets Content Daily Landing Page data.
        /// </summary>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage?> GetDailyLPAsync();
        /// <summary>
        /// Gets Featured Events for Events Landing Page.
        /// </summary>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage?> GetBannerAsync();

        Task<HttpResponseMessage?> GetPostBySlugAsync(string slug, CancellationToken cancellationToken);
        Task<HttpResponseMessage?> GetEventBySlugAsync(string slug, CancellationToken cancellationToken);
        Task<T?> GetPostsFromDrupalAsync<T>(string queue_name, CancellationToken cancellationToken);
        Task<HttpResponseMessage?> GetEventsFromDrupalAsync(CancellationToken cancellationToken);


        /// <summary>
        /// Gets Content Videos Landing Page data.
        /// </summary>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage?> GetVideosLPAsync();

        Task<HttpResponseMessage?> GetDailyLPV2Async();
    }
}
