using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Services.Base;

namespace QLN.ContentBO.WebUI.Services
{
    public class TokenService : ServiceBase<TokenService>, ITokenService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TokenService> _logger;

        public TokenService(HttpClient httpClient, ILogger<TokenService> logger)
            : base(httpClient, logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public Task<HttpResponseMessage> GetRefreshToken()
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> IsValid()
        {
            throw new NotImplementedException();
        }

        public Task<HttpResponseMessage> UserSync()
        {
            throw new NotImplementedException();
        }
    }
}
