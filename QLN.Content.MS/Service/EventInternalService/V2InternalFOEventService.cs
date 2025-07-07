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
        public async Task<V2Events?> GetFOEventById(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _dapr.GetStateAsync<V2Events>(
                    ConstantValues.V2Content.ContentStoreName,
                    id.ToString(),
                    cancellationToken: cancellationToken);
                if (result == null)
                    throw new KeyNotFoundException($"Event with id '{id}' was not found.");
                if (!result.IsActive)
                    return null;
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving event with ID: {id}", ex);
            }
        }
        public async Task<List<V2Events>> GetAllFOIsFeaturedEvents(bool isFeatured, CancellationToken cancellationToken)
        {
            try
            {
                var keys = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.EventIndexKey,
                    cancellationToken: cancellationToken
                ) ?? new List<string>();

                var items = await _dapr.GetBulkStateAsync(
                    ConstantValues.V2Content.ContentStoreName,
                    keys,
                    parallelism: null,
                    cancellationToken: cancellationToken
                );

                var events = items
                    .Select(i => JsonSerializer.Deserialize<V2Events>(i.Value, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }))
                    .Where(e => e != null && e.IsActive == true && e.IsFeatured == isFeatured)
                    .ToList();

                return events;
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while retrieving all events", ex);
            }
        }
        public async Task<PagedResponse<V2Events>> GetFOPagedEvents(GetPagedEventsRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                var allEvents = new List<V2Events>();
                var eventIds = await _dapr.GetStateAsync<List<string>>(
                    ConstantValues.V2Content.ContentStoreName,
                    ConstantValues.V2Content.EventIndexKey,
                    cancellationToken: cancellationToken);

                if (eventIds == null || !eventIds.Any())
                {
                    return EmptyResponse(request.Page, request.PerPage);
                }

                var fetchTasks = eventIds.Select(async eventId =>
                {
                    try
                    {
                        var ev = await _dapr.GetStateAsync<V2Events>(
                            ConstantValues.V2Content.ContentStoreName,
                            eventId,
                            cancellationToken: cancellationToken);
                        return ev;
                    }
                    catch
                    {
                        return null;
                    }
                });

                var fetchedEvents = await Task.WhenAll(fetchTasks);
                allEvents = fetchedEvents.Where(e => e != null).ToList();

                if (!allEvents.Any())
                {
                    return EmptyResponse(request.Page, request.PerPage);
                }

                if (!string.IsNullOrWhiteSpace(request.Search))
                {
                    allEvents = allEvents
                        .Where(e => !string.IsNullOrEmpty(e.EventTitle) &&
                                    e.EventTitle.Contains(request.Search, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

                foreach (var ev in allEvents)
                {
                    if (ev.EventSchedule.EndDate < today && ev.Status != EventStatus.Expired)
                    {
                        ev.Status = EventStatus.Expired;
                        await _dapr.SaveStateAsync<V2Events>(V2Content.ContentStoreName, ev.Id.ToString(), ev, cancellationToken: cancellationToken);
                    }
                }

                if (request.Status.HasValue)
                {
                    allEvents = allEvents
                        .Where(e => e.Status == request.Status.Value)
                        .ToList();
                }

                if (request.CategoryId.HasValue)
                {
                    allEvents = allEvents
                        .Where(e => e.CategoryId == request.CategoryId.Value)
                        .ToList();
                }
                if (request.FromDate.HasValue || request.ToDate.HasValue)
                {
                    allEvents = allEvents
                        .Where(e =>
                        {
                            var eventStart = e.EventSchedule.StartDate;
                            var eventEnd = e.EventSchedule.EndDate;

                            if (request.FromDate.HasValue && request.ToDate.HasValue)
                                return eventStart >= request.FromDate && eventEnd <= request.ToDate;

                            if (request.FromDate.HasValue)
                                return eventEnd >= request.FromDate;

                            if (request.ToDate.HasValue)
                                return eventStart <= request.ToDate;

                            return true;
                        })
                        .ToList();
                }
                if (!string.IsNullOrWhiteSpace(request.FilterType))
                {
                    switch (request.FilterType.ToLowerInvariant())
                    {
                        case "featured":
                            allEvents = allEvents
                                .Where(e => e.IsFeatured)
                                .ToList();
                            break;

                        case "thisweek":
                            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                            var endOfWeek = startOfWeek.AddDays(6);

                            allEvents = allEvents
                                .Where(e => e.EventSchedule.StartDate >= startOfWeek &&
                                            e.EventSchedule.StartDate <= endOfWeek)
                                .ToList();
                            break;

                        case "upcoming":
                            allEvents = allEvents
                                .Where(e => e.EventSchedule.StartDate >= today)
                                .ToList();
                            break;
                    }
                }
                if (request.LocationId is { Count: > 0 })
                {
                    allEvents = allEvents
                        .Where(e => e.LocationId != null && request.LocationId.Contains(e.LocationId.Value))
                        .ToList();
                }
                if (request.FreeOnly == true)
                {
                    allEvents = allEvents
                        .Where(e => e.EventType == V2EventType.FreeAcess || e.EventType == V2EventType.OpenRegistrations)
                        .ToList();
                }

                if (!allEvents.Any())
                    return EmptyResponse(request.Page, request.PerPage);

                request.SortOrder = string.IsNullOrWhiteSpace(request.SortOrder) ? "asc" : request.SortOrder.ToLowerInvariant();
                if (request.FeaturedFirst == true)
                {
                    allEvents = request.SortOrder switch
                    {
                        "desc" => allEvents
                            .OrderByDescending(e => e.IsFeatured)
                            .ThenByDescending(e => e.CreatedAt)
                            .ToList(),
                        _ => allEvents
                            .OrderByDescending(e => e.IsFeatured)
                            .ThenBy(e => e.CreatedAt)
                            .ToList(),
                    };
                }
                else
                {
                    allEvents = request.SortOrder switch
                    {
                        "desc" => allEvents.OrderByDescending(e => e.CreatedAt).ToList(),
                        _ => allEvents.OrderBy(e => e.CreatedAt).ToList(),
                    };
                }
                int currentPage = Math.Max(1, request.Page ?? 1);
                int itemsPerPage = Math.Max(1, Math.Min(100, request.PerPage ?? 12));
                int totalCount = allEvents.Count;
                int totalPages = (int)Math.Ceiling((double)totalCount / itemsPerPage);

                if (currentPage > totalPages && totalPages > 0)
                    currentPage = totalPages;

                var paginated = allEvents
                    .Skip((currentPage - 1) * itemsPerPage)
                    .Take(itemsPerPage)
                    .ToList();
                var featuredCount = allEvents.Count(e => e.IsFeatured);
                var featuredInCurrentPage = paginated.Count(e => e.IsFeatured);
                return new PagedResponse<V2Events>
                {
                    Page = currentPage,
                    PerPage = itemsPerPage,
                    TotalCount = totalCount,
                    Items = paginated,
                    FeaturedCount = featuredCount,
                    FeaturedInCurrentPage = featuredInCurrentPage
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error occurred while retrieving events: {ex.Message}", ex);
            }
        }
        private static PagedResponse<V2Events> EmptyResponse(int? page, int? perPage) => new()
        {
            Page = page ?? 1,
            PerPage = perPage ?? 12,
            TotalCount = 0,
            Items = new List<V2Events>()
        };
    }
}
