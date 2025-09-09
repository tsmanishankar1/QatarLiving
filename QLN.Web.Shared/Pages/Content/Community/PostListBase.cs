using Microsoft.AspNetCore.Components;
using QLN.Web.Shared.Contracts;


namespace QLN.Web.Shared.Pages.Content.Community
{
    public class PostListBase : ComponentBase
    {
        [Inject] public ICommunityService CommunityService { get; set; }
        [Inject] protected NavigationManager Navigation { get; set; }
        protected bool isLoading = false;

        protected List<PostItem> posts = new();

        protected override async Task OnInitializedAsync()
        {
            try
            {
                isLoading = true;
                var morePosts = await CommunityService.GetMorePostsAsync();
                posts = morePosts.Select(p => new PostItem(p.title, p.slug)).ToList();
            }
            finally
            {
                isLoading = false;
            }
        }

        protected record PostItem(string Title, string Slug);
        protected void NavigateToPostDetail(string slug)
        {
            Navigation.NavigateTo($"/content/community/post/detail/{slug}");
        }



    }
}
