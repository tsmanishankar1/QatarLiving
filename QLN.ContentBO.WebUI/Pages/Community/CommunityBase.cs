using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Pages.Community
{
    public class CommunityBase : ComponentBase
    {
        protected string searchText = string.Empty;
        protected List<CommunityListDto> _posts = new();
        protected List<CommunityListDto> _allPosts = new(); // To keep the unfiltered data
        protected bool IsLoading = true;
        protected bool ascending = true;

        protected override async Task OnInitializedAsync()
        {
            IsLoading = true;
            await Task.Delay(2000); // Simulate data fetch

            // Populate data
            _allPosts = Enumerable.Range(1, 12).Select(i => new CommunityListDto
            {
                Number = i,
                PostTitle = "Family Residence Visa status stuck “Under Review”",
                Category = i == 2 ? "Missing home" : "Advise and help",
                Username = "Ismat Zerin",
                CreationDate = new DateTime(2025, 4, 12).AddDays(-i),
                LiveFor = $"{i} hours"
            }).ToList();

            _posts = _allPosts.ToList();
            IsLoading = false;
        }

        protected void DeletePost(int number)
        {
            _posts.RemoveAll(p => p.Number == number);
            _allPosts.RemoveAll(p => p.Number == number);
        }

        protected Task HandleSearch(string value)
        {
            searchText = value?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                _posts = _allPosts.ToList();
            }
            else
            {
                _posts = _allPosts
                    .Where(p =>
                        p.PostTitle.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                        p.Category.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                        p.Username.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return Task.CompletedTask;
        }

        protected Task HandleSort(bool sortAscending)
        {
            ascending = sortAscending;

            _posts = ascending
                ? _posts.OrderBy(p => p.CreationDate).ToList()
                : _posts.OrderByDescending(p => p.CreationDate).ToList();

            return Task.CompletedTask;
        }
    }
}
