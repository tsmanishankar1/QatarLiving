using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Common.Infrastructure.IService.IFileStorage;
using System.Net;

namespace QLN.Backend.API.Service.V2ContentService
{
    public class V2FOExternalEventService : IV2FOEventService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<V2ExternalEventService> _logger;
        public V2FOExternalEventService(DaprClient dapr, ILogger<V2ExternalEventService> logger)
        {
            _dapr = dapr;
            _logger = logger;
        }
        public async Task<V2Events> GetEventBySlug(string slug, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/v2/fo/event/slug/{slug}";

                return await _dapr.InvokeMethodAsync<V2Events>(
                    HttpMethod.Get,
                    ConstantValues.V2Content.ContentServiceAppId,
                    url,
                    cancellationToken
                );
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Event with Slug '{slug}' not found.", slug);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching event by slug '{slug}'", slug);
                throw;
            }
        }
        public async Task<List<V2Events>> GetAllFOIsFeaturedEvents(bool isFeatured, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<List<V2Events>>(
                    HttpMethod.Get,
                    ConstantValues.V2Content.ContentServiceAppId,
                    $"/api/v2/fo/event/getallfofeaturedevents?isFeatured={isFeatured}",
                    cancellationToken
                ) ?? new List<V2Events>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all events.");
                throw;
            }
        }
        public async Task<V2Events?> GetFOEventById(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/v2/fo/event/getfobyid/{id}";

                return await _dapr.InvokeMethodAsync<V2Events>(
                    HttpMethod.Get,
                    ConstantValues.V2Content.ContentServiceAppId,
                    url,
                    cancellationToken);
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving event for Id : {Id}", id);
                throw;
            }
        }
        public async Task<PagedResponse<V2Events>> GetFOPagedEvents(GetPagedEventsRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/api/v2/fo/event/getfopaginatedevents";
                return await _dapr.InvokeMethodAsync<GetPagedEventsRequest, PagedResponse<V2Events>>(
                    HttpMethod.Post,
                    ConstantValues.V2Content.ContentServiceAppId,
                    url,
                    request,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paged event categories.");
                throw;
            }
        }
    }
}
