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
                ExpandGroupForCurrentRoute();
                StateHasChanged();
            };

            ExpandGroupForCurrentRoute();
        }

       private void ExpandGroupForCurrentRoute()
        {
            var matchingGroup = NavigationItems.FirstOrDefault(i =>
                i.IsGroup &&
                (IsActive(i) || i.Children?.Any(c => IsActive(c)) == true));

            if (matchingGroup != null)
            {
                ExpandedGroups.Add(matchingGroup.Title);
            }
        }


        protected void HandleRouterClick(string url)
        {
            var matchingGroup = NavigationItems.FirstOrDefault(i =>
                i.IsGroup && i.Children?.Any(c => c.Url == url) == true);

            if (matchingGroup != null)
            {
                ExpandedGroups.Add(matchingGroup.Title);
            }

            if (currentPath != NavManager.BaseUri + url.TrimStart('/'))
            {
                NavManager.NavigateTo(url, true);
            }
        }

          protected bool IsActive(NavigationItem item)
        {
            if (string.IsNullOrWhiteSpace(currentPath) || item == null)
                return false;

            var relativeUri = NavManager.ToBaseRelativePath(currentPath).ToLowerInvariant();

            // Check ActiveRoutePaths (if any)
            if (item.ActiveRoutePaths != null && item.ActiveRoutePaths.Any())
            {
                foreach (var activePath in item.ActiveRoutePaths)
                {
                    if (string.IsNullOrWhiteSpace(activePath)) continue;

                    var cleanPath = activePath.TrimStart('/').ToLowerInvariant();
                    if (relativeUri.StartsWith(cleanPath))
                        return true;
                }
            }

            // Fallback: match base URL
            if (!string.IsNullOrWhiteSpace(item.Url))
            {
                var cleanBaseUrl = item.Url.TrimStart('/').ToLowerInvariant();
                if (relativeUri.StartsWith(cleanBaseUrl))
                    return true;
            }

            return false;
        }



        protected Task ToggleGroup(string title)
        {
            if (ExpandedGroups.Contains(title))
                ExpandedGroups.Remove(title);
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
                Title = "Classified",
                IconPath = "/qln-images/classified_icon.svg",
                IsGroup = true,
                Children = new List<NavigationItem>
                {
                    new() { Title = "Landing Page", Url = "/manage/classified/landing" },
                    new() { Title = "Items", Url = "/manage/classified/items" },
                    new() { Title = "Deals", Url = "/manage/classified/deals/subscription/listing" },
                    new() { Title = "Stores", Url = "/manage/classified/stores" },
                    new() { Title = "Preloved", Url = "/manage/classified/preloved/subscription/listing" },
                    new() { Title = "Collectibles", Url = "/manage/classified/collectibles" },
                }
            },
             new() {
                Title = "Services",
                IconPath = "/qln-images/services_icon.svg",
                IsGroup = true,
                Children = new List<NavigationItem>
                {
                    new() { Title = "Landing Page", Url = "/manage/services/landing" },
                    new() { Title = "Services", Url = "/manage/services/listing/subscriptions" }, 
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
            },
            new() {
                Title = "Banner",
                Url = "/manage/banner",
                IconPath = "/qln-images/manage_banner.svg",
                ActiveRoutePaths = ["/manage/banner"]
            },
        };
    }
}
