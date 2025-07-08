using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;
using MudBlazor;

namespace QLN.ContentBO.WebUI.Pages.ReportsPage
{
    public class ReportsBase : QLComponentBase, IDisposable
    {
        [Inject]
        protected NavigationManager Navigation { get; set; }

        [Inject] protected ISnackbar Snackbar { get; set; }
       [Inject] protected ILogger<ReportsBase> _logger { get; set; }

        [Inject]
        protected IReportService _reportService { get; set; }

        protected string? Type;
        protected string searchText = string.Empty;
        protected bool ascending = true;
        protected int currentPage = 1;
        protected int pageSize = 10;
        protected bool IsLoading = false;

        protected List<ReportDto> _paginatedPosts = new();
        protected int TotalCount = 0;

        private string? previousType;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                await AuthorizedPage();
                Navigation.LocationChanged += OnLocationChanged;
                await SetTypeAndLoadReportsAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnInitializedAsync");
                throw;
            }
        }

        private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            var uri = new Uri(e.Location);
            var query = QueryHelpers.ParseQuery(uri.Query);
            if (query.TryGetValue("type", out var param))
            {
                var newType = param.ToString();
                if (newType != previousType)
                {
                    Type = newType;
                    previousType = newType;
                    currentPage = 1;
                    await InvokeAsync(LoadReportsAsync); // Avoid threading issues
                }
            }
        }

        private async Task SetTypeAndLoadReportsAsync()
        {
            var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("type", out var param))
            {
                Type = param;
                previousType = Type;
            }

            await LoadReportsAsync();
        }

        protected async Task LoadReportsAsync()
        {
            IsLoading = true;
             StateHasChanged();

            string endpoint = Type switch
            {
                "article-comments" => "/api/v2/report/getAll",
                "community-posts" => "/api/v2/report/getallcommunitypostswithpagination",
                "community-comments" => "/api/v2/report/getAllCommunityCommentReports",
                _ => "/api/v2/report/getAll"
            };

            var response = await _reportService.GetReportsWithPaginationAsync(
                endpoint,
                sortOrder: ascending ? "asc" : "desc",
                pageNumber: currentPage,
                pageSize: pageSize,
                searchTerm: searchText
            );

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var apiResult = JsonSerializer.Deserialize<ReportApiResponseDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (apiResult != null)
                {
                    _paginatedPosts = apiResult.Items.Select((item, index) =>
                    {
                        TotalCount = apiResult.TotalCount;
                        item.Number = index + 1 + ((currentPage - 1) * pageSize);
                        return item;
                    }).ToList();

                }
            }

            IsLoading = false;
            StateHasChanged();
        }
          protected async Task HandleIgnoreAction(Guid reportId)
            {
                await HandleReportAction(reportId, isDelete: false);
            }

            protected async Task HandleDeleteAction(Guid reportId)
            {
                await HandleReportAction(reportId, isDelete: true);
            }

    protected async Task HandleReportAction(Guid reportId, bool isDelete)
{
    var report = _paginatedPosts.FirstOrDefault(p => p.Id == reportId);
    if (report == null) return;

    string endpoint = Type switch
    {
        "article-comments" => "/api/v2/report/updatearticlecommentstatus",
        "community-posts" => "/api/v2/report/updatecommunitypoststatus",
        "community-comments" => "/api/v2/report/updatecommunitycommentreportstatus",
        _ => throw new ArgumentException("Invalid report type")
    };

    var isKeep = (!isDelete).ToString().ToLowerInvariant();
    var isDeleteStr = isDelete.ToString().ToLowerInvariant();

    _logger.LogInformation(
        "Calling UpdateReport: Endpoint={Endpoint}, ReportId={ReportId}, IsKeep={IsKeep}, IsDelete={IsDelete}",
        endpoint, reportId, isKeep, isDeleteStr
    );

    var response = await _reportService.UpdateReport(endpoint, reportId.ToString(), isKeep: !isDelete, isDelete: isDelete);

    if (response.IsSuccessStatusCode)
    {
        DeletePost(reportId);

        Snackbar.Add(
            isDelete ? "Report deleted successfully." : "Report ignored successfully.",
            isDelete ? Severity.Success : Severity.Info
        );

        StateHasChanged();
    }
    else
    {
        Snackbar.Add("Failed to update the report.", Severity.Error);
    }
}



        protected async Task HandleSearch(string value)
        {
            searchText = value?.Trim() ?? string.Empty;
            currentPage = 1;
            await LoadReportsAsync();
        }

        protected async Task HandleSort(bool sortAscending)
        {
            ascending = sortAscending;
            await LoadReportsAsync();
        }

        protected async Task HandlePageChange(int page)
        {
            currentPage = page;
            await LoadReportsAsync();
        }

        protected async Task HandlePageSizeChange(int size)
        {
            pageSize = size;
            currentPage = 1;
            await LoadReportsAsync();
        }
            protected void DeletePost(Guid id)
        {
            _paginatedPosts.RemoveAll(p => p.Id == id);
            TotalCount--;

            if (_paginatedPosts.Count == 0 && currentPage > 1)
            {
                currentPage--;
            }
        }

            
        public void Dispose()
        {
            Navigation.LocationChanged -= OnLocationChanged;
        }
    }
}
