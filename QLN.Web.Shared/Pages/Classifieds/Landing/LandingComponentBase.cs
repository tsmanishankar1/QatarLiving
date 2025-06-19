using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Web.Shared.Services.Interface;
using System.Net.Http.Json;

public class LandingComponentBase : ComponentBase
{
  [Inject] private IClassifiedsServices _classifiedsService { get; set; }
    protected bool IsLoading { get; set; } = true;
    protected string? ErrorMessage { get; set; }

    protected List<string> CarouselImages { get; set; } = new()
    {
        "/qln-images/banner_image.svg",
        "/qln-images/banner_image.svg",
        "/qln-images/banner_image.svg"
    };

    protected IEnumerable<LandingBackOfficeIndex>? HeroBannerList { get; set; }
    protected IEnumerable<LandingFeaturedItemDto>? FeaturedItemsList { get; set; }
    protected IEnumerable<LandingFeaturedItemDto>? FeaturedServicesList { get; set; }
    protected IEnumerable<LandingBackOfficeIndex>? FeaturedCategoriesList { get; set; }
    protected IEnumerable<LandingBackOfficeIndex> ReadyToGrowList { get; set; } = new List<LandingBackOfficeIndex>();
    protected IEnumerable<LandingBackOfficeIndex>? FeaturedStoresList { get; set; } = new List<LandingBackOfficeIndex>();
    protected IEnumerable<LandingBackOfficeIndex>? CategoriesList { get; set; }
    protected IEnumerable<LandingBackOfficeIndex> SeasonalPicksList { get; set; } = new List<LandingBackOfficeIndex>();
    protected IEnumerable<LandingBackOfficeIndex>? SocialPostDetailList { get; set; }
    protected IEnumerable<LandingBackOfficeIndex>? SocialLinksList { get; set; }
    protected IEnumerable<LandingBackOfficeIndex>? SocialMediaVideosList { get; set; }
    protected IEnumerable<LandingBackOfficeIndex> FaqItemsList { get; set; }  = new List<LandingBackOfficeIndex>();
protected IEnumerable<PopularSearchDto> PopularSearchesList { get; set; } = new List<PopularSearchDto>();

  protected override async Task OnInitializedAsync()
{
    try
    {
        var response = await _classifiedsService.GetClassifiedsLPAsync();

        if (response != null && response.IsSuccessStatusCode)
        {
            var landingData = await response.Content.ReadFromJsonAsync<LandingPageDto>();

                if (landingData != null)
                {
                    HeroBannerList = landingData.HeroBanner ?? new List<LandingBackOfficeIndex>();
                    FeaturedItemsList = (landingData.FeaturedItems ?? Enumerable.Empty<LandingFeaturedItemDto>()).ToList();
                    FeaturedServicesList = (landingData.FeaturedServices ?? Enumerable.Empty<LandingFeaturedItemDto>()).ToList();
                    FeaturedCategoriesList = landingData.FeaturedCategories ?? new List<LandingBackOfficeIndex>();
                    ReadyToGrowList = landingData.ReadyToGrow ?? new List<LandingBackOfficeIndex>();
                    FeaturedStoresList = landingData.FeaturedStores ?? new List<LandingBackOfficeIndex>();
                    CategoriesList = landingData.Categories ?? new List<LandingBackOfficeIndex>();
                    SeasonalPicksList = landingData.SeasonalPicks ?? new List<LandingBackOfficeIndex>();
                    SocialPostDetailList = landingData.SocialPostDetail ?? new List<LandingBackOfficeIndex>();
                    SocialLinksList = landingData.SocialLinks ?? new List<LandingBackOfficeIndex>();
                    SocialMediaVideosList = landingData.SocialMediaVideos ?? new List<LandingBackOfficeIndex>();
                    FaqItemsList = landingData.FaqItems ?? new List<LandingBackOfficeIndex>();
                    PopularSearchesList = landingData.PopularSearches ?? Enumerable.Empty<PopularSearchDto>();

            }
        }
        else
        {
            ErrorMessage = "Failed to fetch landing page data.";
        }
    }
    catch (Exception ex)
    {
        ErrorMessage = "An error occurred while loading the landing page.";
        Console.WriteLine($"Error: {ex.Message}");
    }
    finally
    {
        IsLoading = false;
    }
}

}
