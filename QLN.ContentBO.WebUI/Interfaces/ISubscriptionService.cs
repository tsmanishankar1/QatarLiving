namespace QLN.ContentBO.WebUI.Interfaces
{
    public interface ISubscriptionService
    {
        Task<HttpResponseMessage?> GetAllSubscriptionProducts(int? vertical = null,
            int? subvertical = null,
            int? productType = null);
    }
}
