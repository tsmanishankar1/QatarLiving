using Microsoft.AspNetCore.Components;
using QLN.Web.Shared.Components.BreadCrumb;
using Microsoft.Extensions.Logging;
using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Services.Interface;
using QLN.Common.Infrastructure.DTO_s; // ✅ Import your DTO
using System.Net.Http.Json;

namespace QLN.Web.Shared.Pages.Content.Events.EventDetails
{
    public class EventDetailsBase : ComponentBase
    {
        [Inject] protected IEventService EventService { get; set; }
        [Inject] protected ILogger<EventDetailsBase> Logger { get; set; }

        [Parameter] public string slug { get; set; }

        protected bool isLoading { get; set; } = true;

        // ✅ Use the correct DTO type here
        protected ContentEvent eventDetails { get; set; }

        protected List<string> carouselImages = new()
        {
            "/images/banner_image.svg",
            "/images/banner_image.svg",
            "/images/banner_image.svg"
        };

        protected List<BreadcrumbItem> breadcrumbItems = new();

        protected override async Task OnInitializedAsync()
        {
          
            try
            {
                isLoading = true;
                var response = await EventService.GetEventBySlugAsync(slug);
                if (response != null && response.IsSuccessStatusCode)
                {
                    // ✅ Deserialize directly into ContentEvent
                    eventDetails = await response.Content.ReadFromJsonAsync<ContentEvent>();
                    breadcrumbItems = new()
                    {
                        new() { Label = "Events", Url = "/content/events" },
                        new()
                        {
                            Label = eventDetails?.Title ?? "Event Detail",
                            Url = $"/events/details/{slug}",
                            IsLast = true
                        }
                     };
                }
                else
                {
                    Logger.LogWarning($"Failed to fetch event details for slug: {slug}. StatusCode: {response?.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Exception in GetEventBySlugAsync for slug: {slug}");
            }
            finally
            {
                isLoading = false;
            }
        }
    }
}
