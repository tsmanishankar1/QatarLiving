using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Web.Shared.Services
{
    public class CommunityService : ICommunityService
    {
        private readonly HttpClient _httpClient;

        public CommunityService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<PostModel>> GetAllAsync()
        {
            var response = await _httpClient.GetFromJsonAsync<IEnumerable<PostModel>>("api/community/posts");
            return response ?? new List<PostModel>();
        }
    }
}
