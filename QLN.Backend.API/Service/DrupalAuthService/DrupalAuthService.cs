using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.IAuth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QLN.Backend.API.Service.DrupalAuthService
{
    internal class DrupalAuthService(HttpClient httpClient) : IDrupalAuthService
    {
        public async Task<DrupalAuthResponse> LoginAsync(string username, string password, CancellationToken cancellationToken)
        {
            // Prepare form-url-encoded content
            var formData = new Dictionary<string, string>
            {
                { "username", username },
                { "device_type", "web" },
                { "password", password }
            };
            using var content = new FormUrlEncodedContent(formData);

            // Send POST request
            var response = await httpClient.PostAsync(DrupalContentConstants.LoginPath, content, cancellationToken);

            // Ensure success and deserialize response
            response.EnsureSuccessStatusCode();
            var drupalAuthResponse = await response.Content.ReadFromJsonAsync<DrupalAuthResponse>(cancellationToken);

            return drupalAuthResponse!;
        }

        public Task LogoutAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
