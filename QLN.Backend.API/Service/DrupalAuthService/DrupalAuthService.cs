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
            try
            {
                // Prepare form-url-encoded content
                var formData = new Dictionary<string, string>
            {
                { "username", username },
                { "device_type", "web" },
                { "password", password }
            };
                using var content = new FormUrlEncodedContent(formData);

                var response = await httpClient.PostAsync(DrupalContentConstants.LoginPath, content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var drupalAuthResponse = await response.Content.ReadFromJsonAsync<DrupalAuthResponse>(cancellationToken);
                    return drupalAuthResponse ?? new DrupalAuthResponse { Status = false, Message = "Empty response from Drupal" };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    return new DrupalAuthResponse
                    {
                        Status = false,
                        Message = $"Drupal login failed: {response.StatusCode}",
                    };
                }
            }
            catch (HttpRequestException ex)
            {
                return new DrupalAuthResponse
                {
                    Status = false,
                    Message = "Network error connecting to Drupal",
                };
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                return new DrupalAuthResponse
                {
                    Status = false,
                    Message = "Drupal login timeout",
                };
            }
            catch (Exception ex)
            {
                return new DrupalAuthResponse
                {
                    Status = false,
                    Message = "Unexpected error during Drupal login",
                };
            }
        }

        public Task LogoutAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
