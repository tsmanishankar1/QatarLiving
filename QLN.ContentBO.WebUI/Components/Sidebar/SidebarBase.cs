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

                // Recalculate expanded groups based on route
                ExpandGroupForCurrentRoute();

                StateHasChanged();
            };

            // Expand relevant group on first load too
            ExpandGroupForCurrentRoute();
        }
        private void ExpandGroupForCurrentRoute()
        {
            var relativePath = NavManager.ToBaseRelativePath(currentPath).ToLowerInvariant();

            var matchingGroup = NavigationItems
                .FirstOrDefault(i =>
                    i.IsGroup &&
                    i.Children?.Any(c => relativePath.StartsWith(c.Url.TrimStart('/').ToLowerInvariant())) == true);

            if (matchingGroup != null)
            {
                ExpandedGroups.Add(matchingGroup.Title);
            }
        }

        protected void HandleRouterClick(string url)
        {
            // Find the group this child belongs to (if any)
            var matchingGroup = NavigationItems
                .FirstOrDefault(i => i.IsGroup && i.Children?.Any(c => c.Url == url) == true);

            if (matchingGroup != null)
            {
                // Ensure the group is expanded (but DON'T clear others!)
                ExpandedGroups.Add(matchingGroup.Title);
            }

            if (currentPath != NavManager.BaseUri + url.TrimStart('/'))
            {
                NavManager.NavigateTo(url, true);
            }
        }


        protected bool IsActive(string path)
        {
            var relativeUri = NavManager.ToBaseRelativePath(currentPath ?? "").ToLowerInvariant();
            return relativeUri.StartsWith(path.TrimStart('/').ToLowerInvariant());
        }

        protected Task ToggleGroup(string title)
        {
            if (ExpandedGroups.Contains(title))
            {
                ExpandedGroups.Remove(title);
            }
            else
            {
                ExpandedGroups.Clear();
                ExpandedGroups.Add(title);
            }

            StateHasChanged();
            return Task.CompletedTask;
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
            },
            new() {
                Title = "Banner",
                Url = "/manage/banner",
                IconPath = "/qln-images/manage_banner.svg",
                ActiveRoutePaths = ["/manage/banner"]
            },
        };
        }
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
            Title = "Classified",
            IconPath = "/qln-images/classified_icon.svg",
            IsGroup = true,
            ActiveRoutePaths = new() { "/manage/classified/" },
            Children = new List<NavigationItem>
            {
                new() { Title = "Landing Page", Url = "/manage/classified/landing" },
                new() { Title = "Items", Url = "/manage/classified/items" },
                new() { Title = "Deals", Url = "/manage/classified/deals" },
                new() { Title = "Stores", Url = "/manage/classified/stores" },
                new() { Title = "Preloved", Url = "/manage/classified/preloved" },
                new() { Title = "Collectibles", Url = "/manage/classified/collectibles" },

            }
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