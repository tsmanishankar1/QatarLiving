using Microsoft.AspNetCore.Components;
using QLN.Web.Shared.Model;


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
    }
}
