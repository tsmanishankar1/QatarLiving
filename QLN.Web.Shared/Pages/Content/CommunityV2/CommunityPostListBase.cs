using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Model;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services.Interface;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace QLN.Web.Shared.Pages.Content.CommunityV2
{
    public class CommunityPostListBase :ComponentBase
    {
        [Inject]
        public ISnackbar Snackbar { get; set; }
        [Inject]
        public IDialogService DialogService { get; set; }

        [Inject]
        public ISimpleMemoryCache _simpleCacheService { get; set; }

        [Inject] private ILogger<CommunityPostListBase> Logger { get; set; }
        [Inject] private ICommunityService CommunityService { get; set; }
        [Inject] private IContentService _contentService { get; set; }
        [Inject] protected NavigationManager Navigation { get; set; }
        [Inject] INewsLetterSubscription newsLetterSubscriptionService { get; set; }
        [Inject] protected IJSRuntime JS { get; set; }
        [Inject] public HttpClient Http { get; set; }
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
        // Newsletter subscription
        protected NewsLetterSubscriptionModel SubscriptionModel { get; set; } = new();
        protected string SubscriptionStatusMessage = string.Empty;

        protected bool IsSubscribingToNewsletter { get; set; } = false;


        protected List<PostModel>? PostList { get; set; } = [];
   
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
 
        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;

            try
            {
                await Task.WhenAll(
                    LoadPosts(),
                    LoadBanners()
                //GetAdAsync()
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
            StateHasChanged();
        }

        protected override async Task OnParametersSetAsync()
        {
            if (!string.IsNullOrEmpty(categoryId))
            {
                SelectedForumId = categoryId;
                await HandleCategoryChanged(categoryId);

            }
            else
            {
                SelectedForumId = null;
            }

        }
        protected async Task HandleSearchChanged(string searchText)
        {
            Console.WriteLine($"Received searchText from child: {searchText}");
            search = searchText;
            CurrentPage = 1;
            await GetPostListAsync();
        }
        protected async Task HandleCategoryChanged(string forumId)
        {
            SelectedForumId = forumId;
            CurrentPage = 1;
            await LoadPosts();
        }

        protected async Task HandleSearchResults()
        {
            Console.WriteLine("Search completed.");
            await Task.CompletedTask;
        }

        protected async Task<(List<PostModel>? Posts, int TotalCount)> GetPostListAsync()
        {
            Console.WriteLine($"Search passed to service: {search}");

            try
            {
                IsLoading = true;
                HasError = false;

                int? forumId = int.TryParse(SelectedForumId, out var parsedId) ? parsedId : null;

                var (dtoList, totalCount) = await CommunityService.GetCommunityPostList(
                    categoryId: forumId,
                    search: search, 
                    page: CurrentPage,
                    pageSize: PageSize,
                    sortDirection: GetOrderFromSortOption()
                );

                if (dtoList == null || !dtoList.Any())
                {
                    HasError = true;
                    return (null, 0);
                }

                var postModelList = dtoList.Select(dto => new PostModel
                {
                    Id = dto.Id.ToString(),
                    Category = dto.Category,
                    Title = dto.Title,
                    BodyPreview = dto.Description,
                    Author = dto.UserName,
                    Time = dto.DateCreated,
                    LikeCount = dto.LikeCount,
                    CommentCount = dto.CommentCount,
                    isCommented = false,
                    ImageUrl = dto.ImageUrl,
                    Slug = dto.Slug,
                    //isCommented = dto.CommentedUserIds?.Contains(currentUserId) ?? false,
                    //IsLiked = dto.LikedUserIds?.Contains(currentUserId) ?? false,
                }).ToList();

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
            }
        }


        private string GetOrderFromSortOption()
        {
            Logger.LogInformation($"SelectedCategoryId: {SelectedCategoryId}");
           
            return SelectedCategoryId switch
            {
                "desc" => "desc",
                "asc" => "asc",
                _ => "desc" 
            };
        }

        protected async Task OnSortChanged(string newSortId)
        {
            Logger.LogInformation($"Sort changed to: {newSortId}");
            SelectedCategoryId = newSortId;
            CurrentPage = 1;
            await LoadPosts();
            StateHasChanged();
        }
        protected async Task HandlePageSizeChange(int newPageSize)
        {
            PageSize = newPageSize;
            CurrentPage = 1;
            await LoadPosts();
            StateHasChanged();
        }

        protected async Task HandlePageChange(int newPage)
        {
            CurrentPage = newPage;
            Console.WriteLine("current page", CurrentPage);
            Logger.LogInformation($"Page changed to: {CurrentPage}");

            await LoadPosts();
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
            var (PostList, TotalPosts) = await GetPostListAsync(); 
        }

        protected async Task LoadBanners()
        {
            isLoadingBanners = true;
            try
            {
             

                var banners = await _simpleCacheService.GetBannerAsync();
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
        protected void NavigateToCreatePost()
        {
            Navigation.NavigateTo("qln/community/post/create", forceLoad: true);
        }


        protected async Task SubscribeAsync()
        {
            if (string.IsNullOrWhiteSpace(SubscriptionModel?.Email))
            {
                Snackbar.Add("Email is required.", Severity.Warning);
                return;
            }

            var emailAttribute = new EmailAddressAttribute();
            if (!emailAttribute.IsValid(SubscriptionModel.Email))
            {
                Snackbar.Add("Please enter a valid email address.", Severity.Warning);
                return;
            }
            IsSubscribingToNewsletter = true;

            try
            {
                string baseUrl = "https://qatarliving.us9.list-manage.com/subscribe/post-json";
                string u = "3ab0436d22c64716e67a03f64";
                string id = "94198fac96";
                string email = SubscriptionModel.Email;
                string callback = $"jQuery{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
                string botField = "";
                string subscribe = "Subscribe";
                string cacheBuster = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

                var query = HttpUtility.ParseQueryString(string.Empty);
                query["u"] = u;
                query["id"] = id;
                query["c"] = callback;
                query["EMAIL"] = email;
                query["b_3ab0436d22c64716e67a03f64_94198fac96"] = botField;
                query["subscribe"] = subscribe;
                query["_"] = cacheBuster;

                string url = $"{baseUrl}?{query}";

                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("User-Agent", "Mozilla/5.0");
                request.Headers.Add("Referer", "https://qatarliving.com/");
                request.Headers.Add("Origin", "https://qatarliving.com");
                var response = await Http.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();
                var successPatteren = "Thank you for subscribing!";

                var matches = Regex.Matches(responseContent, @"\((\{.*?\})\)");
                string msg = "";
                foreach (Match match in matches)
                {
                    string json = match.Groups[1].Value;
                    using var doc = JsonDocument.Parse(json);

                    if (doc.RootElement.TryGetProperty("msg", out var msgElement))
                    {
                        msg = msgElement.GetString();
                    }
                    else if (doc.RootElement.TryGetProperty("errors", out var errorsElement) && errorsElement.ValueKind != JsonValueKind.Null)
                    {
                        msg = errorsElement.ToString();
                    }
                }

                if (response.IsSuccessStatusCode && successPatteren.Equals(msg, StringComparison.OrdinalIgnoreCase))

                {
                    Snackbar.Add($"Subscription submitted: {msg}", Severity.Success);
                    SubscriptionStatusMessage = $"Subscription submitted: {msg}";
                }
                else if (response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(msg))
                {
                    Snackbar.Add($"{msg}", Severity.Warning);
                    SubscriptionStatusMessage = $"{msg}";
                }
                else
                {
                    Snackbar.Add("Failed to subscribe. Please try again.", Severity.Error);
                    SubscriptionStatusMessage = "Failed to subscribe. Please try again.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message} Newsletter subscription failed.");
                SubscriptionStatusMessage = "An error occurred while subscribing.";
                Snackbar.Add($"Failed to subscribe: {ex.Message}", Severity.Error);
            }
            finally
            {
                IsSubscribingToNewsletter = false;
                StateHasChanged();
            }


        }
    }
}
