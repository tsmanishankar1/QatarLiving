using Microsoft.AspNetCore.Components;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Services.Interface;
using System.Net.Http.Json;

namespace QLN.Web.Shared.Pages.Content.Events
{
    public class EventsComponentBase : LayoutComponentBase
    {
        [Inject] private IEventService _eventService { get; set; }
        protected List<ContentEvent> ListOfEvents { get; set; } = [];

        protected async override Task OnInitializedAsync()
        {
            try
            {
                ListOfEvents = await GetAllEvents() ?? [];
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message, "OnInitializedAsync");
            }
        }

        /// <summary>
        /// Gets Content Events Landing Page data.
        /// </summary>
        /// <returns>List of Content Events</returns>
        protected async Task<List<ContentEvent>> GetAllEvents()
        {
            try
            {
                var apiResponse = await _eventService.GetAllEventsAsync() ?? new HttpResponseMessage();

                if (apiResponse.IsSuccessStatusCode && apiResponse.Content != null)
                {
                    var response = await apiResponse.Content.ReadFromJsonAsync<List<ContentEvent>>();
                    return response ?? [];
                }

                return [];
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message, "OnInitializedAsync");
                return [];
            }
        }
    }
}
