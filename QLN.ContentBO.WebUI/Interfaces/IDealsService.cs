namespace QLN.ContentBO.WebUI.Interfaces
{
    public interface IDealsService
    {
        Task<HttpResponseMessage?> BulkActionAsync(List<long?> adIds, int action, string? reason = null, string? comments = null);
    }
}
