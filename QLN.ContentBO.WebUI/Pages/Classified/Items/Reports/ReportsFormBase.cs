using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace QLN.ContentBO.WebUI.Pages.Classified.Items.Reports
{

    public class ReportsFormBase : ComponentBase
    {
        protected List<string> _categories = new() { "Sales", "Marketing", "HR", "IT" };
        protected List<string> _statuses = new() { "Active", "Inactive", "Pending" };
        protected List<string> _subcategories = new() { "Q1", "Q2", "Q3", "Q4" };

        protected string _selectedCategory;
        protected string _selectedStatus;
        protected string _selectedSubcategory;

        // Date range logic
        protected DateRange _dateRange = new(); // both Start and End are null by default
        protected DateRange _tempDateRange = new();

        protected bool showDatePopover = false;

        protected void ToggleDatePopover()
        {
            _tempDateRange = new DateRange(_dateRange.Start, _dateRange.End);
            showDatePopover = !showDatePopover;
        }

        protected void CancelDatePopover()
        {
            showDatePopover = false;
        }

        protected void ApplyDatePopover()
        {
            _dateRange = new DateRange(_tempDateRange.Start, _tempDateRange.End);
            showDatePopover = false;
            StateHasChanged();
        }
        protected bool showFilterPopover = false;
        protected bool _brandApple;
        protected bool _brandSamsung;

        protected bool _conditionUsed;
        protected bool _conditionNew;

        protected void ToggleFilterPopover()
        {
            showFilterPopover = !showFilterPopover;
        }

       protected void CancelFilters()
        {
            showFilterPopover = false;
            StateHasChanged(); // to apply filters
        }

        protected void ApplyFilters()
        {
            showFilterPopover = false;
            StateHasChanged(); // to apply filters
        }
        protected string FilterIconClass => showFilterPopover
            ? "report-date-icon active-icon"
            : "report-date-icon";


    }
}