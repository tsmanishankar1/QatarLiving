namespace QLN.Web.Shared.Contracts
{
    public interface IPostDialogService
    {
        Task<bool> PostSelectedCategoryAsync(string selectedCategoryId);
    }

}
