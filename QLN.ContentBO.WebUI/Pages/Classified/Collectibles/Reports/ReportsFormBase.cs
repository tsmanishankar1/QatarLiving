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
        private ValidationMessageStore messageStore;

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

            protected int SelectedFilterCount =>
               reports.ItemFieldFilters.Values.Sum(list => list.Count);


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
            BuildSelectedOptions();
            StateHasChanged();
        }

        protected void OnSubCategoryChanged(string subCategoryId)
        {
            reports.SelectedSubcategoryId = subCategoryId;
            reports.SelectedSubSubcategoryId = null;
            BuildSelectedOptions();
            StateHasChanged();
        }

        protected void OnSubSubCategoryChanged(string subSubCategoryId)
        {
            reports.SelectedSubSubcategoryId = subSubCategoryId;
            BuildSelectedOptions();
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

        protected void ToggleFilterPopover()
        {
            BuildSelectedOptions(); // Make sure filters are rebuilt
            showFilterPopover = !showFilterPopover;
        }


        protected void CancelFilters()
        {
            showFilterPopover = false;
            StateHasChanged();
        }
         private void BuildSelectedOptions()
        {
            SelectedOptions.Clear();

            foreach (var field in AvailableFields)
            {
                if (!SelectedOptions.ContainsKey(field.Name))
                    SelectedOptions[field.Name] = new Dictionary<string, bool>();

                foreach (var option in field.Options)
                {
                    bool isChecked = reports.ItemFieldFilters.TryGetValue(field.Name, out var selectedList)
                                    && selectedList.Contains(option);

                    SelectedOptions[field.Name][option] = isChecked;
                }
            }
        }


          protected List<string> GetFilteredModels(CategoryField field)
        {
            if (string.IsNullOrWhiteSpace(ModelSearchTerm))
                return field.Options;

            // Prioritize matches that start with the search term
            return field.Options
                .OrderByDescending(option => option.StartsWith(ModelSearchTerm, StringComparison.OrdinalIgnoreCase))
                .ThenBy(option => option.IndexOf(ModelSearchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
            protected Dictionary<string, Dictionary<string, bool>> ConfirmedOptions = new();
            protected Dictionary<string, Dictionary<string, bool>> SelectedOptions = new();
            protected string ModelSearchTerm = "";

            protected override void OnParametersSet()
        {
            if (AvailableFields == null)
                return;

            SelectedOptions.Clear();

            foreach (var field in AvailableFields)
            {
                SelectedOptions[field.Name] = new Dictionary<string, bool>();

                foreach (var option in field.Options)
                {
                    // If already saved in reports, restore the checked state
                    bool isChecked = reports.ItemFieldFilters.TryGetValue(field.Name, out var selectedList)
                                    && selectedList.Contains(option);

                    SelectedOptions[field.Name][option] = isChecked;
                }
            }
        }


            protected async Task ApplyFilters()
            {
                Console.WriteLine("Selected Filters:");

                var selectedFilterMap = new Dictionary<string, List<string>>();

                foreach (var field in SelectedOptions)
                {
                    var selectedOptions = field.Value
                        .Where(x => x.Value)
                        .Select(x => x.Key)
                        .ToList();

                    if (selectedOptions.Any())
                    {
                        selectedFilterMap[field.Key] = selectedOptions;
                        Console.WriteLine($"{field.Key}: {string.Join(", ", selectedOptions)}");
                    }
                }

                // ✅ Always update ConfirmedOptions from SelectedOptions
                ConfirmedOptions = SelectedOptions
                    .ToDictionary(
                        kv => kv.Key,
                        kv => kv.Value.ToDictionary(e => e.Key, e => e.Value)
                    );

                reports.ItemFieldFilters = selectedFilterMap;
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
