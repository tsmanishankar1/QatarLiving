using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using QLN.Web.Shared.Contracts;
using QLN.Web.Shared.Model;
using QLN.Web.Shared.Models;
using System.Security.Claims;


namespace QLN.Web.Shared.Pages.Content.Community
{
    public class CommentSectionBase : ComponentBase
    {

        [Inject] protected IJSRuntime JS { get; set; }
        [Inject] protected CookieAuthStateProvider CookieAuthenticationStateProvider { get; set; }
        [Inject] protected ICommunityService CommunityService { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }

        protected MudTextField<string> multilineReference;

        [Parameter]
        public PostModel PostModelData { get; set; }

        //public PostModel CommentList { get; set; } 

        public List<CommentModel> Comments { get; set; } = new();

        protected string newComment = string.Empty;

        protected int CurrentPage { get; set; } = 1;
        protected int PageSize { get; set; } = 5;
        protected int TotalCount { get; set; } = 10;


        protected bool isMenuOpen = false;
        protected bool IsLiked { get; set; } = false;


        public string Name { get; set; } = string.Empty;
        public int CurrentUserId { get; set; }
        public bool IsLoggedIn { get; set; } = false;
        protected bool IsLoading { get; set; } = false;
        protected bool IsPosting { get; set; } = false;
        protected HashSet<string> expandedComments = new();

        protected override async Task OnInitializedAsync()
        {
            //CommentList = new PostModel();
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
                if (string.IsNullOrWhiteSpace(newComment) || CurrentUserId == 0 || PostModelData == null)
                {
                    Snackbar.Add("Unable to post comment. Missing data.Please check back later!", Severity.Error);
                    return;
                }
                var request = new CommentPostRequest
                {
                    nid = int.TryParse(PostModelData.Id?.ToString(), out var nid) ? nid : 0,
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
                if (PostModelData?.Id == null || !IsLoggedIn || PostModelData.Comments.Count == 0)
                {
                    Comments.Clear();
                    return;
                }

                int nid = int.TryParse(PostModelData.Id, out var parsedNid) ? parsedNid : 0;
                var response = await CommunityService.GetCommentsByPostIdAsync(nid, page: CurrentPage, pageSize: PageSize);
                TotalCount = response.total_comments;

                if (TotalCount > 0)
                {
                    Comments = [.. response.comments.Select(c => new CommentModel
                    {
                        Id = c.comment_id,
                        CreatedBy = !string.IsNullOrWhiteSpace(c.user_name) ? c.user_name : "User not found",
                        CreatedAt = DateTime.TryParse(c.date_created, out var date) ? date : DateTime.Now,
                        Description = c.subject ?? "No content to display",
                        LikeCount = c.LikeCount,
                        UnlikeCount = c.UnlikeCount,
                        Avatar = !string.IsNullOrWhiteSpace(c.profile_picture)
                            ? c.profile_picture
                            : "/qln-images/content/Sample.svg"
                    })];
                }
                else
                {
                    Comments.Clear();
                }
            }
            catch (Exception ex)
            {
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
            Console.WriteLine("current page", CurrentPage);

            await GetCommentAsync();

            //StateHasChanged();
        }
        protected async Task ToggleLikeAsync()
        {
            IsLiked = !IsLiked;
        }

        protected void OnReport()
        {
            Console.WriteLine($"Reporting Comment");
        }
    }
}
