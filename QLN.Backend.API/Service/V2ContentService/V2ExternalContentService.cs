using Dapr;
using Dapr.Client;
using QLN.Backend.API.Service.ContentService;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IService.V2IContent;
using static QLN.Common.Infrastructure.Constants.ConstantValues;

namespace QLN.Backend.API.Service.V2ContentService
{
    public class V2ExternalContentService: IV2ContentNews
    { 
        private readonly DaprClient _dapr;
        private readonly ILogger<ExternalContentService> _log;

        public V2ExternalContentService(DaprClient dapr, ILogger<ExternalContentService> log)
        {
            _dapr = dapr;
            _log = log;
        }

        public async Task<NewsSummary> ProcessNewsContentAsync(ContentNewsDto dto, string userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (dto == null) throw new ArgumentNullException(nameof(dto));
                if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID is required", nameof(userId));

                // Prepare metadata headers if necessary (optional)
                var metadata = new Dictionary<string, string>
                {
                    { "user-id", userId } // Only if your target service uses headers for auth/context
                };

                var result = await _dapr.InvokeMethodAsync<ContentNewsDto, NewsSummary>(
                    HttpMethod.Post,
                    V2ContentNews.v2contentServiceAppId,
                    "api/v2/content/news/process",  // Route in Content service
                    dto,
                    cancellationToken
                );

                return result;
            }
            catch (DaprException dex)
            {
                _log.LogError(dex, "Dapr error while invoking Content Service.");
                throw;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "General error while invoking Content Service.");
                throw;
            }
        }
    }
}
