using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Hosting;
using Microsoft.JSInterop;
using MudBlazor;
using QLN.Web.Shared.Components.ReportDialog;
using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Model;
using QLN.Web.Shared.Models;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging;


namespace QLN.Web.Shared.Pages.Content.CommunityV2
{
    public class CommentSectionBaseV2 : ComponentBase
    {

        [Inject] protected IJSRuntime JS { get; set; }
        [Inject] protected CookieAuthStateProvider CookieAuthenticationStateProvider { get; set; }
        [Inject] protected ICommunityService CommunityService { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }
        [Inject] protected IDialogService DialogService { get; set; }
        [Inject] protected ILogger<CommentSectionBaseV2> Logger { get; set; } = default!;

        protected MudTextField<string> multilineReference;

        [Parameter]
        public PostModel CurrentPost { get; set; }

        //public PostModel CommentList { get; set; } 

        public List<CommentModelV2> Comments { get; set; } = new();

        protected string newComment = string.Empty;

        protected int CurrentPage { get; set; } = 1;
        protected int PageSize { get; set; } = 5;
        protected int TotalCount { get; set; } = 10;


        protected bool isMenuOpen = false;
        protected bool IsLiked { get; set; } = false;

        public string Name { get; set; } = string.Empty;
        public string CurrentUserId { get; set; }
        public bool IsLoggedIn { get; set; } = false;
        protected bool IsLoading { get; set; } = false;
        protected bool IsPosting { get; set; } = false;
        protected HashSet<string> expandedComments = new();

        protected override async Task OnInitializedAsync()
        { 
            try
            {
                var authState = await CookieAuthenticationStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;

                if (user.Identity?.IsAuthenticated == true)
                {
                    CurrentUserId = user.FindFirst("uid")?.Value
                         ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? string.Empty;
                    IsLoggedIn = true;

                }
                else
                {
                    Logger.LogWarning("User is not authenticated.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while retrieving user authentication state.");
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await GetCommentAsync();
            }
        }

        protected void ToggleComment(string id)
        {
            if (!expandedComments.Add(id))
                expandedComments.Remove(id);
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
            IsPosting = true;

            try
            {
                if (string.IsNullOrWhiteSpace(newComment) || CurrentPost == null)
                {
                    Snackbar.Add("Unable to post comment. Missing data.Please check back later!", Severity.Error);
                    return;
                }
                var request = new CommentPostRequestDto
                {
                    CommunityPostId = CurrentPost.Id,
                    Content = newComment
                };

                var success = await CommunityService.PostCommentAsyncV2(request);
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
            catch (Exception ex)
            {
                Snackbar.Add($"Error posting comment: {ex.Message}", Severity.Error);
                Console.WriteLine($"Error posting comment: {ex}");
            }
            finally
            {
                IsPosting = false;
                StateHasChanged();
            }
        }

        protected async Task GetCommentAsync()
        {
            IsLoading = true;

            try
            {
                if (CurrentPost?.Id == null || !IsLoggedIn || CurrentPost.Comments.Count == 0)
                {
                    Comments.Clear();
                    return;
                }

                string postId = CurrentPost.Id;
                var response = await CommunityService.GetCommentsByPostIdAsyncV2(postId ,page: CurrentPage, pageSize: PageSize);
                TotalCount = response.TotalComments;

                if (response?.comments != null && response.comments.Any())
                {
                    Comments = [.. response.comments.Select(c => new CommentModelV2
                    {
                        CommentId = c.CommentId,
                        UserName = !string.IsNullOrWhiteSpace(c.UserName) ? c.UserName : "User not found",
                        CommentedAt = c.CommentedAt != default ? c.CommentedAt : DateTime.Now,
                        Content = c.Content ?? "No content to display",
                        CommentsLikeCount = c.CommentsLikeCount,
                        IsLiked = c.LikedUserIds?.Contains(CurrentUserId) ?? false,

                    })];
                }
                else
                {
                    Comments.Clear();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error loading comments: {ex.Message}");
                Comments ??= new List<CommentModelV2>();
                Comments.Clear();

                Snackbar.Add("Error loading comments.", Severity.Error);
                Console.WriteLine($"Error loading comments: {ex.Message}");
                Comments.Clear();
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }


        protected async Task HandlePageSizeChange(int newPageSize)
        {
            PageSize = newPageSize;
            CurrentPage = 1;
            await GetCommentAsync();
        }

        protected async Task HandlePageChange(int newPage)
        {
            CurrentPage = newPage;

            await GetCommentAsync();

            //StateHasChanged();
        }

        protected async Task OnReport(string postId, string commentId)
        {
            var parameters = new DialogParameters
    {
        { "PostId", postId },
        { "CommentId", commentId },
        { "Type", "Comment" }
    };

            var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = DialogService.Show<ReportDialog>("", parameters, options);
            var result = await dialog.Result;
        }
        protected async Task ToggleLikeAsync(string postId, string commentId)
        {
            if (!IsLoggedIn)
            {
                Snackbar.Add("Please login to like this comment.", Severity.Warning);
                return;
            }

            try
            {
                var success = await CommunityService.LikeCommunityCommentstAsync(postId, commentId);

                if (success)
                {
                    var comment = Comments.FirstOrDefault(c => c.CommentId == commentId);
                    if (comment is not null)
                    {
                        IsLiked = !IsLiked;
                        comment.CommentsLikeCount += IsLiked ? 1 : -1;
                    }
                }
                else
                {
                    Snackbar.Add("Failed to like the comment.", Severity.Warning);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error while liking comment: {ex.Message}");
                Snackbar.Add("An unexpected error occurred.", Severity.Error);
            }

            StateHasChanged();
        }

    }

}
