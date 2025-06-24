using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using QLN.Web.Shared.Services.Interface;
using QLN.Common.DTO_s;
using System.Net.Http.Json;

namespace QLN.Web.Shared.Pages.Classifieds.Collectibles
{
    public class CollectiblesDetailsComponentBase : ComponentBase
    {
        [Inject] private IClassifiedsServices _classifiedsService { get; set; } = default!;
        [Inject] protected ILogger<CollectiblesDetailsComponentBase> Logger { get; set; }

        [Parameter] public string Id { get; set; }

        protected string viewAllUrl => $"/classifieds";
        protected bool IsLoading { get; set; } = true;  
        protected ClassifiedsIndex? CollectiblesDetails { get; set; } = null;
        protected List<ClassifiedsIndex> CollectiblesDetailsSimler { get; set; } = new();
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

                var response = await _classifiedsService.GetClassifiedWithSimilarAsync(Id, 4); // 4 is page size

                if (response != null && response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<ClassifiedWithSimilarResponse>();
                    if (data != null)
                    {
                        CollectiblesDetails = data.Detail;
                        CollectiblesDetailsSimler = data.Similar ?? new();
                    }
                }
                else
                {
                    Logger.LogWarning($"Failed to fetch classified details with similar items for Id: {Id}. StatusCode: {response?.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Exception in GetClassifiedWithSimilarAsync for Id: {Id}");
            }
            finally
            {
                IsLoading = false;
            }
        }


    }
}
