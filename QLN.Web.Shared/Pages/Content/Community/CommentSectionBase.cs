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
using System.Xml.Linq;


namespace QLN.Web.Shared.Pages.Content.Community
{
    public class CommentSectionBase : ComponentBase
    {

        [Inject] protected IJSRuntime JS { get; set; }
        [Inject] protected CookieAuthStateProvider CookieAuthenticationStateProvider { get; set; }
        [Inject] protected ICommunityService CommunityService { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }

        protected MudTextField<string> multilineReference;

        //[Parameter]
        //public PostModel Comment { get; set; }
        public PostModel Comment { get; set; }
        protected string newComment = string.Empty;

        protected IEnumerable<CommentModel> PagedComments =>
          Comment.Comments?.Skip((CurrentPage - 1) * PageSize).Take(PageSize) ?? Enumerable.Empty<CommentModel>();

        protected int CurrentPage { get; set; } = 1;
        protected int PageSize { get; set; } = 10;

   
        protected bool isMenuOpen = false;
        protected bool IsLiked { get; set; } = false;


        public string Name { get; set; } = string.Empty;
        public int CurrentUserId { get; set; }
        public bool IsLoggedIn { get; set; } = false;
        protected bool IsLoading { get; set; } = false;

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
            await GetCommentAsync();
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

        protected async void PostComment()
        {
            if (string.IsNullOrWhiteSpace(newComment) || CurrentUserId == 0 || Comment == null)
            {
                Snackbar.Add("Unable to post comment. Missing data.Please check back later!", Severity.Error);
                return;
            }

            var request = new CommentPostRequest
            {
                nid = int.TryParse(Comment.Id?.ToString(), out var nid) ? nid : 0,
                uid = CurrentUserId,
                comment = newComment
            };
           
            var success = await CommunityService.PostCommentAsync(request);
            if (success)
            {
                newComment = string.Empty;
                await GetCommentAsync();

            }
            else
            {
                Snackbar.Add("Failed to post comment.", Severity.Error);
            }
        }

        protected async Task GetCommentAsync()
        {
            IsLoading = true;
            StateHasChanged();

            try
            {
                int nid = int.TryParse(Comment?.Id?.ToString(), out var parsedNid) ? parsedNid : 0;
                var response = await CommunityService.GetCommentsByPostIdAsync(nid, page: CurrentPage, pageSize: PageSize);

                if (response?.comments != null && response.comments.Any())
                {
                    Comment.Comments = response.comments.Select(c => new CommentModel
                    {
                        Id = c.comment_id,
                        CreatedBy = !string.IsNullOrWhiteSpace(c.user_name) ? c.user_name : "User " + c.user_id,
                        CreatedAt = DateTime.TryParse(c.date_created, out var date) ? date : DateTime.Now,
                        Description = c.subject ?? "No content to display",
                        LikeCount = c.LikeCount,
                        UnlikeCount = c.UnlikeCount,
                        Avatar = !string.IsNullOrWhiteSpace(c.profile_picture)
                            ? c.profile_picture
                            : "/qln-images/content/Sample.svg"
                    }).ToList();
                }
                else
                {
                    Comment.Comments = new List<CommentModel>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading comments: {ex.Message}");
                Comment.Comments = new List<CommentModel>();
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
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
