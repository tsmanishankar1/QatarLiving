using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Services.Base;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Services
{
    public class ClassifiedService : ServiceBase<ClassifiedService>, IClassifiedService
    {
        private readonly HttpClient _httpClient;

        public ClassifiedService(HttpClient httpClient, ILogger<ClassifiedService> Logger)
            : base(httpClient, Logger)
        {
            _httpClient = httpClient;
        }



    }
}
