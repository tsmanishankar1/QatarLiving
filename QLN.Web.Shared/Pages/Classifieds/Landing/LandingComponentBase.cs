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

    protected IEnumerable<LandingBackOfficeIndex>? CategoriesList { get; set; }
    protected IEnumerable<LandingBackOfficeIndex>? SeasonalPicksList { get; set; }
    protected IEnumerable<LandingBackOfficeIndex>? SocialPostDetailList { get; set; }
    protected IEnumerable<LandingBackOfficeIndex>? SocialLinksList { get; set; }
    protected IEnumerable<LandingBackOfficeIndex>? SocialMediaVideosList { get; set; }
    protected IEnumerable<LandingBackOfficeIndex>? FaqItemsList { get; set; }
  protected List<LandingBackOfficeIndex> FeaturedStoresList { get; set; } = new()
    {
        new() { Title = "AIRBNB", ImageUrl = "qln-images/stores/vector.svg", ListingCount = 2152 },
        new() { Title = "Starlink", ImageUrl = "qln-images/stores/starlink.svg", ListingCount = 2152 },
        new() { Title = "MICROSOFT", ImageUrl = "qln-images/stores/microsoft.svg", ListingCount = 2152 },
        new() { Title = "Lulu Hypermarket", ImageUrl = "qln-images/stores/hypermarket.svg", ListingCount = 2152 },
        new() { Title = "City Hypermarket", ImageUrl = "qln-images/stores/hypermarket_city.svg", ListingCount = 1845 },
        new() { Title = "Al Meera", ImageUrl = "qln-images/stores/meera.svg", ListingCount = 3021 },
        new() { Title = "MICROSOFT", ImageUrl = "qln-images/stores/microsoft.svg", ListingCount = 2152 },
        new() { Title = "Lulu Hypermarket", ImageUrl = "qln-images/stores/hypermarket.svg", ListingCount = 2152 },
        new() { Title = "AIRBNB", ImageUrl = "qln-images/stores/vector.svg", ListingCount = 2152 },
        new() { Title = "Starlink", ImageUrl = "qln-images/stores/starlink.svg", ListingCount = 2152 },
        new() { Title = "Al Meera", ImageUrl = "qln-images/stores/meera.svg", ListingCount = 3021 },
        new() { Title = "MICROSOFT", ImageUrl = "qln-images/stores/microsoft.svg", ListingCount = 2152 }
    };
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

                CategoriesList = landingData.Categories ?? new List<LandingBackOfficeIndex>();
                SeasonalPicksList = landingData.SeasonalPicks ?? new List<LandingBackOfficeIndex>();
                SocialPostDetailList = landingData.SocialPostDetail ?? new List<LandingBackOfficeIndex>();
                SocialLinksList = landingData.SocialLinks ?? new List<LandingBackOfficeIndex>();
                SocialMediaVideosList = landingData.SocialMediaVideos ?? new List<LandingBackOfficeIndex>();
                FaqItemsList = landingData.FaqItems ?? new List<LandingBackOfficeIndex>();
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
