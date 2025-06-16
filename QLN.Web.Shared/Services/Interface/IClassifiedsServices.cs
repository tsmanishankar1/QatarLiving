namespace QLN.Web.Shared.Services.Interface
{
    public interface IClassifiedsServices
    {
         /// <summary>
        /// Gets Classifieds  Landing Page data.
        /// </summary>
        /// <returns>HttpResponseMessage</returns>
        Task<HttpResponseMessage?> GetClassifiedsLPAsync();

    }
}
