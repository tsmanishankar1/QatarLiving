using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using QLN.Web.Shared.Services.Interface;
using QLN.Common.DTO_s;
using System.Net.Http.Json;

namespace QLN.Web.Shared.Pages.Classifieds.Items
{
    public class ItemsDetailsComponentBase : ComponentBase
    {
        [Inject] private IClassifiedsServices _classifiedsService { get; set; } = default!;
        [Inject] protected ILogger<ItemsDetailsComponentBase> Logger { get; set; }

        [Parameter] public string Id { get; set; }

        protected string viewAllUrl => $"/classifieds";
        protected bool IsLoading { get; set; } = true;

        protected ClassifiedsIndex ItemsDetails { get; set; } = new();
        protected List<string> carouselImages = new()
        {
            "/images/banner_image.svg",
            "/images/banner_image.svg",
            "/images/banner_image.svg"
        };


        protected override async Task OnInitializedAsync()
        {
            try
            {
                IsLoading = true;

                var response = await _classifiedsService.GetClassifiedsByIdAsync(Id);

                if (response != null && response.IsSuccessStatusCode)
                {
                    ItemsDetails = await response.Content.ReadFromJsonAsync<ClassifiedsIndex>();
                }
                else
                {
                    Logger.LogWarning($"Failed to fetch event details for slug: {Id}. StatusCode: {response?.StatusCode}");
                }

            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Exception in GetEventBySlugAsync for slug: {Id}");
            }
            finally
            {
                IsLoading = false;
            }
        }

    }
}
