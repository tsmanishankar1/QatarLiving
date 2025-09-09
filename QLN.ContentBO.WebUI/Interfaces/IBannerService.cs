using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Interfaces
{
    /// <summary>
    /// Provides methods for managing banners.
    /// </summary>
    public interface IBannerService
    {
        /// <summary>
        /// Retrieves all available banner types.
        /// </summary>
        /// <returns>An HTTP response containing the list of banner types.</returns>
        Task<HttpResponseMessage> GetBannerTypes();

        /// <summary>
        /// Creates a new banner.
        /// </summary>
        /// <param name="banner">The banner to create.</param>
        /// <returns>An HTTP response indicating the result of the operation.</returns>
        Task<HttpResponseMessage> CreateBanner(BannerDTO banner);

        /// <summary>
        /// Updates an existing banner.
        /// </summary>
        /// <param name="banner">The banner with updated information.</param>
        /// <returns>An HTTP response indicating the result of the operation.</returns>
        Task<HttpResponseMessage> UpdateBanner(BannerDTO banner);

        /// <summary>
        /// Retrieves a banner by its unique identifier.
        /// </summary>
        /// <param name="bannerId">The unique identifier of the banner.</param>
        /// <returns>An HTTP response containing the banner details.</returns>
        Task<HttpResponseMessage> GetBannerById(Guid bannerId);

        /// <summary>
        /// Deletes a banner by its unique identifier.
        /// </summary>
        /// <param name="bannerId">The unique identifier of the banner to delete.</param>
        /// <returns>An HTTP response indicating the result of the operation.</returns>
        Task<HttpResponseMessage> DeleteBanner(Guid bannerId);
        Task<HttpResponseMessage> GetBannerByVerticalAndStatus(int? verticalId, bool? status);
        Task<HttpResponseMessage> ReorderBanner(List<string> newOrder, int verticalId, int subVerticalId, int pageId);
        
    }
}
