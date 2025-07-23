using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Pages.Classified.Collectibles.Reports
{
    public class ReportsFormBase : ComponentBase
    {
        [Inject] public IClassifiedService _classifiedsService { get; set; }
        [Inject] public ISnackbar Snackbar { get; set; }

        protected ReportsFormDto reports { get; set; } = new ReportsFormDto();
        protected EditContext editContext;
        protected ValidationMessageStore messageStore;

        protected override async Task OnInitializedAsync()
        {
            editContext = new EditContext(reports);
            messageStore = new ValidationMessageStore(editContext);
            await LoadCategoryTreesAsync();
        }

        protected bool IsLoadingCategories { get; set; } = true;
        protected string? ErrorMessage { get; set; }

        protected List<CategoryTreeDto> CategoryTrees { get; set; } = new();

        protected CategoryTreeDto SelectedCategory =>
            CategoryTrees.FirstOrDefault(x => x.Id.ToString() == reports.SelectedCategoryId);

        protected CategoryTreeDto SelectedSubcategory =>
            SelectedCategory?.Children?.FirstOrDefault(x => x.Id.ToString() == reports.SelectedSubcategoryId);

        protected CategoryTreeDto SelectedSubSubcategory =>
            SelectedSubcategory?.Children?.FirstOrDefault(x => x.Id.ToString() == reports.SelectedSubSubcategoryId);

        protected List<CategoryField> AvailableFields =>
            SelectedSubSubcategory?.Fields ??
            SelectedSubcategory?.Fields ??
            SelectedCategory?.Fields ??
            new List<CategoryField>();

        protected string[] AllowedFields => new[]
        {
            "Condition", "Ram", "Model", "Capacity", "Processor", "Brand",
            "Storage", "Colour", "Gender", "Resolution", "Coverage", "Battery Life",
            "Size"
        };

        private async Task LoadCategoryTreesAsync()
        {
            try
            {
                var response = await _classifiedsService.GetAllCategoryTreesAsync("collectibles");

                if (response?.IsSuccessStatusCode == true)
                {
                    var result = await response.Content.ReadFromJsonAsync<List<CategoryTreeDto>>();
                    CategoryTrees = result ?? new();
                }
                else
                {
                    ErrorMessage = $"Failed to load category trees. Status: {response?.StatusCode}";
                }
            }
            catch (Exception)
            {
                ErrorMessage = "Error loading category trees.";
            }
            finally
            {
                IsLoadingCategories = false;
                StateHasChanged();
            }
        }

        // Category Change Handlers
        protected void OnCategoryChanged(string categoryId)
        {
            reports.SelectedCategoryId = categoryId;
            reports.SelectedSubcategoryId = null;
            reports.SelectedSubSubcategoryId = null;
            StateHasChanged();
        }

        protected void OnSubCategoryChanged(string subCategoryId)
        {
            reports.SelectedSubcategoryId = subCategoryId;
            reports.SelectedSubSubcategoryId = null;
            StateHasChanged();
        }

        protected void OnSubSubCategoryChanged(string subSubCategoryId)
        {
            reports.SelectedSubSubcategoryId = subSubCategoryId;
            StateHasChanged();
        }

        // Date Range Logic
        protected DateRange _dateRange = new();
        protected DateRange _tempDateRange = new();

        protected bool showDatePopover = false;

        protected void ToggleDatePopover()
        {
            _tempDateRange = new DateRange(_dateRange.Start, _dateRange.End);
            showDatePopover = !showDatePopover;
        }

        protected void CancelDatePopover() => showDatePopover = false;

        protected void ApplyDatePopover()
        {
            _dateRange = new DateRange(_tempDateRange.Start, _tempDateRange.End);
            showDatePopover = false;
            StateHasChanged();
        }

        // Filter Popover
        protected bool showFilterPopover = false;

        protected void ToggleFilterPopover() => showFilterPopover = !showFilterPopover;

        protected void CancelFilters()
        {
            showFilterPopover = false;
            StateHasChanged();
        }

        protected void ApplyFilters()
        {
            showFilterPopover = false;
            StateHasChanged();
        }

        protected string FilterIconClass =>
            showFilterPopover ? "report-date-icon active-icon" : "report-date-icon";

        // Form Submission
        protected Dictionary<string, string> DynamicFieldErrors = new();

        protected void SubmitForm()
        {
            messageStore.Clear();
            DynamicFieldErrors.Clear();

            var isValid = editContext.Validate();

            if (SelectedCategory?.Children?.Any() == true && string.IsNullOrEmpty(reports.SelectedSubcategoryId))
            {
                messageStore.Add(() => reports.SelectedSubcategoryId, "Subcategory is required.");
                isValid = false;
            }

            if (SelectedSubcategory?.Children?.Any() == true && string.IsNullOrEmpty(reports.SelectedSubSubcategoryId))
            {
                messageStore.Add(() => reports.SelectedSubSubcategoryId, "Sub Subcategory is required.");
                isValid = false;
            }

            editContext.NotifyValidationStateChanged();

            if (!isValid)
            {
                Snackbar.Add("Please fill all required fields.", Severity.Error);
                return;
            }

            Snackbar.Add("Form is valid and ready to submit!", Severity.Success);
            // Proceed with form submission logic here...
        }
    }
}
