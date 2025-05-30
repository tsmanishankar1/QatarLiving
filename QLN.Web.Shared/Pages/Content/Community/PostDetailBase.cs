using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Model;
using QLN.Web.Shared.Services;


namespace QLN.Web.Shared.Pages.Content.Community
{
    public class PostDetailBase : ComponentBase
    {

        [Inject] protected ICommunityService CommunityService { get; set; } = default!;
        [Inject] protected ILogger<PostDetailBase> Logger { get; set; } = default!;

        protected List<QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem> breadcrumbItems = new();
        protected QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem? postBreadcrumbItem;

        protected string search = string.Empty;
        protected string sortOption = "Popular";
        [Parameter]
        public string slug { get; set; }

        protected bool IsLoading { get; set; } = true;
        protected bool HasError { get; set; } = false;
        protected PostModel SelectedPost { get; private set; }

        protected PostModel? post;

        protected override async Task OnInitializedAsync()
        {
            postBreadcrumbItem = new()
            {
                Label = slug ?? "Not Found",
                Url = $"/content/community/post/detail/{slug}",
                IsLast = true
            };

            breadcrumbItems = new()
            {
                new() { Label = "Community", Url = "/content/community" },
                new() { Label = "Discussion", Url = "/content/community" },
                postBreadcrumbItem
            };

            await LoadPostAsync();
        }

        private async Task LoadPostAsync()
        {
            try
            {
                IsLoading = true;
                HasError = false;
                StateHasChanged();

                var fetched = await CommunityService.GetPostBySlugAsync(slug);

                if (fetched != null)
                {
                    SelectedPost = new PostModel
                    {
                        Id = fetched.nid,
                        Category = fetched.category,
                        Title = fetched.title,
                        BodyPreview = fetched.description,
                        Author = fetched.user_name ?? "Unknown User",
                        Time = DateTime.TryParse(fetched.date_created, out var parsedDate) ? parsedDate : DateTime.MinValue,
                        LikeCount = 0,
                        CommentCount = fetched.comments?.Count ?? 0,
                        ImageUrl = fetched.image_url,
                        Slug = fetched.slug,
                        Comments = fetched.comments?.Select(c => new CommentModel
                        {
                            CreatedBy = c.username ?? "Unknown User",
                            CreatedAt = c.created_date,      
                            Description = c.subject ?? "No content to display",
                            LikeCount = 0,
                            UnlikeCount = 0,
                            Avatar = !string.IsNullOrWhiteSpace(c.profile_picture)
                                ? c.profile_picture
                                : "/images/content/Sample.svg"
                        }).ToList() ?? new List<CommentModel>()
                    };

                    if (postBreadcrumbItem is not null)
                    {
                        postBreadcrumbItem.Label = SelectedPost.Title;
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

    }
}
