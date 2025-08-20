namespace QLN.ContentBO.WebUI.Interfaces
{
    public interface ISubscriptionService
    {
        Task<HttpResponseMessage?> GetAllSubscriptionProductsAsync(int? vertical = null,
            int? subvertical = null,
            int? productType = null);
    }
}
