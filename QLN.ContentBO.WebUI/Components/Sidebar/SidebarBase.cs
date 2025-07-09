using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Options;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Components.Sidebar
{
    public class SidebarBase : ComponentBase
    {
        [Inject] protected NavigationManager NavManager { get; set; }
        [Inject] protected IOptions<NavigationPath> Options { get; set; }

        [Parameter] public bool IsLoggedIn { get; set; }
        [Parameter] public string Name { get; set; }
        [Parameter] public string Email { get; set; }

        protected HashSet<string> ExpandedGroups = new();
        protected string currentPath;

        protected override void OnInitialized()
        {
            currentPath = NavManager.Uri;
            NavManager.LocationChanged += (_, args) =>
            {
                currentPath = args.Location;
                StateHasChanged();
            };
        }

        protected void HandleRouterClick(string url)
        {
            var isChildOfExpandedGroup = NavigationItems
                .Where(i => i.IsGroup && ExpandedGroups.Contains(i.Title))
                .SelectMany(i => i.Children)
                .Any(child => child.Url == url);

            if (!isChildOfExpandedGroup)
            {
                ExpandedGroups.Clear();
            }

            if (currentPath != NavManager.BaseUri + url.TrimStart('/'))
            {
                NavManager.NavigateTo(url);
            }
        }

        protected bool IsActive(string path)
        {
            var relativeUri = NavManager.ToBaseRelativePath(currentPath ?? "").ToLowerInvariant();
            return relativeUri.StartsWith(path.TrimStart('/').ToLowerInvariant());
        }

        protected async Task ToggleGroup(string title)
        {
            var group = NavigationItems.FirstOrDefault(i => i.IsGroup && i.Title == title);
            if (group == null || group.Children == null || group.Children.Count == 0)
                return;

            if (ExpandedGroups.Contains(title))
            {
                ExpandedGroups.Remove(title);
            }
            else
            {
                ExpandedGroups.Clear();
                ExpandedGroups.Add(title);
                StateHasChanged();

                await Task.Delay(200);
                var firstChildUrl = group.Children.FirstOrDefault()?.Url;
                if (!string.IsNullOrEmpty(firstChildUrl))
                {
                    HandleRouterClick(firstChildUrl);
                }
            }
        }

        public class NavigationItem
        {
            public string Title { get; set; }
            public string Url { get; set; }
            public string IconPath { get; set; }
            public bool IsGroup { get; set; }
            public List<NavigationItem> Children { get; set; }
            public List<string> ActiveRoutePaths { get; set; } = new();
        }

        protected List<NavigationItem> NavigationItems => new()
    {
        new() {
            Title = "Daily Living",
            Url = "/dashboard",
            IconPath = "/qln-images/daily_icon.svg",
            ActiveRoutePaths = new() { "/dashboard" }
        },
        new() {
            Title = "News",
            IconPath = "/qln-images/news_icon.svg",
            IsGroup = true,
            ActiveRoutePaths = new() { "/manage/news/category", "/manage/news/addarticle", "/manage/news/editarticle" },
            Children = new List<NavigationItem>
            {
                new() { Title = "News", Url = "/manage/news/category/101" },
                new() { Title = "Business", Url = "/manage/news/category/102" },
                new() { Title = "Sports", Url = "/manage/news/category/103" },
                new() { Title = "Lifestyle", Url = "/manage/news/category/104" }
            }
        },
        new() {
            Title = "Community",
            Url = "/manage/community",
            IconPath = "/qln-images/community_icon.svg",
            ActiveRoutePaths = new() { "/manage/community" }
        },
        new() {
            Title = "Events",
            Url = "/manage/events",
            IconPath = "/qln-images/event_icon.svg",
            ActiveRoutePaths = new() { "/manage/events", "/content/events/create", "/content/events/edit" }
        },
        new() {
            Title = "Report",
            IconPath = "/qln-images/report_icon.svg",
            IsGroup = true,
            ActiveRoutePaths = new() { "/manage/reports" },
            Children = new List<NavigationItem>
            {
                new() { Title = "Article Comments", Url = "/manage/reports?type=article-comments" },
                new() { Title = "Community Posts", Url = "/manage/reports?type=community-posts" },
                new() { Title = "Community Comments", Url = "/manage/reports?type=community-comments" }
            }
        }
    };
    }

}