using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using MudBlazor;
using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Model;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services;
using System.Net.NetworkInformation;
using System.Security.Claims;


namespace QLN.Web.Shared.Pages.Content.Community
{
    public class CommentSectionBase : ComponentBase
    {

        [Parameter]
        public PostModel Comment { get; set; }
        protected string newComment = string.Empty;

        protected int CurrentPage { get; set; } = 1;
        protected int PageSize { get; set; } = 10;

        protected IEnumerable<CommentModel> PagedComments =>
            Comment.Comments?.Skip((CurrentPage - 1) * PageSize).Take(PageSize) ?? Enumerable.Empty<CommentModel>();

        protected bool isMenuOpen = false;
        protected bool IsLiked { get; set; } = false;

        [Inject] protected IJSRuntime JS { get; set; }
        [Inject] protected CookieAuthStateProvider CookieAuthenticationStateProvider { get; set; }
        [Inject] protected ICommunityService CommunityService { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }

        protected MudTextField<string> multilineReference;



        private MudTextField<string> textFieldRef;
      

        private NavigationPath navigationPath;
        public string Name { get; set; } = string.Empty;
        public int CurrentUserId { get; set; }
        public bool IsLoggedIn { get; set; } = false;

        protected override async Task OnInitializedAsync()
        {
            var authState = await CookieAuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity?.IsAuthenticated == true)
            {
                Name = user.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
                CurrentUserId = int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : 0;
                Console.WriteLine($"Current User: {CurrentUserId}");
                IsLoggedIn = true;
            }
        }



        protected void OnMenuToggle(bool open)
        {
            isMenuOpen = open;
            StateHasChanged();
        }
        protected void OnInputChanged(ChangeEventArgs e)
        {
            newComment = e.Value?.ToString() ?? string.Empty;
        }
      

        protected void PostComment()
        {
            Console.WriteLine($"Posted comment: {newComment}");
            newComment = string.Empty;
        }

        protected void OnReport()
        {
            Console.WriteLine($"Reporting Comment");
        }
        protected void HandlePageChange(int page)
        {
            CurrentPage = page;
        }

        protected void HandlePageSizeChange(int size)
        {
            PageSize = size;
            CurrentPage = 1; 
        }
        protected async Task ToggleLikeAsync()
        {
            IsLiked = !IsLiked;
        }

    }
}
