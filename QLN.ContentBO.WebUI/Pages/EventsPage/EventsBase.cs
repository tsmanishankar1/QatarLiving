using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using MudBlazor;
using Microsoft.AspNetCore.Components.Routing;

namespace QLN.ContentBO.WebUI.Pages.EventsPage
{
    public class EventsBase : ComponentBase
    {
        [Inject]
        protected NavigationManager Navigation { get; set; }
        protected int activeIndex = 0;
        protected string searchText;
        protected string selectedType;
        protected List<string> categories = new List<string> { "All Events", "Featured Events" };
        protected void NavigateToAddEvent()
        {
            Navigation.NavigateTo("/content/events/create");
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
            public string Reporter { get; set; } = "";
            public DateTime ReportDate { get; set; }
            public string LiveFor { get; set; } = "";
        }
         protected Status status = Status.Live;

        protected Color GetButtonColor(Status s) => s == status ? Color.Warning : Color.Default;

       protected enum Status
        {
            Live,
            Published,
            Unpublished
        }
    }
}