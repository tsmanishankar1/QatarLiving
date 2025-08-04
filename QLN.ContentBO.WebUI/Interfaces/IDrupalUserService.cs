namespace QLN.ContentBO.WebUI.Interfaces
{
    public interface IDrupalUserService
    {
        Task<List<HttpResponseMessage>> SearchDrupalUsersAsync(string searchText);
    }
}