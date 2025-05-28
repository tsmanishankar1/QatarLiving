using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Model;


namespace QLN.Web.Shared.Pages.Content.Community
{
    public class CommunityBase : ComponentBase
    {

        [Inject] private ILogger<CommunityBase> Logger { get; set; }
        [Inject] private ICommunityService CommunityService { get; set; }
        [Inject] private INewsLetterSubscription NewsLetterSubscriptionService { get; set; }
        [Inject] private IAdService AdService { get; set; }
        protected string search = string.Empty;
        protected string sortOption = "Popular";
        protected bool IsLoading { get; set; } = true;
        protected bool HasError { get; set; } = false;


        // Newsletter subscription
        protected NewsLetterSubscriptionModel SubscriptionModel { get; set; } = new();
        protected string SubscriptionStatusMessage = string.Empty;

        protected bool IsSubscribingToNewsletter { get; set; } = false;


        protected List<PostModel> PostList { get; set; } = [];
        //Ad
        protected AdModel Ad { get; set; } = null;

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

                var response = await CommunityService.GetAllAsync();
                if (response != null)
                {
                    return response.ToList();
                }
                return [];
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Get Community Post Async");
                HasError = true;
                return [];
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected async Task SubscribeAsync()
        {

            try
            {
                var success = await NewsLetterSubscriptionService.SubscribeAsync(SubscriptionModel);
                SubscriptionStatusMessage = success ? "Subscribed successfully!" : "Failed to subscribe.";
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Newsletter subscription failed.");
                SubscriptionStatusMessage = "An error occurred while subscribing.";
            }
        }
        private async Task<AdModel> GetAdAsync()
        {
            var response = await AdService.GetAdDetail();
            return response.FirstOrDefault();
        }

    }
}
