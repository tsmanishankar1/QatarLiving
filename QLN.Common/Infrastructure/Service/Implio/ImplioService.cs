using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using QLN.Common.DTO_s.Implio;
using QLN.Common.Infrastructure.IService.IImplio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Service.Implio
{
    public class ImplioService(HttpClient httpClient) : IImplioService
    {
        public async Task<ImplioModerationResponse?> CreateModerationRequest(List<ImplioModerationRequest> requests, CancellationToken cancellationToken = default)
        {
            if (requests == null || !requests.Any())
            {
                throw new ArgumentException("Requests cannot be null or empty.", nameof(requests));
            }

            if (requests.Any(r => r.Result != null))
            {
                throw new ArgumentException("All requests must have a null Result when creating a new moderation request.", nameof(requests));
            }

            if (requests.Any(r => string.IsNullOrWhiteSpace(r.Id)))
            {
                throw new ArgumentException("All requests must have a valid Id.", nameof(requests));
            }

            if (requests.Any(r => r.Content == null || r.User == null || r.Location == null))
            {
                throw new ArgumentException("All requests must have valid Content, User, and Location.", nameof(requests));
            }

            var response = await httpClient.PostAsJsonAsync("/v1/ads", requests, cancellationToken);

            response.EnsureSuccessStatusCode();

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"Failed to create moderation request. Status code: {response.StatusCode}, Error: {errorContent}");
            }

            return await response.Content.ReadFromJsonAsync<ImplioModerationResponse>(cancellationToken: cancellationToken);
        }

        public async Task<ImplioModerationResponse?> UpdateModerationRequest(List<ImplioModerationRequest> requests, CancellationToken cancellationToken = default)
        {
            if (!requests.Select(x => x.Result).Any())
            {
                throw new ArgumentException("At least one request must have a Result to update.", nameof(requests));
            }

            if (requests == null || !requests.Any())
            {
                throw new ArgumentException("Requests cannot be null or empty.", nameof(requests));
            }
            var response = await httpClient.PostAsJsonAsync($"v1/ads", requests, cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<ImplioModerationResponse>(cancellationToken: cancellationToken);
        }

        public async Task<ImplioGetResponse?> GetModerationRequests(int timestamp, string taskIds, bool noAdContent = true, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(taskIds))
            {
                throw new ArgumentException("Task IDs cannot be null or empty.", nameof(taskIds));
            }

            if (taskIds.Split(',').Any(id => string.IsNullOrWhiteSpace(id)))
            {
                throw new ArgumentException("Task IDs cannot contain empty values.", nameof(taskIds));
            }

            if (timestamp <= 0)
            {
                throw new ArgumentException("Timestamp must be a positive integer.", nameof(timestamp));
            }

            var query = $"timestamp={timestamp}&taskIds={taskIds}&noAdContent={noAdContent}";

            var response = await httpClient.GetAsync($"/v1/ads?{query}", cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<ImplioGetResponse>(cancellationToken: cancellationToken);
        }

        public async Task<bool> DeleteModerationRequests(string userId, string taskIds, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
            }

            if (string.IsNullOrWhiteSpace(taskIds))
            {
                throw new ArgumentException("Task IDs cannot be null or empty.", nameof(taskIds));
            }

            if (taskIds.Split(',').Any(id => string.IsNullOrWhiteSpace(id)))
            {
                throw new ArgumentException("Task IDs cannot contain empty values.", nameof(taskIds));
            }

            if (fromDate == default || toDate == default)
            {
                throw new ArgumentException("From and To dates must be valid DateTime values.", nameof(fromDate));
            }

            if (fromDate > toDate)
            {
                throw new ArgumentException("From date cannot be later than To date.", nameof(fromDate));
            }

            var fromDateString = fromDate.ToString("yyyy-MM-ddTHH:mmZ");
            var toDateString = toDate.ToString("yyyy-MM-ddTHH:mmZ");

            var query = $"userId={userId}&taskId={taskIds}&fromDate={fromDateString}&toDate={toDateString}";

            var response = await httpClient.DeleteAsync($"/v1/ads?{query}", cancellationToken);

            response.EnsureSuccessStatusCode();

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ImplioHealthCheck(CancellationToken cancellationToken = default)
        {
            var response = await httpClient.GetAsync("/v1/health", cancellationToken);

            response.EnsureSuccessStatusCode();

            return response.IsSuccessStatusCode;

        }
    }
}
