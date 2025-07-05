using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Services.Base;
using System.Net;
using System.Text;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.MockServices
{
    public class EventsMockService : ServiceBase<EventsMockService>, IEventsService
    {
        List<EventDTO> mockEvents = new List<EventDTO>
{
    new EventDTO
    {
        Id = Guid.NewGuid(),
        EventTitle = "Doha Tech Expo 2025",
        CategoryId = 1,
        EventType = EventType.OpenRegistrations,
        Price = 50,
        Location = "Doha Exhibition Center",
        Venue = "Hall A",
        Longitude = "51.5310",
        Latitude = "25.2854",
        RedirectionLink = "https://techdoha2025.com",
        EventSchedule = new EventScheduleModel
        {
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            TimeSlotType = EventTimeType.GeneralTime,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(18, 0),
            TimeSlots = null
        },
        EventDescription = "An international technology expo showcasing innovation in the Gulf.",
        CoverImage = "https://example.com/images/tech.jpg",
        IsFeatured = true,
        FeaturedSlot = new Slot(), // Assuming default constructor is valid
        PublishedDate = DateTime.Now,
        Status = EventStatus.Published,
        Slug = "doha-tech-expo-2025",
        IsActive = true,
        CreatedBy = "admin",
        CreatedAt = DateTime.Now,
        UpdatedBy = null,
        UpdatedAt = null
    },
    new EventDTO
    {
        Id = Guid.NewGuid(),
        EventTitle = "Qatar Art Fair",
        CategoryId = 2,
        EventType = EventType.FreeAcess,
        Price = null,
        Location = "Katara Cultural Village",
        Venue = "Gallery 2",
        Longitude = "51.5382",
        Latitude = "25.3240",
        RedirectionLink = null,
        EventSchedule = new EventScheduleModel
        {
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(12)),
            TimeSlotType = EventTimeType.PerDayTime,
            TimeSlots = new List<TimeSlotModel>
            {
                new TimeSlotModel { DayOfWeek = DayOfWeek.Friday, Time = "3:00 PM - 7:00 PM" },
                new TimeSlotModel { DayOfWeek = DayOfWeek.Saturday, Time = "1:00 PM - 6:00 PM" }
            }
        },
        EventDescription = "An open exhibition featuring emerging local and international artists.",
        CoverImage = "https://example.com/images/art.jpg",
        IsFeatured = false,
        FeaturedSlot = new Slot(),
        PublishedDate = DateTime.Now,
        Status = EventStatus.Published,
        Slug = "qatar-art-fair",
        IsActive = true,
        CreatedBy = "editor",
        CreatedAt = DateTime.Now,
        UpdatedBy = "editor",
        UpdatedAt = DateTime.Now
    }
};
        public EventsMockService(HttpClient httpClientDI, ILogger<EventsMockService> Logger)
           : base(httpClientDI, Logger)
        {

        }
        public Task<HttpResponseMessage> CreateEvent(EventDTO events)
        {
            throw new NotImplementedException();
        }
        public Task<HttpResponseMessage> GetEventCategories()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                // Content = new StringContent(JsonSerializer.Serialize(newsCateg), Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
        public Task<HttpResponseMessage> GetEventLocations()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                // Content = new StringContent(JsonSerializer.Serialize(newsCateg), Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
        public Task<HttpResponseMessage> GetAllEvents()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(mockEvents), Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
        public Task<HttpResponseMessage> DeleteEvent(string eventId)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                // Content = new StringContent(JsonSerializer.Serialize(mockEvents), Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
        public Task<HttpResponseMessage> GetFeaturedEvents()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                // Content = new StringContent(JsonSerializer.Serialize(mockEvents), Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
        public Task<HttpResponseMessage> UpdateFeaturedEvents(EventDTO events)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                // Content = new StringContent(JsonSerializer.Serialize(mockEvents), Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
        public Task<HttpResponseMessage> GetEventById(Guid eventId)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                // Content = new StringContent(JsonSerializer.Serialize(mockEvents), Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
        public Task<HttpResponseMessage> UpdateEvents(EventDTO events)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                // Content = new StringContent(JsonSerializer.Serialize(mockEvents), Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }

        public async Task<HttpResponseMessage> GetEventsByPagination(
       int page,
       int perPage,
       string? search = null,
       int? categoryId = null,
       string? sortOrder = null,
       string? fromDate = null,
       string? toDate = null,
       string? filterType = null,
       string? location = null,
       bool? freeOnly = null,
       bool? featuredFirst = null,
        int? status = null)
        {
            try
            {
                var queryParams = new Dictionary<string, string?>
                {
                    ["page"] = page.ToString(),
                    ["perPage"] = perPage.ToString(),
                    ["search"] = search,
                    ["categoryId"] = categoryId?.ToString(),
                    ["sortOrder"] = sortOrder,
                    ["fromDate"] = fromDate,
                    ["toDate"] = toDate,
                    ["filterType"] = filterType,
                    ["location"] = location,
                    ["freeOnly"] = freeOnly?.ToString()?.ToLower(),
                    ["featuredFirst"] = featuredFirst?.ToString()?.ToLower(),
                    ["status"] = status?.ToString()
                };

                var queryString = string.Join("&",
                    queryParams
                        .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
                        .Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value!)}")
                );

                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"api/v2/event/getpaginatedevents?{queryString}"
                );

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(mockEvents), Encoding.UTF8, "application/json")
                };

                return response;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetEventsByPagination");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
public async Task<HttpResponseMessage> ReorderFeaturedSlots(int fromSlot, int toSlot, string userId)
{
    try
    {
        var payload = new
        {
            fromSlot = fromSlot,
            toSlot = toSlot,
            userId = userId
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });

        var request = new HttpRequestMessage(HttpMethod.Post, "api/v2/event/reorderslots")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

      
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(mockEvents), Encoding.UTF8, "application/json")
                };

                return response;
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "ReorderFeaturedSlots");
        return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
    }
}

    }

}
