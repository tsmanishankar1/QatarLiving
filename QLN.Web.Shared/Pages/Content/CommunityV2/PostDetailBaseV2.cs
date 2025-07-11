 using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Model;
using QLN.Web.Shared.Services;
using QLN.Web.Shared.Services.Interface;
using System.Net.Http.Json;


namespace QLN.Web.Shared.Pages.Content.CommunityV2
{
    public class PostDetailBaseV2 : ComponentBase
    {

        [Inject] protected ICommunityService CommunityService { get; set; } = default!;
        [Inject] protected ILogger<PostDetailBaseV2> Logger { get; set; } = default!;
        [Inject] private IContentService _contentService { get; set; }
        [Inject] private ISimpleMemoryCache _simpleCacheService { get; set; }
        [Inject] protected IOptions<NavigationPath> options { get; set; }

        protected List<QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem> breadcrumbItems = new();
        protected QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem? postBreadcrumbItem;
        protected QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem? postBreadcrumbCategory;

        protected string search = string.Empty;
        protected string sortOption = "Popular";
        [Parameter]
        public string slug { get; set; }

        protected bool IsLoading { get; set; } = true;
        protected bool HasError { get; set; } = false;
        protected PostModel SelectedPost { get; private set; }

        protected PostModel? post;

        protected bool isLoadingBanners = true;
        protected List<BannerItem> ContentCommunityPostHero { get; set; } = new();
        protected List<BannerItem> CommunitySideBanners { get; set; } = new();


        protected override async Task OnParametersSetAsync()
        {
            await LoadBanners();

            // Update breadcrumb for current slug
            postBreadcrumbItem = new()
            {
                Label =  "Not Found",
                Url = $"/content/v2/community/post/detail/{slug}",
                IsLast = true
            };
            postBreadcrumbCategory = new()
            {
                Label = "Not Found",
                Url =options.Value.ContentCommunity
            };

            breadcrumbItems = new List<QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem>
            {
                new() { Label = "Community", Url = options.Value.ContentCommunity}, // leaving this as it is a V2 component
               postBreadcrumbCategory,
                postBreadcrumbItem
            };

            await LoadPostAsync();
        }

     
        private async Task LoadPostAsync()
        {
            try
            {
                SelectedPost = null;
                IsLoading = true;
                HasError = false;
                StateHasChanged();

                var fetched = await CommunityService.GetCommunityPostDetail(slug);

                if (fetched != null)
                {
                    SelectedPost = new PostModel
                    {
                        Id = fetched.Id.ToString(),
                        Category = fetched.Category,
                        CategoryId = fetched.CategoryId,
                        Title = fetched.Title,
                        BodyPreview = fetched.Description,
                        Author = fetched.UserName ?? "Unknown User",
                        Time = fetched.DateCreated,
                        LikeCount = fetched.LikeCount,
                        CommentCount = fetched.CommentCount,
                        ImageUrl = fetched.ImageUrl,
                        Slug = fetched.Slug,
                        
                    };

                    if (postBreadcrumbItem is not null)
                    {
                        postBreadcrumbItem.Label = SelectedPost.Title;
                        StateHasChanged();
                    }
                    if (postBreadcrumbCategory is not null)
                    {
                        postBreadcrumbCategory.Label = SelectedPost.Category;
                        postBreadcrumbCategory.Url = $"/content/community/v2?categoryId={SelectedPost.CategoryId}"; // leaving this as it is a V2 component
                        StateHasChanged();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to load post with slug: {Slug}", slug);
                HasError = true;
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }
        protected async Task LoadBanners()
        {
            isLoadingBanners = true;
            try
            {
                var banners = await _simpleCacheService.GetBannerAsync();
                ContentCommunityPostHero = banners?.ContentCommunityPostHero ?? new();
                CommunitySideBanners = banners?.ContentCommunityPostSide ?? new (); 

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading banners: {ex.Message}");
            }
            finally
            {
                isLoadingBanners = false;
            }
        }
        private async Task<BannerResponse?> FetchBannerData()
        {
            try
            {
                var response = await _contentService.GetBannerAsync();
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return await response.Content.ReadFromJsonAsync<BannerResponse>();
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FetchBannerData error: {ex.Message}");
                return null;
            }
        }
    }
}
