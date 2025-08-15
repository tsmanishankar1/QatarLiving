using Microsoft.AspNetCore.Components;
using MudBlazor;
using Microsoft.JSInterop;
using Nextended.Core.Extensions;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Models;
using System.Text.Json;
using static QLN.ContentBO.WebUI.Components.ToggleTabs.ToggleTabs;

namespace QLN.ContentBO.WebUI.Pages.Classified.PreLoved.P2p
{
    public class P2pListingBase : QLComponentBase
    {

        [Inject] protected IPrelovedService PrelovedService { get; set; } = default!;
        [Inject] protected ILogger<P2pListingBase> Logger { get; set; } = default!;
         [Inject] protected IJSRuntime JS { get; set; } = default!;
        protected List<PrelovedP2PSubscriptionItem> Listings { get; set; } = new();
         [Inject] protected IDialogService DialogService { get; set; } = default!;
        protected string SearchText { get; set; } = string.Empty;
        protected string SortIcon { get; set; } = Icons.Material.Filled.Sort;
        protected string SortDirection { get; set; } = "asc";
        protected string SortField { get; set; } = "creationDate";

        protected DateTime? dateCreated { get; set; }
        protected DateTime? datePublished { get; set; }
        protected DateTime? tempCreatedDate { get; set; }
        protected DateTime? tempPublishedDate { get; set; }

        protected bool showCreatedPopover { get; set; } = false;
        protected bool showPublishedPopover { get; set; } = false;

        protected bool IsLoading { get; set; } = true;
        protected bool IsEmpty => !IsLoading && Listings.Count == 0;
        protected int TotalCount { get; set; }
        protected int CurrentPage { get; set; } = 1;
        protected int PageSize { get; set; } = 12;
        protected string SelectedTab { get; set; } = "Pending Approval";
        
        protected List<TabOption> tabOptions =
        [
            new() { Label = "Pending Approval", Value = "PendingApproval" },
            new() { Label = "Published", Value = "Published" },
            new() { Label = "Unpublished", Value = "Unpublished" },
            new() { Label = "Promoted", Value = "Promoted" },
            new() { Label = "Promoted", Value = "Promoted"  }
        ];

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
        }

        protected async Task LoadData()
        {
            IsLoading = true;

            try
            {
                var request = new PrelovedP2PSubscriptionQuery
                {
                    Status = SelectedTab.Capitalize(),
                    CreatedDate = dateCreated.ToString(),
                    PublishedDate = datePublished.ToString(),
                    Page = CurrentPage,
                    PageSize = PageSize,
                    Search = SearchText,
                    SortBy = SortField,
                    SortOrder = SortDirection
                };

                var response = await PrelovedService.GetPrelovedP2pListing(request);

                if (response?.IsSuccessStatusCode ?? false)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<PrelovedP2PSubscriptionResponse>(content, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    Listings = result?.Items ?? [];
                    TotalCount = result?.TotalCount ?? 0;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "LoadData");
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected async Task HandleTabChange(string newTab)
        {
            if (SelectedTab != newTab)
            {
                SelectedTab = newTab;
                CurrentPage = 1;
                await LoadData();
            }
        }

        protected async void OnSearchChanged(ChangeEventArgs e)
        {
            SearchText = e.Value?.ToString();
            CurrentPage = 1;
            await LoadData();
        }

        protected async void ToggleSort()
        {
            SortDirection = SortDirection == "asc" ? "desc" : "asc";
            SortIcon = SortDirection == "asc"
                ? Icons.Material.Filled.ArrowUpward
                : Icons.Material.Filled.ArrowDownward;
            await LoadData();
        }
        protected async Task ShowConfirmationExport()
        {
            var parameters = new DialogParameters
            {
                { "Title", "Export Classified Items" },
                { "Descrption", "Do you want to export the current classified item data to Excel?" },
                { "ButtonTitle", "Export" },
                { "OnConfirmed", EventCallback.Factory.Create(this, ExportToExcel) }
            };

            var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = DialogService.Show<ConfirmationDialog>("", parameters, options);
            var result = await dialog.Result;
        }
        private async Task ExportToExcel()
        {
            try
            {
                if (Listings == null || !Listings.Any())
                {
                    Snackbar.Add("No data available to export.", Severity.Warning);
                    return;
                }

                var exportData = Listings.Select((x, index) => new Dictionary<string, object?>
                {
                    ["S.No."] = index + 1,
                    ["Item Image"] = string.IsNullOrWhiteSpace(x.ImageUrl) ? "-" : x.ImageUrl, 
                    ["Ad ID"] = x?.AdId == null ? "-" : x.AdId,
                    ["Ad Title"] = string.IsNullOrWhiteSpace(x.AdTitle) ? "-" : x.AdTitle,
                    ["User Id"] = x?.UserId == null ? x?.UserId : "-",
                    ["User Name"] = string.IsNullOrWhiteSpace(x.UserName) ? "-" : x.UserName,
                    ["Category"] = string.IsNullOrWhiteSpace(x.Category) ? "-" : x.Category,
                    ["Sub Category"] = string.IsNullOrWhiteSpace(x.SubCategory) ? "-" : x.SubCategory,
                    ["Creation Date"] = x.CreatedDate.ToString("dd-MM-yyyy") ?? "-",
                    ["Date Published"] = x.PublishedDate.ToString("dd-MM-yyyy") ?? "-",
                    ["Date Expiry"] = x.ExpiryDate.ToString("dd-MM-yyyy") ?? "-"
                }).ToList();

                await JS.InvokeVoidAsync("exportToExcel", exportData, "P2P_Listings.xlsx", "P2P Listings");

                Snackbar.Add("Export successful!", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Export failed: {ex.Message}", Severity.Error);
            }
        }

        protected async Task HandlePageChanged(int newPage)
        {
            PageSize = 12;
            CurrentPage = newPage;
            await LoadData();
        }

        protected async Task HandlePageSizeChanged(int newSize)
        {
            PageSize = newSize;
            CurrentPage = 1;
            await LoadData();
        }

        protected void ToggleCreatedPopover()
        {
            showCreatedPopover = !showCreatedPopover;
        }

        protected void CancelCreatedPopover()
        {
            tempCreatedDate = dateCreated;
            showCreatedPopover = false;
        }

        protected void ConfirmCreatedPopover()
        {
            dateCreated = tempCreatedDate;
            showCreatedPopover = false;
        }

        protected void TogglePublishedPopover()
        {
            showPublishedPopover = !showPublishedPopover;
        }

        protected void CancelPublishedPopover()
        {
            tempPublishedDate = datePublished;
            showPublishedPopover = false;
        }

        protected void ConfirmPublishedPopover()
        {
            datePublished = tempPublishedDate;
            showPublishedPopover = false;
        }

        protected void ClearFilters()
        {
            dateCreated = null;
            datePublished = null;
            SearchText = string.Empty;
        }
    }
}
