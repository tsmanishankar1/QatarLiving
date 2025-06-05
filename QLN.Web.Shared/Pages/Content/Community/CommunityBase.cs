using System.Linq;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using MudBlazor;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.Model;
using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Model;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services.Interface;


namespace QLN.Web.Shared.Pages.Content.Community
{
    public class CommunityBase : ComponentBase
    {
        [Inject]
        public ISnackbar Snackbar { get; set; }
        [Inject]
        public IDialogService DialogService { get; set; }

        [Inject]
        public IBannerService _bannerService{ get; set; }

        [Inject] private ILogger<CommunityBase> Logger { get; set; }
        [Inject] private ICommunityService CommunityService { get; set; }
        [Inject] private IContentService _contentService { get; set; }

        [Inject] private INewsLetterSubscription NewsLetterSubscriptionService { get; set; }
        [Inject] private IAdService AdService { get; set; }
        protected string search = string.Empty;
        protected string sortOption = "Default";
        private string ApiSortValue => sortOption == "Default" ? null : sortOption;

        protected bool IsLoading { get; set; } = true;
        protected bool HasError { get; set; } = false;
        protected bool IsForumIdNotLoadedError { get; set; } = false;
        // Pagination
        protected int CurrentPage { get; set; } = 1;
        protected int PageSize { get; set; } = 10;
        protected int TotalPosts { get; set; } = 50;
        //protected int TotalPosts { get; set; }
        // Newsletter subscription
        protected NewsLetterSubscriptionModel SubscriptionModel { get; set; } = new();
        protected string SubscriptionStatusMessage = string.Empty;

        protected bool IsSubscribingToNewsletter { get; set; } = false;


        protected List<PostModel>? PostList { get; set; } = [];
        //Ad
        protected AdModel Ad { get; set; } = null;

        protected MudForm _form;

        //sort option
        protected string selectedCategory;
        protected string SelectedCategoryId { get; set; } = "Default";
        protected List<SelectOption> CategorySelectOptions { get; set; } = new List<SelectOption>
              {
                 new () { Id = "Default", Label = "Default" },
                 new () { Id = "desc", Label = "Date : Recent First" },
                 new() { Id = "asc", Label = "Date : Oldest First" },
            };
        protected string SelectedForumId;

        protected bool isLoadingBanners = true;
        protected List<BannerItem> DailyHeroBanners { get; set; } = new();
        protected List<BannerItem> CommunitySideBanners { get; set; } = new();

        [Parameter]
        [SupplyParameterFromQuery]
        public string? categoryId { get; set; }
        protected string _newComment;
        protected string newComment
        {
            get => _newComment;
            set
            {
                _newComment = value;
                StateHasChanged();
            }
        }
        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;

            try
            {
                await Task.WhenAll(
                    LoadPosts(),
                    LoadBanners(),
                    GetAdAsync()
                );
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnAfterRenderAsync");
            }
        }

        private async Task LoadPosts()
        {
            var (posts, totalCount) = await GetPostListAsync();
            PostList = posts;
            TotalPosts = totalCount;
        }

        protected override async Task OnParametersSetAsync()
        {
            Console.WriteLine($"Received categoryId: {categoryId}");
            if (!string.IsNullOrEmpty(categoryId))
            {
                SelectedForumId = categoryId;
            }
            else
            {
                SelectedForumId = null;
            }

            try
            {
                var (posts, totalCount) = await GetPostListAsync();
                PostList = posts;
                TotalPosts = totalCount;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to load posts");
                HasError = true;
            }
            finally
            {
                IsLoading = false;
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

        protected async Task HandleCategoryChanged(string forumId)
        {
            SelectedForumId = forumId;
            CurrentPage = 1;
            var (posts, totalCount) = await GetPostListAsync();
            PostList = posts;
            TotalPosts = totalCount;
        }

        protected async Task HandleSearchResults()
        {
            Console.WriteLine("Search completed.");
            await Task.CompletedTask;
        }

        protected async Task<(List<PostModel>? Posts, int TotalCount)> GetPostListAsync()
        {
            try
            {
                IsLoading = true;
                HasError = false;
                StateHasChanged();

                int? forumId = int.TryParse(SelectedForumId, out var parsedId) ? parsedId : null;
                Console.WriteLine("current page in GetPostListAsync", CurrentPage);

                var (dtoList, totalCount) = await CommunityService.GetPostsAsync(
             forumId: forumId,
             order: GetOrderFromSortOption(),
             page: CurrentPage,
             pageSize: PageSize
         );
                if (dtoList == null || !dtoList.Any())
                {
                    HasError = true;
                    return (null, 0);
                }


                var postModelList = dtoList.Select(dto => new PostModel
                {
                    Id = dto.nid,
                    Category = dto.forum_category,
                    Title = dto.title,
                    BodyPreview = dto.description,
                    Author = dto.user_name,
                    Time = DateTime.TryParse(dto.date_created, out var parsedDate) ? parsedDate : DateTime.MinValue,
                    LikeCount = 0,
                    CommentCount = 0,
                    isCommented = false,
                    ImageUrl = dto.image_url,
                    Slug = dto.slug,
                }).ToList();
                //TotalPosts = postModelList.TotalCount;

                return (postModelList, totalCount);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Get Community Post Async");
                HasError = true;
                return (null, 0);
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        private string GetOrderFromSortOption()
        {
            Logger.LogInformation($"SelectedCategoryId: {SelectedCategoryId}");
            Console.WriteLine($"SelectedCategoryId: {SelectedCategoryId}");
            //return SelectedCategoryId == "Default" ? null : SelectedCategoryId;

            return SelectedCategoryId switch
            {
                "desc" => "desc",
                "asc" => "asc",
                _ => "asc" // default to ascending
            };
        }

        protected async Task OnSortChanged(string newSortId)
        {
            Logger.LogInformation($"Sort changed to: {newSortId}");
            Console.WriteLine($"Sort changed to: {newSortId}");
            SelectedCategoryId = newSortId;
            CurrentPage = 1;
            var (posts, totalCount) = await GetPostListAsync();
            PostList = posts;
            TotalPosts = totalCount;
            StateHasChanged();
        }
        protected async Task HandlePageSizeChange(int newPageSize)
        {
            PageSize = newPageSize;
            CurrentPage = 1;
            var (posts, totalCount) = await GetPostListAsync();
            PostList = posts;
            TotalPosts = totalCount;
            StateHasChanged();
        }

        protected async Task HandlePageChange(int newPage)
        {
            CurrentPage = newPage;
            Console.WriteLine("current page", CurrentPage);
            Logger.LogInformation($"Page changed to: {CurrentPage}");

            var (posts, totalCount) = await GetPostListAsync();
            PostList = posts;
            TotalPosts = totalCount;
            StateHasChanged();
        }

        protected async Task SubscribeAsync()
        {
            IsSubscribingToNewsletter = true;
            await _form.Validate();

            if (_form.IsValid)
            {
                try
                {
                    var success = await NewsLetterSubscriptionService.SubscribeAsync(SubscriptionModel);
                    SubscriptionStatusMessage = success ? "Subscribed successfully!" : "Failed to subscribe.";
                    if (success)
                    {
                        Snackbar.Add("Subscription successful!", Severity.Success);
                    }
                    else
                    {
                        Snackbar.Add("Failed to subscribe. Please try again later.", Severity.Error);
                    }

                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Newsletter subscription failed.");
                    SubscriptionStatusMessage = "An error occurred while subscribing.";
                    Snackbar.Add($"Failed to subscribe: {ex.Message}", Severity.Error);

                }
                finally
                {
                    IsSubscribingToNewsletter = false;
                }
            }
            else
            {
                Snackbar.Add("Failed to subscribe. Please try again later.", Severity.Error);

            }
        }
        private async Task GetAdAsync()
        {
            var response = await AdService.GetAdDetail();
            Ad = response.FirstOrDefault();
        }

        protected bool _isCreatePostDialogOpen = false;

        protected void OpenCreatePostDialog()
        {
            _isCreatePostDialogOpen = true;
            StateHasChanged();
        }


        [Parameter] public EventCallback<string> OnCategoryChanged { get; set; }

        protected Task OpenDialogAsync()
        {
            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.Small,
                FullWidth = true,
                CloseOnEscapeKey = true,

            };
            return DialogService.ShowAsync<AddPostDialog>("Post Dialog", options);
        }

        protected async Task OnCategoryChange(string newId)
        {
            SelectedCategoryId = newId;
            CurrentPage = 1;
            var (posts, totalCount) = await GetPostListAsync();
            PostList = posts;
            TotalPosts = totalCount;
        }

        protected async Task LoadBanners()
        {
            isLoadingBanners = true;
            try
            {
                var banners = await _bannerService.GetBannerAsync();
                DailyHeroBanners = banners?.ContentCommunityHero ?? new();
                CommunitySideBanners = banners?.ContentCommunitySide ?? new();

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
        

    }
}
