using Microsoft.AspNetCore.Components;
using MudBlazor;
namespace QLN.ContentBO.WebUI.Pages.Community
{
    public class CommunityBase : ComponentBase
    {
        protected string searchText;
        protected List<PostItem> _posts = Enumerable.Range(1, 12).Select(i => new PostItem
        {
            Number = i,
            PostTitle = "Family Residence Visa status stuck “Under Review”",
            Category = i == 2 ? "Missing home" : "Advise and help",
            Username = "Ismat Zerin",
            CreationDate = new DateTime(2025, 4, 12),
            LiveFor = "2 hours"
        }).ToList();

            protected void DeletePost(int number)
        {
             _posts.RemoveAll(p => p.Number == number);
        }

        public class PostItem
        {
            public int Number { get; set; }
            public string PostTitle { get; set; } = "";
            public string Category { get; set; } = "";
            public string Username { get; set; } = "";
            public DateTime CreationDate { get; set; }
            public string LiveFor { get; set; } = "";
        }
    };    
}
