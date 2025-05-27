using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using QLN.Web.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Web.Shared.Pages.Content.Community
{
    public class PostCardBase : ComponentBase
    {
        [Inject]
        protected NavigationManager Navigation { get; set; }

        [Parameter] public PostModel Post { get; set; } = new();
        [Parameter] public bool IsDetailView { get; set; } = false;

        protected void OnReport()
        {
            Console.WriteLine($"Reporting post: {Post.Title}");
        }
        protected void NavigateToPostDetail()
        {
            Navigation.NavigateTo($"/content/community/post/detail/{Post.Id}");
        }
    }
}
