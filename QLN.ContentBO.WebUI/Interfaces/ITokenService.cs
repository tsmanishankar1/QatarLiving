namespace QLN.ContentBO.WebUI.Interfaces
{
    public interface ITokenService
    {
        Task<HttpResponseMessage> UserSync();
        Task<HttpResponseMessage> GetRefreshToken();
        Task<HttpResponseMessage> IsValid();
    }
}
