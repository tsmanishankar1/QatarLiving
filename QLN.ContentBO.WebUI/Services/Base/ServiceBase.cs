namespace QLN.ContentBO.WebUI.Services.Base
{
    public class ServiceBase<T> : IServiceBase where T : ServiceBase<T>
    {

        private HttpClient _HttpClient;
        public ILogger<T> Logger;
        public string BaseAddress => _HttpClient.BaseAddress.AbsoluteUri;

        public ServiceBase(HttpClient httpClient, ILogger<T> logger)
        {
            _HttpClient = httpClient;
            Logger = logger;
        }

        public Task<HttpResponseMessage> GetAsync(string? requestUri)
        {
            var response = _HttpClient.GetAsync(requestUri);
            return response;
        }

        public Task<HttpResponseMessage> PostAsync(string? requestUri, HttpContent content)
        {
            var response = _HttpClient.PostAsync(requestUri, content);
            return response;
        }

        public Task<HttpResponseMessage> PutAsync(string? requestUri, HttpContent content)
        {
            var response = _HttpClient.PutAsync(requestUri, content);
            return response;
        }
    }
}
