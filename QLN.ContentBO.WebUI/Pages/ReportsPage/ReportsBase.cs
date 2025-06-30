using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
namespace QLN.ContentBO.WebUI.Pages.ReportsPage
{
    public class ReportsBase : ComponentBase
    {
        [Inject]
        protected NavigationManager Navigation { get; set; }
        protected string searchText;
        protected string? Type;
        protected override void OnInitialized()
        {
            var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("type", out var param))
            {
                Type = param;
            }
        }
        protected List<PostItem> _posts = Enumerable.Range(1, 12).Select(i => new PostItem
        {
            Number = i,
            PostTitle = "Family Residence Visa status stuck “Under Review”",
            Category = i == 2 ? "Missing home" : "Advise and help",
            Username = "Ismat Zerin",
            CreationDate = new DateTime(2025, 4, 12),
            Reporter = "Ismat Zerin",
            ReportDate = new DateTime(2025, 4, 12),
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
            public string Reporter { get; set; } = "";
            public DateTime ReportDate { get; set; }
        }
        
    };    
}
