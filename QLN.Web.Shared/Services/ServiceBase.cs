using Microsoft.Extensions.Logging;
using QLN.Web.Shared.Services.Interface;

namespace QLN.Web.Shared.Services
{
    public class ServiceBase<T> : IServiceBase where T : ServiceBase<T>
    {

        private HttpClient _HttpClient;

        public string BaseAddress => _HttpClient.BaseAddress.AbsoluteUri;

        public ServiceBase(HttpClient httpClient)
        {
            _HttpClient = httpClient;
        }
    }
}
