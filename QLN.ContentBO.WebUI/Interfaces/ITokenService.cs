namespace QLN.ContentBO.WebUI.Interfaces
{
    public interface ITokenService
    {
        Task<HttpResponseMessage> GetUserSync();
        Task<HttpResponseMessage> GetRefreshToken();
        Task<HttpResponseMessage> IsValid();
    }
}
