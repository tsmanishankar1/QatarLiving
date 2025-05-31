using System.Linq;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;
using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Model;


namespace QLN.Web.Shared.Pages.Content.Community
{
    public class CommunityBase : ComponentBase
    {
        [Inject]
        public ISnackbar Snackbar { get; set; }
        [Inject] private ILogger<CommunityBase> Logger { get; set; }
        [Inject] private ICommunityService CommunityService { get; set; }
        [Inject] private INewsLetterSubscription NewsLetterSubscriptionService { get; set; }
        [Inject] private IAdService AdService { get; set; }
        protected string search = string.Empty;
        protected string sortOption = "Default";
        private string ApiSortValue => sortOption == "Default" ? null : sortOption;

        protected bool IsLoading { get; set; } = true;
        protected bool HasError { get; set; } = false;

        // Pagination
        protected int CurrentPage { get; set; } = 1;
        protected int PageSize { get; set; } = 10;
        protected int TotalPosts { get; set; } = 0;

        // Newsletter subscription
        protected NewsLetterSubscriptionModel SubscriptionModel { get; set; } = new();
        protected string SubscriptionStatusMessage = string.Empty;

        protected bool IsSubscribingToNewsletter { get; set; } = false;


        protected List<PostModel> PostList { get; set; } = [];
        //Ad
        protected AdModel Ad { get; set; } = null;

        protected MudForm _form;

        protected async override Task OnInitializedAsync()
        {
            try
            {
                PostList = await GetPostListAsync();
                Ad = await GetAdAsync();

            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnInitializedAsync");
            }
        }


        protected async Task HandleSearchResults()
        {
            Console.WriteLine("Search completed.");
        }

        protected async Task<List<PostModel>> GetPostListAsync()
        {
            try
            {
                IsLoading = true;
                HasError = false;

                var dtoList = await CommunityService.GetPostsAsync(
                    forumId: 20000006,
                    order: GetOrderFromSortOption(),
                    page: CurrentPage,
                    pageSize: PageSize
                );
                if (dtoList == null || !dtoList.Any())
                {
                    HasError = true;
                    return null;
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
                    Slug = dto.slug
                }).ToList();

                return postModelList;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Get Community Post Async");
                HasError = true;
                return null;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private string GetOrderFromSortOption()
        {
            return sortOption switch
            {
                "Recent" => "desc",
                "Oldest" => "asc",
                _ => "desc" 
            };
        }

        protected async Task HandleSortChange(string newSortOption)
        {
            sortOption = newSortOption;
            CurrentPage = 1;
            await GetPostListAsync();
        }

        protected async Task HandlePageChange(int newPage)
        {
            CurrentPage = newPage;
            await GetPostListAsync();
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
        private async Task<AdModel> GetAdAsync()
        {
            var response = await AdService.GetAdDetail();
            return response.FirstOrDefault();
        }

    }
}
