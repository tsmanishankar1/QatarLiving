using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components;
using Microsoft.JSInterop;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Pages.Classified.DealsMenu.DealsSection
{
    public class DealsListingBase : QLComponentBase
    {

        [Inject]
        protected IClassifiedService ClassifiedService { get; set; } = default!;
        [Inject] protected IDialogService DialogService { get; set; } = default!;
         [Inject] protected IJSRuntime JS { get; set; } = default!;
        protected List<DealsListingModal> Listings { get; set; } = new();
        protected string SearchText { get; set; } = string.Empty;
         protected string SubscriptionType { get; set; } = string.Empty;
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
        protected int currentPage { get; set; } = 1;
        protected int pageSize { get; set; } = 12;
        protected string SelectedTab { get; set; } = ((int)AdStatusEnum.Published).ToString();

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
        }

        protected async Task LoadData()
        {
            IsLoading = true;
            StateHasChanged();

            try
            {
                int? status = null;
                bool? isPromoted = null;
                bool? isFeatured = null;

                if (int.TryParse(SelectedTab, out var tabValue))
                {
                    status = tabValue;
                }
                else
                {
                    if (SelectedTab == "promoted")
                        isPromoted = true;
                    else if (SelectedTab == "featured")
                        isFeatured = true;
                }
                var request = new FilterRequest
                {
                    PageNumber = currentPage,
                    PageSize = pageSize,
                    SubscriptionType = SubscriptionType,
                    SearchText = SearchText,
                    StartDate = dateCreated,
                    PublishedDate = datePublished,
                    SortBy = SortDirection,
                };


                var response = await ClassifiedService.GetDealsListing(request);

                if (response?.IsSuccessStatusCode ?? false)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<PagedResult<DealsListingModal>>(content, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    });
                    Listings = result?.Items ?? new List<DealsListingModal>();
                    TotalCount = result?.TotalCount ?? 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading data: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
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
                    ["Ad ID"] = x.AdId ?? 0,
                    ["Deals Title"] = string.IsNullOrWhiteSpace(x.DealTitle) ? "-" : x.DealTitle,
                    ["Subscription Type"] = string.IsNullOrWhiteSpace(x.SubscriptionType) ? "-" : x.SubscriptionType,
                    ["Location"] = (x.Location?.Any() == true) ? string.Join(", ", x.Location) : "-",
                    ["Date Created"] = x.DateCreated?.ToString("dd-MM-yyyy") ?? "-",
                    ["Start Date"] = x.StartDate?.ToString("dd-MM-yyyy") ?? "-", 
                    ["Expiry Date"] = x.ExpiryDate?.ToString("dd-MM-yyyy") ?? "-",
                    ["Mobile"] = string.IsNullOrWhiteSpace(x.ContactNumber) ? "-" : x.ContactNumber,
                    ["Whatsapp"] = string.IsNullOrWhiteSpace(x.WhatsappNumber) ? "-" : x.WhatsappNumber,
                    ["Web URL"] = string.IsNullOrWhiteSpace(x.WebUrl) ? "-" : x.WebUrl,
                    ["Web Clicks"] = x.WebClick,
                    ["Views"] = x.Views,
                    ["Impression"] = x.Impression,
                    ["Phone Leads"] = x.PhoneLead
                }).ToList();

                await JS.InvokeVoidAsync("exportToExcel", exportData, "Deals_Listings.xlsx", "Deals Listings");

                Snackbar.Add("Export successful!", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Export failed: {ex.Message}", Severity.Error);
            }
        }





        protected async Task HandleTabChange(string newTab)
        {
            if (SelectedTab != newTab)
            {
                SelectedTab = newTab;
                currentPage = 1;
                await LoadData();
                StateHasChanged();
            }
        }

        protected async void OnSearchChanged(ChangeEventArgs e)
        {
            SearchText = e.Value?.ToString();
            currentPage = 1;
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


        protected async Task HandlePageChanged(int newPage)
        {
            currentPage = newPage;
            await LoadData();
        }

        protected async Task HandlePageSizeChanged(int newSize)
        {
            pageSize = newSize;
            currentPage = 1;
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
