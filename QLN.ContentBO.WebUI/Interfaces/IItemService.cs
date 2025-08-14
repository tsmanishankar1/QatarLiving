using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Interfaces
{
    public interface IItemService
    {
        Task<HttpResponseMessage?> GetAllItemsListing(ItemsRequest itemsRequest);
        Task<HttpResponseMessage?> GetItemsTransactionListing(ItemTransactionRequest itemTransactionRequest);
        Task<HttpResponseMessage?> BulkItemsActionAsync(List<long> adIds, int action, string? reason = null, string? comments = null);
    }
}
