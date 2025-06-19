using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.Web.Shared.Components.BreadCrumb;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services.Interface;
using System.ComponentModel.DataAnnotations;

namespace QLN.Web.Shared.Pages.Company
{
    public  partial class EditCompany : ComponentBase
    {

        [Inject] private ICompanyProfileService CompanyProfileService { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }

        [Parameter] public string id { get; set; } = string.Empty;

        protected List<QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem> breadcrumbItems = new();

        protected bool isCompanyLoading;
        private string _authToken;

        protected CompanyProfileModel? companyProfile;

        protected override void OnInitialized()
        {
            breadcrumbItems = new()
            {
                new() { Label = "Classifieds", Url = "qln/classifieds" },
                new() { Label = "Dashboard", Url = "/qln/classified/dashboard/items" },
                new() { Label = "Edit Company Profile", Url = $"/qln/dashboard/company/edit/{id}",IsLast=true },

            };
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _authToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6Ijk3NTQ1NGI1LTAxMmItNGQ1NC1iMTUyLWUzMGYzNmYzNjNlMiIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJNVUpBWSIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL2VtYWlsYWRkcmVzcyI6Im11amF5LmFAa3J5cHRvc2luZm9zeXMuY29tIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbW9iaWxlcGhvbmUiOiIrOTE3NzA4MjA0MDcxIiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjpbIlVzZXIiLCJTdWJzY3JpYmVyIl0sIlVzZXJJZCI6Ijk3NTQ1NGI1LTAxMmItNGQ1NC1iMTUyLWUzMGYzNmYzNjNlMiIsIlVzZXJOYW1lIjoiTVVKQVkiLCJFbWFpbCI6Im11amF5LmFAa3J5cHRvc2luZm9zeXMuY29tIiwiUGhvbmVOdW1iZXIiOiIrOTE3NzA4MjA0MDcxIiwiZXhwIjoxNzUwMzE2NjEyLCJpc3MiOiJRYXRhciBMaXZpbmciLCJhdWQiOiJRYXRhciBMaXZpbmcifQ.5wi8JuxREtoJFNutPFvZ7aZGscXgCIB9avR-cBdZwWg";
                await LoadCompanyProfileAsync(id, _authToken);
                StateHasChanged();
                

            }

            await base.OnAfterRenderAsync(firstRender);
        }

        protected async Task LoadCompanyProfileAsync(string id, string authToken)
        {
            isCompanyLoading = true;
            companyProfile = null;
            StateHasChanged();

            try
            {
                companyProfile = await CompanyProfileService.GetCompanyProfileByIdAsync(id,authToken);
                if (companyProfile == null)
                {
                    Snackbar.Add("Company profile not found", Severity.Warning);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading company profile: {ex.Message}");
                Snackbar.Add("Failed to load company profile", Severity.Error);

            }
            finally
            {
                isCompanyLoading = false;
                StateHasChanged();
            }
        }

        private async Task SaveCompanyProfileAsync()
        {
            try
            {
                if (companyProfile != null)
                {
                    // You may need to include CR or logo/document files if applicable
                    var updated = await CompanyProfileService.UpdateCompanyProfileAsync(companyProfile, _authToken);
                    if (updated)
                    {
                        Snackbar.Add("Company profile updated successfully", Severity.Success);
                    }
                    else
                    {
                        Snackbar.Add("Failed to update company profile", Severity.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SaveCompanyProfileAsync Exception: {ex.Message}");
                Snackbar.Add("An error occurred while updating", Severity.Error);
            }
        }

        public static string GetDisplayName<TEnum>(TEnum enumValue) where TEnum : Enum
        {
            var member = typeof(TEnum).GetMember(enumValue.ToString()).FirstOrDefault();
            var displayAttr = member?.GetCustomAttributes(typeof(DisplayAttribute), false)
                                     .FirstOrDefault() as DisplayAttribute;
            return displayAttr?.Name ?? enumValue.ToString();
        }

    }

}

