using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.ISearchService;
using QLN.Common.Infrastructure.Utilities;
using System.Net;
using System.Text;
using System.Text.Json;

namespace QLN.Backend.API.Service.V2ContentService
{
    public class V2ExternalEventService : IV2EventService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<V2ExternalEventService> _logger;
        private readonly IFileStorageBlobService _blobStorage;
        private readonly ISearchService _search;
        public V2ExternalEventService(DaprClient dapr, ILogger<V2ExternalEventService> logger, IFileStorageBlobService blobStorage, ISearchService search)
        {
            _dapr = dapr;
            _logger = logger;
            _blobStorage = blobStorage;
            _search = search;
        }
        private static string TodayIsoUtc() => DateTime.UtcNow.Date.ToString("o"); // midnight UTC
        private static string NotExpiredClause() => $"EventSchedule/EndDate ge {TodayIsoUtc()}";
        private static string ExpiredClause() => $"EventSchedule/EndDate lt {TodayIsoUtc()}";

        private static V2Events MapIndexToDto(ContentEventsIndex i)
        {
            _ = i ?? throw new ArgumentNullException(nameof(i));
            Guid.TryParse(i.Id, out var id);

            static TEnum ParseEnum<TEnum>(string? s, TEnum fallback) where TEnum : struct
                => Enum.TryParse<TEnum>(s ?? string.Empty, true, out var v) ? v : fallback;

            return new V2Events
            {
                Id = id,
                Slug = i.Slug,
                CategoryId = i.CategoryId,
                CategoryName = i.CategoryName,
                EventTitle = i.EventTitle,
                EventType = ParseEnum(i.EventType, V2EventType.FreeAcess),
                Price = i.Price,
                LocationId = i.LocationId,
                Location = i.Location,
                Venue = i.Venue,
                Longitude = i.Longitude,
                Latitude = i.Latitude,
                RedirectionLink = i.RedirectionLink,
                EventDescription = i.EventDescription,
                CoverImage = i.CoverImage,
                IsFeatured = i.IsFeatured,
                FeaturedSlot = i.FeaturedSlot is null
                    ? new V2Slot()
                    : new V2Slot { Id = i.FeaturedSlot.Id, Name = i.FeaturedSlot.Name },
                Status = ParseEnum(i.Status, EventStatus.UnPublished),
                PublishedDate = i.PublishedDate,
                IsActive = i.IsActive,
                CreatedBy = i.CreatedBy,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt,
                UpdatedBy = i.UpdatedBy,

                EventSchedule = i.EventSchedule is null
                    ? null
                    : new EventSchedule
                    {
                        StartDate = i.EventSchedule.StartDate.ToDateOnly(),
                        EndDate = i.EventSchedule.EndDate.ToDateOnly(),
                        GeneralTextTime = i.EventSchedule.GeneralTextTime,
                        TimeSlotType = ParseEnum(i.EventSchedule.TimeSlotType, V2EventTimeType.GeneralTime),
                        TimeSlots = i.EventSchedule.TimeSlots?.Select(ts => new TimeSlot
                        {
                            DayOfWeek = ParseEnum(ts.DayOfWeek, DayOfWeek.Sunday),
                            TextTime = ts.TextTime
                        }).ToList()
                    }
            };
        }

        private static string OrList(string field, IEnumerable<int> values)
            => string.Join(" or ", values.Select(v => $"{field} eq {v}"));

        private static string Escape(string s) => s.Replace("'", "''");

        public async Task<string> CreateEvent(string userId, V2Events dto, CancellationToken cancellationToken = default)
        {
            string? FileName = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(dto.CoverImage))
                {
                    var (ext, base64Data) = Base64Helper.ParseBase64(dto.CoverImage);
                    if (ext is not ("jpeg" or "png" or "jpg" or "svg" or "webp"))
                        throw new ArgumentException("Cover Image must be in Jpeg, PNG, Webp, svg or JPG format.");
                    var imageName = $"{dto.EventTitle}_{userId}.{ext}";
                    var blobUrl = await _blobStorage.SaveBase64File(base64Data, imageName, "imageurl", cancellationToken);
                    dto.CoverImage = blobUrl;
                }
                var url = "/api/v2/event/createbyuserid";

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.V2Content.ContentServiceAppId, url);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);

                    string errorMessage;
                    try
                    {
                        var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                        errorMessage = problem?.Detail ?? "Unknown validation error.";
                    }
                    catch
                    {
                        errorMessage = errorJson;
                    }
                    await CleanupUploadedFiles(FileName, cancellationToken);
                    throw new InvalidDataException(errorMessage);
                }
                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

                return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
            }
            catch (Exception ex)
            {
                await CleanupUploadedFiles(FileName, cancellationToken);
                _logger.LogError(ex, "Error creating event");
                throw;
            }
        }
        private async Task CleanupUploadedFiles(string? file, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(file))
                await _blobStorage.DeleteFile(file, "CoverImage", cancellationToken);
        }
        public async Task<List<V2Events>> GetAllEvents(CancellationToken cancellationToken = default)
        {
            var req = new RawSearchRequest
            {
                Filter = $"IsActive eq true and {NotExpiredClause()}",
                OrderBy = "PublishedDate desc, CreatedAt desc",
                Top = 100,
                Skip = 0,
                Text = "*",
                IncludeTotalCount = false
            };

            var res = await _search.SearchRawAsync<ContentEventsIndex>(
                ConstantValues.IndexNames.ContentEventsIndex, req, cancellationToken);

            return (res.Items ?? new List<ContentEventsIndex>()).Select(MapIndexToDto).ToList();
        }
        public async Task<List<V2Events>> GetAllIsFeaturedEvents(bool isFeatured, CancellationToken cancellationToken = default)
        {
            var req = new RawSearchRequest
            {
                Filter = $"IsActive eq true and IsFeatured eq {isFeatured.ToString().ToLowerInvariant()} and {NotExpiredClause()}",
                OrderBy = "CreatedAt desc",
                Top = 200,
                Skip = 0,
                Text = "*",
                IncludeTotalCount = false
            };

            var res = await _search.SearchRawAsync<ContentEventsIndex>(
                ConstantValues.IndexNames.ContentEventsIndex, req, cancellationToken);

            return (res.Items ?? new List<ContentEventsIndex>()).Select(MapIndexToDto).ToList();
        }
        public async Task<V2Events?> GetEventById(Guid id, CancellationToken cancellationToken = default)
        {
            var doc = await _search.GetByIdAsync<ContentEventsIndex>(
                ConstantValues.IndexNames.ContentEventsIndex, id.ToString());

            if (doc is null || !doc.IsActive) return null;
            return MapIndexToDto(doc);
        }
        public async Task<string> UpdateEvent(string userId, V2Events dto, CancellationToken cancellationToken = default)
        {
            string? FileName = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(dto.CoverImage) && !dto.CoverImage.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    var (ext, base64Data) = Base64Helper.ParseBase64(dto.CoverImage);
                    if (ext is not ("jpeg" or "png" or "jpg" or "svg" or "webp"))
                        throw new ArgumentException("Cover Image must be in Jpeg, PNG, Webp, svg or JPG format.");
                    var imageName = $"{dto.EventTitle}_{userId}.{ext}";
                    var blobUrl = await _blobStorage.SaveBase64File(base64Data, imageName, "imageurl", cancellationToken);
                    dto.CoverImage = blobUrl;
                }

                var url = "/api/v2/event/updatebyuserid";

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, ConstantValues.V2Content.ContentServiceAppId, url);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);

                    string errorMessage;
                    try
                    {
                        var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                        errorMessage = problem?.Detail ?? "Unknown validation error.";
                    }
                    catch
                    {
                        errorMessage = errorJson;
                    }

                    await CleanupUploadedFiles(FileName, cancellationToken);

                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.Conflict:
                            throw new InvalidOperationException(errorMessage);

                        case HttpStatusCode.BadRequest:
                            throw new InvalidDataException(errorMessage);

                        case HttpStatusCode.NotFound:
                            throw new KeyNotFoundException(errorMessage);

                        default:
                            throw new DaprServiceException((int)response.StatusCode, errorMessage);
                    }
                }
                response.EnsureSuccessStatusCode();

                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

                return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
            }
            catch(InvalidOperationException e)
            {
                await CleanupUploadedFiles(FileName, cancellationToken);
                _logger.LogError(e, "Invalid operation while updating event");
                throw new InvalidOperationException("Invalid operation while updating event", e);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, $"Validation error while updating event {dto.Id}");
                throw new BadHttpRequestException(ex.Message, statusCode: 400);
            }
            catch (InvocationException ex)
            {
                await CleanupUploadedFiles(FileName, cancellationToken);
                var status = ex.Response?.StatusCode ?? HttpStatusCode.BadGateway;
                string body = ex.Response?.Content is { }
                    ? await ex.Response.Content.ReadAsStringAsync()
                    : ex.Message;

                string detail;
                try
                {
                    var pd = JsonSerializer.Deserialize<ProblemDetails>(body,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    detail = pd?.Detail ?? body;
                }
                catch
                {
                    detail = body;
                }

                if (status == HttpStatusCode.NotFound)
                {
                    return null;
                }
                else if (status == HttpStatusCode.Conflict)
                {
                    throw new InvalidOperationException(detail);
                }
                else
                {
                    throw new DaprServiceException((int)status, detail);
                }
            }
            catch (Exception ex)
            {
                await CleanupUploadedFiles(FileName, cancellationToken);
                _logger.LogError(ex, "Error updating event");
                throw;
            }
        }
        public async Task<string> DeleteEvent(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/v2/event/delete/{id}";

                return await _dapr.InvokeMethodAsync<string>(
                    HttpMethod.Delete,
                    ConstantValues.V2Content.ContentServiceAppId,
                    url,
                    cancellationToken
                );
            }
            catch (InvocationException ex)
            {               
                var status = ex.Response?.StatusCode ?? HttpStatusCode.BadGateway;
                string body = ex.Response?.Content is { }
                    ? await ex.Response.Content.ReadAsStringAsync()
                    : ex.Message;

                string detail;
                try
                {
                    var pd = JsonSerializer.Deserialize<ProblemDetails>(body,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    detail = pd?.Detail ?? body;
                }
                catch
                {
                    detail = body;
                }

                if (status == HttpStatusCode.NotFound)
                {
                    return null;
                }
                else if (status == HttpStatusCode.Conflict)
                {
                    throw new InvalidOperationException(detail);
                }
                else
                {
                    throw new DaprServiceException((int)status, detail);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event with Id {id}", id);
                throw;
            }
        }
        public async Task<string> CreateCategory(EventsCategory dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/api/v2/event/createcategory";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.V2Content.ContentServiceAppId, url);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    "application/json");
                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    string errorMessage;
                    try
                    {
                        var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                        errorMessage = problem?.Detail ?? "Unknown validation error.";
                    }
                    catch
                    {
                        errorMessage = errorJson;
                    }
                    throw new InvalidDataException(errorMessage);
                }
                response.EnsureSuccessStatusCode();
                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event category");
                throw;
            }
        }
        public async Task<List<EventsCategory>> GetAllCategories(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<List<EventsCategory>>(
                    HttpMethod.Get,
                    ConstantValues.V2Content.ContentServiceAppId,
                    "/api/v2/event/getallcategories",
                    cancellationToken
                ) ?? new List<EventsCategory>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all event categories.");
                throw;
            }
        }
        public async Task<EventsCategory?> GetEventCategoryById(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/v2/event/getcategorybyid/{id}";

                return await _dapr.InvokeMethodAsync<EventsCategory>(
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
        public async Task<PagedResponse<V2Events>> GetPagedEvents(GetPagedEventsRequest request, CancellationToken cancellationToken = default)
        {
            if ((request.Page ?? 0) <= 0 || (request.PerPage ?? 0) <= 0)
                throw new ArgumentException("Page and PerPage must be greater than zero.");

            var page = Math.Max(1, request.Page ?? 1);
            var perPage = Math.Clamp(request.PerPage ?? 12, 1, 100);

            var filters = new List<string> { "IsActive eq true" };

            // status
            if (request.Status.HasValue)
            {
                if (request.Status.Value == EventStatus.Expired)
                {
                    // when explicitly asking for expired, include both status+date guard to be safe
                    filters.Add($"(Status eq 'Expired' or {ExpiredClause()})");
                }
                else
                {
                    filters.Add($"Status eq '{request.Status.Value}'");
                    filters.Add(NotExpiredClause());
                }
            }
            else
            {
                // default: exclude expired
                filters.Add(NotExpiredClause());
            }

            if (request.CategoryId.HasValue)
                filters.Add($"CategoryId eq {request.CategoryId.Value}");

            if (request.LocationId is { Count: > 0 })
                filters.Add($"({OrList("LocationId", request.LocationId!)})");


            if (request.FreeOnly == true)
                filters.Add("(EventType eq 'FreeAcess' or EventType eq 'OpenRegistrations')");

            if (request.FromDate.HasValue || request.ToDate.HasValue)
            {
                if (request.FromDate.HasValue && request.ToDate.HasValue)
                    filters.Add($"EventSchedule/StartDate ge {request.FromDate.Value.ToDateTime(TimeOnly.MinValue):o} and EventSchedule/EndDate le {request.ToDate.Value.ToDateTime(TimeOnly.MinValue):o}");
                else if (request.FromDate.HasValue)
                    filters.Add($"EventSchedule/EndDate ge {request.FromDate.Value.ToDateTime(TimeOnly.MinValue):o}");
                else if (request.ToDate.HasValue)
                    filters.Add($"EventSchedule/StartDate le {request.ToDate.Value.ToDateTime(TimeOnly.MinValue):o}");
            }

            if (!string.IsNullOrWhiteSpace(request.FilterType))
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                var endOfWeek   = startOfWeek.AddDays(6);

                switch (request.FilterType.ToLowerInvariant())
                {
                    case "featured":
                        filters.Add("IsFeatured eq true");
                        break;
                    case "thisweek":
                        filters.Add($"EventSchedule/StartDate ge {startOfWeek.ToDateTime(TimeOnly.MinValue):o} and EventSchedule/StartDate le {endOfWeek.ToDateTime(TimeOnly.MinValue):o}");
                        break;
                    case "upcoming":
                        filters.Add($"EventSchedule/StartDate ge {DateTime.UtcNow.Date:o}");
                        break;
                }
            }

            string orderBy;
            if (!string.IsNullOrWhiteSpace(request.PriceSortOrder))
            {
                var dir = request.PriceSortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
                orderBy = $"Price {dir}, CreatedAt desc";
            }
            else if (request.FeaturedFirst == true)
            {
                var dir = string.Equals(request.SortOrder, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
                orderBy = $"IsFeatured desc, CreatedAt {dir}";
            }
            else
            {
                var dir = string.Equals(request.SortOrder, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";
                orderBy = $"CreatedAt {dir}";
            }

            var req = new RawSearchRequest
            {
                Filter = string.Join(" and ", filters),
                OrderBy = orderBy,
                Top = perPage,
                Skip = (page - 1) * perPage,
                Text = string.IsNullOrWhiteSpace(request.Search) ? "*" : request.Search!.Trim(),
                IncludeTotalCount = true
            };

            var res = await _search.SearchRawAsync<ContentEventsIndex>(
                ConstantValues.IndexNames.ContentEventsIndex, req, cancellationToken);

            var items = (res.Items ?? new List<ContentEventsIndex>()).Select(MapIndexToDto).ToList();

            return new PagedResponse<V2Events>
            {
                Page = page,
                PerPage = perPage,
                TotalCount = (int)res.TotalCount,
                Items = items,
                FeaturedCount = items.Count(e => e.IsFeatured),
                FeaturedInCurrentPage = items.Count(e => e.IsFeatured)
            };
        }

        public async Task<List<V2Slot>> GetAllEventSlot(CancellationToken cancellationToken = default)
        {
            try
            {
                var appId = ConstantValues.V2Content.ContentServiceAppId;
                var path = "/api/v2/event/slots";

                return await _dapr.InvokeMethodAsync<List<V2Slot>>(
               HttpMethod.Get,
               appId,
               path,
               cancellationToken
           ) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving writer tags from internal service");
                throw;
            }
        }
        public async Task<IEnumerable<V2Events>> GetExpiredEvents(CancellationToken cancellationToken = default)
        {
            var todayIso = DateTime.UtcNow.Date.ToString("o"); // midnight UTC
            var req = new RawSearchRequest
            {
                Filter = $"IsActive eq true and EventSchedule/EndDate lt {todayIso}",
                OrderBy = "EventSchedule/EndDate desc",
                Top = 1000,
                Skip = 0,
                Text = "*",
                IncludeTotalCount = false
            };

            var res = await _search.SearchRawAsync<ContentEventsIndex>(
                ConstantValues.IndexNames.ContentEventsIndex, req, cancellationToken);

            return (res.Items ?? new List<ContentEventsIndex>())
                .Select(MapIndexToDto)
                .ToList();
        }
        public async Task<string> ReorderEventSlotsAsync(EventSlotReorderRequest dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/api/v2/event/reorderslotsbyuserid";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.V2Content.ContentServiceAppId, url);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    "application/json");
                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    string errorMessage;
                    try
                    {
                        var problem = JsonSerializer.Deserialize<ProblemDetails>(errorJson);
                        errorMessage = problem?.Detail ?? "Unknown validation error.";
                    }
                    catch
                    {
                        errorMessage = errorJson;
                    }
                    throw new InvalidDataException(errorMessage);
                }
                response.EnsureSuccessStatusCode();
                var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonSerializer.Deserialize<string>(rawJson) ?? "Unknown response";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering event slots");
                throw;
            }
        }
        public async Task<List<V2Events>> GetEventsByStatus(EventStatus status, CancellationToken ct)
        {
            var baseFilter = status == EventStatus.Expired
                ? $"IsActive eq true and (Status eq 'Expired' or {ExpiredClause()})"
                : $"IsActive eq true and Status eq '{status}' and {NotExpiredClause()}";

            var req = new RawSearchRequest
            {
                Filter = baseFilter,
                OrderBy = "CreatedAt desc",
                Top = 500,
                Skip = 0,
                Text = "*",
                IncludeTotalCount = false
            };

            var res = await _search.SearchRawAsync<ContentEventsIndex>(
                ConstantValues.IndexNames.ContentEventsIndex, req, ct);

            return (res.Items ?? new List<ContentEventsIndex>()).Select(MapIndexToDto).ToList();
        }

        public async Task<List<V2Events>> GetEventStatus(EventStatus status, CancellationToken ct)
        {
            var baseFilter = status == EventStatus.Expired
                ? $"IsActive eq true and IsFeatured eq true and (Status eq 'Expired' or {ExpiredClause()})"
                : $"IsActive eq true and IsFeatured eq true and Status eq '{status}' and {NotExpiredClause()}";

            var req = new RawSearchRequest
            {
                Filter = baseFilter,
                OrderBy = "CreatedAt desc",
                Top = 500,
                Skip = 0,
                Text = "*",
                IncludeTotalCount = false
            };

            var res = await _search.SearchRawAsync<ContentEventsIndex>(
                ConstantValues.IndexNames.ContentEventsIndex, req, ct);

            return (res.Items ?? new List<ContentEventsIndex>()).Select(MapIndexToDto).ToList();
        }
        public async Task UpdateFeaturedEvent(UpdateFeaturedEvent dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/api/v2/event/updatefeaturedeventbyuserid";
                await _dapr.InvokeMethodAsync(
                    HttpMethod.Post,
                    ConstantValues.V2Content.ContentServiceAppId,
                    url,
                    dto,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating featured event");
                throw;
            }
        }
        public async Task<string> UnfeatureEvent(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/api/v2/event/unfeature/{id}";
                return await _dapr.InvokeMethodAsync<string>(
                    HttpMethod.Put,
                    ConstantValues.V2Content.ContentServiceAppId,
                    url,
                    cancellationToken
                );
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning(ex, "Event with ID {id} not found.", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unfeaturing event with ID {id}", id);
                throw;
            }
        }

        public Task<string> BulkMigrateEvents(List<V2Events> events, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> MigrateEvent(V2Events dto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
