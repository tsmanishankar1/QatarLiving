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

    protected IEnumerable<BackofficemasterIndex>? HeroBannerList { get; set; }
    protected IEnumerable<BackofficemasterIndex>? FeaturedItemsList { get; set; }
    protected IEnumerable<BackofficemasterIndex>? FeaturedServicesList { get; set; }
    protected IEnumerable<BackofficemasterIndex>? FeaturedCategoriesList { get; set; }
    protected IEnumerable<BackofficemasterIndex>? ReadyToGrowList { get; set; }
    protected IEnumerable<BackofficemasterIndex>? FeaturedStoresList { get; set; }
    protected IEnumerable<BackofficemasterIndex>? CategoriesList { get; set; }
    protected IEnumerable<BackofficemasterIndex>? SeasonalPicksList { get; set; }
    protected IEnumerable<BackofficemasterIndex>? SocialPostDetailList { get; set; }
    protected IEnumerable<BackofficemasterIndex>? SocialLinksList { get; set; }
    protected IEnumerable<BackofficemasterIndex>? SocialMediaVideosList { get; set; }
    protected IEnumerable<BackofficemasterIndex>? FaqItemsList { get; set; }

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
                    HeroBannerList = landingData.HeroBanner ?? new List<BackofficemasterIndex>();
                    FeaturedItemsList = landingData.FeaturedItems ?? new List<BackofficemasterIndex>();
                    FeaturedServicesList = landingData.FeaturedServices ?? new List<BackofficemasterIndex>();
                    FeaturedCategoriesList = landingData.FeaturedCategories ?? new List<BackofficemasterIndex>();
                    ReadyToGrowList = landingData.ReadyToGrow ?? new List<BackofficemasterIndex>();
                    FeaturedStoresList = landingData.FeaturedStores ?? new List<BackofficemasterIndex>();
                    CategoriesList = landingData.Categories ?? new List<BackofficemasterIndex>();
                    SeasonalPicksList = landingData.SeasonalPicks ?? new List<BackofficemasterIndex>();
                    SocialPostDetailList = landingData.SocialPostDetail ?? new List<BackofficemasterIndex>();
                    SocialLinksList = landingData.SocialLinks ?? new List<BackofficemasterIndex>();
                    SocialMediaVideosList = landingData.SocialMediaVideos ?? new List<BackofficemasterIndex>();
                    FaqItemsList = landingData.FaqItems ?? new List<BackofficemasterIndex>();
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
