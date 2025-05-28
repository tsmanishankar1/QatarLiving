namespace QLN.Web.Shared.Services.Interface
{
    public  interface INewsService
    {
        /// <summary>
        /// Gets Content News Landing Page data.
        /// </summary>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage?> GetNewsQatarAsync();
         /// <summary>
        /// Gets Content News Landing Page data.
        /// </summary>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage?> GetNewsMiddleEastAsync();
         /// <summary>
        /// Gets Content News Landing Page data.
        /// </summary>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage?> GetNewsWorldAsync();
         /// <summary>
        /// Gets Content News Landing Page data.
        /// </summary>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage?> GetNewsHealthAndEducationAsync();
         /// <summary>
        /// Gets Content News Landing Page data.
        /// </summary>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage?> GetNewsCommunityAsync();
        /// <summary>
        /// Gets Content News Landing Page data.
        /// </summary>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage?> GetNewsLawAsync();
    }
}
