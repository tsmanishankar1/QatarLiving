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


namespace QLN.Web.Shared.Pages.Content.Community
{
    public class CommentSectionBaseV2 : ComponentBase
    {

        [Inject] protected IJSRuntime JS { get; set; }
        [Inject] protected CookieAuthStateProvider CookieAuthenticationStateProvider { get; set; }
        [Inject] protected ICommunityService CommunityService { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }
        [Inject] protected IDialogService DialogService { get; set; }

        protected MudTextField<string> multilineReference;

        [Parameter]
        public PostModel Comment { get; set; }

        //public PostModel CommentList { get; set; } 

        public List<CommentModelV2> Comments { get; set; } = new();

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
                if (string.IsNullOrWhiteSpace(newComment) || CurrentUserId == 0 || Comment == null)
                {
                    Snackbar.Add("Unable to post comment. Missing data.Please check back later!", Severity.Error);
                    return;
                }
                var request = new CommentPostRequestDto
                {
                    CommunityPostId = Comment.Id,
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
                string postId = Comment.Id;
                var response = await CommunityService.GetCommentsByPostIdAsyncV2(postId);

                TotalCount = 10;

                if (response?.Any() == true)
                {
                    Comments = response.Select(c => new CommentModelV2
                    {
                        CommentId = c.CommentId,
                        UserName = !string.IsNullOrWhiteSpace(c.UserName) ? c.UserName : "User not found",
                        CommentedAt = c.CommentedAt != default ? c.CommentedAt : DateTime.Now,
                        Content = c.Content ?? "No content to display",
                        CommentsLikeCount = c.CommentsLikeCount,
                        UnlikeCount = c.UnlikeCount,

                    }).ToList();
                }
                else
                {
                    Comments.Clear();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading comments: {ex.Message}");
                Comments ??= new List<CommentModelV2>();
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
    }
    }
