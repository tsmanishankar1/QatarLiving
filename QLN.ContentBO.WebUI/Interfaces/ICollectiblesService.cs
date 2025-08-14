using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Interfaces
{
    public interface ICollectiblesService
    {
        Task<HttpResponseMessage?> GetAllListing(ItemsRequest itemsRequest);
        Task<HttpResponseMessage?> GetTransactionListing(ItemTransactionRequest itemTransactionRequest);
        Task<HttpResponseMessage?> BulkActionAsync(List<long?> adIds, int action, string? reason = null, string? comments = null);
    }
}
