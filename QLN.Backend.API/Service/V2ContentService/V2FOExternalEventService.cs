using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Common.Infrastructure.IService.ISearchService;
using QLN.Common.Infrastructure.Utilities;
using System.Net;

namespace QLN.Backend.API.Service.V2ContentService
{
    public class V2FOExternalEventService : IV2FOEventService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<V2ExternalEventService> _logger;
        private readonly ISearchService _search;
        public V2FOExternalEventService(DaprClient dapr, ILogger<V2ExternalEventService> logger, ISearchService search)
        {
            _dapr = dapr;
            _logger = logger;
            _search = search;
        }
        public async Task<V2Events> GetEventBySlug(string slug, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(slug))
                    return null;

                var req = new CommonSearchRequest
                {
                    Text = "*",
                    PageNumber = 1,
                    PageSize = 1,
                    Filters = new Dictionary<string, object>
                    {
                        ["Slug"] = slug
                    }
                };

                var res = await _search.SearchAsync(ConstantValues.IndexNames.ContentEventsIndex, req);
                var doc = res.ContentEventsItems?.FirstOrDefault();
                if (doc is null)
                {
                    _logger.LogWarning("Event with Slug '{Slug}' not found in index.", slug);
                    return null;
                }

                return MapIndexToDto(doc);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching event by slug '{Slug}' from Azure Search", slug);
                throw;
            }
        }

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
    }
}
