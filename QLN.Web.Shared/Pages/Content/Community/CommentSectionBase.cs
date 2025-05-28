using Microsoft.AspNetCore.Components;
using QLN.Web.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Web.Shared.Pages.Content.Community
{
    public class CommentSectionBase : ComponentBase
    {
        [Parameter]
        public PostModel Comment { get; set; }
        protected string newComment = string.Empty;

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
    }
}
