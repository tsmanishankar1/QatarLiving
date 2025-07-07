using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Pages.ReportsPage
{
    public class ReportsBase : QLComponentBase
    {
        [Inject]
        protected NavigationManager Navigation { get; set; }

        protected string? Type;
        protected string searchText = string.Empty;
        protected bool ascending = true;
        protected int currentPage = 1;
        protected int pageSize = 10;
        protected bool IsLoading = false;

        protected int TotalCount => _filteredPosts.Count;

        protected List<ReportsListDto> _allPosts = new();

        protected List<ReportsListDto> _filteredPosts => string.IsNullOrWhiteSpace(searchText)
            ? _allPosts
            : _allPosts.Where(p =>
                p.PostTitle.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                p.Category.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                p.Username.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();

        protected List<ReportsListDto> PaginatedPosts => ascending
            ? _filteredPosts.OrderBy(p => p.CreationDate).Skip((currentPage - 1) * pageSize).Take(pageSize).ToList()
            : _filteredPosts.OrderByDescending(p => p.CreationDate).Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();

        protected override async Task OnInitializedAsync()
        {
            AuthorizedPage();
            var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("type", out var param))
            {
                Type = param;
            }
            IsLoading = true;
            await Task.Delay(2000);
            // Simulated data
            _allPosts = Enumerable.Range(1, 12).Select(i => new ReportsListDto
            {
                Number = i,
                PostTitle = "Family Residence Visa status stuck “Under Review”",
                Category = i == 2 ? "Missing home" : "Advise and help",
                Username = "Ismat Zerin",
                CreationDate = new DateTime(2025, 4, 12).AddDays(-i),
                Reporter = "Ismat Zerin",
                ReportDate = new DateTime(2025, 4, 12).AddDays(-i),
            }).ToList();
            IsLoading = false;
        }

        protected void DeletePost(int number)
        {
            _allPosts.RemoveAll(p => p.Number == number);

            // Adjust current page if needed
            if (_filteredPosts.Count <= (currentPage - 1) * pageSize)
                currentPage = Math.Max(1, currentPage - 1);
        }

        protected Task HandleSearch(string value)
        {
            searchText = value?.Trim() ?? string.Empty;
            currentPage = 1;
            return Task.CompletedTask;
        }

        protected Task HandleSort(bool sortAscending)
        {
            ascending = sortAscending;
            return Task.CompletedTask;
        }

        protected Task HandlePageChange(int page)
        {
            currentPage = page;
            return Task.CompletedTask;
        }

        protected Task HandlePageSizeChange(int size)
        {
            pageSize = size;
            currentPage = 1;
            return Task.CompletedTask;
        }
    }
}
