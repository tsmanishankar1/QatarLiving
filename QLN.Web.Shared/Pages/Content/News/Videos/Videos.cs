using Microsoft.AspNetCore.Components;
using QLN.Web.Shared.Models;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Services.Interface;
using Microsoft.JSInterop;
using QLN.Web.Shared.Components.ViewToggleButtons;
using QLN.Web.Shared.Model;
public class VideosBase : ComponentBase
{
    protected bool isLoadingBanners = true;
    public ElementReference videoDisplaySectionRef;
    [Inject] IJSRuntime JSRuntime { get; set; }
    protected List<BannerItem> DailyTakeOverBanners = new();
    [Inject] private ISimpleMemoryCache _simpleCacheService { get; set; }
    public ContentVideo SelectedVideo { get; set; }
    protected List<BannerItem> DailyHeroBanners { get; set; } = new();
    protected List<ContentVideo>? VideoList => NewsContent?.News?.WatchOnQatarLiving?.Items;

    protected GenericNewsPageResponse? NewsContent { get; set; }
    protected List<ContentVideo>? contentVideos = new List<ContentVideo>
{
    new ContentVideo
    {
        Nid = "1",
        DateCreated = "2025-06-01",
        ImageUrl = "https://files.qatarliving.com/Indonesia.jpg",
        VideoUrl = "https://www.youtube.com/watch?v=x33XYlFnVKw",
        UserName = "User1",
        Title = "Video Title 1"
    },
    new ContentVideo
    {
        Nid = "2",
        DateCreated = "2025-06-02",
        ImageUrl = "https://files.qatarliving.com/Indonesia.jpg",
        VideoUrl = "https://www.youtube.com/watch?v=x33XYlFnVKw",
        UserName = "User2",
        Title = "Second Indonesian Education Expo 2025 Qatar"
    },
    new ContentVideo
    {
        Nid = "3",
        DateCreated = "2025-06-03",
        ImageUrl = "https://files.qatarliving.com/Indonesia.jpg",
        VideoUrl = "https://www.youtube.com/watch?v=x33XYlFnVKw",
        UserName = "User3",
        Title = "Second Indonesian Education Expo 2025 Qatar"
    },
    new ContentVideo
    {
        Nid = "4",
        DateCreated = "2025-06-04",
        ImageUrl = "https://files.qatarliving.com/Indonesia.jpg",
        VideoUrl = "https://www.youtube.com/watch?v=x33XYlFnVKw",
        UserName = "User4",
        Title = "Second Indonesian Education Expo 2025 Qatar"
    },
    new ContentVideo
    {
        Nid = "5",
        DateCreated = "2025-06-05",
        ImageUrl = "https://files.qatarliving.com/Indonesia.jpg",
        VideoUrl = "https://www.youtube.com/watch?v=x33XYlFnVKw",
        UserName = "User5",
        Title = "Second Indonesian Education Expo 2025 Qatar"
    },
    new ContentVideo
    {
        Nid = "6",
        DateCreated = "2025-06-06",
        ImageUrl = "https://files.qatarliving.com/Indonesia.jpg",
        VideoUrl = "https://www.youtube.com/watch?v=GKvEuA80FAE",
        UserName = "User6",
        Title = "Second Indonesian Education Expo 2025 Qatar"
    }
};



    protected async override Task OnInitializedAsync()
    {
        await LoadBanners();
        NewsContent = await _simpleCacheService.GetCurrentNews("Qatar") ?? new();
    }
    protected async Task LoadBanners()
    {
        isLoadingBanners = true;
        try
        {
            var banners = await _simpleCacheService.GetBannerAsync();
            DailyHeroBanners = banners?.ContentVideosHero ?? new List<BannerItem>();
            DailyTakeOverBanners = banners?.ContentVideosTakeover ?? new List<BannerItem>();
        }
        finally
        {
            isLoadingBanners = false;
        }
    }
    protected async Task PlayVideo(ContentVideo video)
    {
        SelectedVideo = video;
        StateHasChanged();
        await JSRuntime.InvokeVoidAsync("scrollToElementById", "video-cards-section");
    }
    
}