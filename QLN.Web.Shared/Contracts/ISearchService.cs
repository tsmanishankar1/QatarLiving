namespace QLN.Web.Shared.Contracts
{
    public interface ISearchService
    {
        Task<bool> PerformSearchAsync(string searchText);

    }
}
