// IClassifiedsServices.cs
namespace QLN.Web.Shared.Services.Interface
{
    public interface IClassifiedsServices
    {
        Task<HttpResponseMessage?> GetClassifiedsLPAsync();

        /// <summary>
        /// Searches classifieds with a raw dictionary as JSON body.
        /// </summary>
        /// <param name="searchPayload">The raw payload object</param>
        /// <returns>List of HttpResponseMessage wrapping results</returns>
        Task<List<HttpResponseMessage>> SearchClassifiedsAsync(object searchPayload);

        /// <summary>
        /// Gets Classifieds by Id.
        /// </summary>
        /// <param name="ClassifiedId">Classifieds Id</param>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage?> GetClassifiedsByIdAsync(string ClassifiedId);
    }
}
