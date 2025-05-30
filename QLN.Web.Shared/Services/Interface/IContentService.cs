namespace QLN.Web.Shared.Services.Interface
{
    public  interface IContentService
    {
        /// <summary>
        /// Gets Content Daily Landing Page data.
        /// </summary>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage?> GetDailyLPAsync();

        Task<HttpResponseMessage?> GetPostBySlugAsync(string slug, CancellationToken cancellationToken);
        Task<HttpResponseMessage?> GetEventBySlugAsync(string slug, CancellationToken cancellationToken);
        Task<T?> GetPostsFromDrupalAsync<T>(string queue_name, CancellationToken cancellationToken);
        Task<HttpResponseMessage?> GetEventsFromDrupalAsync(CancellationToken cancellationToken);
    }
}
