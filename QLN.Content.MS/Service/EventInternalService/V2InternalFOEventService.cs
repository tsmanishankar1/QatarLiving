using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IContentService;
using System.Text.Json;
using static QLN.Common.Infrastructure.Constants.ConstantValues;

namespace QLN.Content.MS.Service.EventInternalService
{
    public class V2InternalFOEventService : IV2FOEventService
    {
        private readonly DaprClient _dapr;
        public V2InternalFOEventService(DaprClient dapr)
        {
            _dapr = dapr;
        }
        public async Task<V2Events?> GetEventBySlug(string slug, CancellationToken cancellationToken = default)
        {
            try
            {
                var keys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.EventIndexKey,
                    cancellationToken: cancellationToken) ?? new();
                var items = await _dapr.GetBulkStateAsync(
                    ConstantValues.V2Content.ContentStoreName,
                    keys,
                    parallelism: null,
                    cancellationToken: cancellationToken);
                foreach (var item in items)
                {
                    if (string.IsNullOrWhiteSpace(item.Value))
                        continue;
                    var ev = JsonSerializer.Deserialize<V2Events>(item.Value, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (ev is not null && ev.IsActive &&
                        ev.Slug.Trim() == slug.Trim())
                    {
                        return ev;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
