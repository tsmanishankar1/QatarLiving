using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using MudBlazor;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services.Interface;
using System.ComponentModel.DataAnnotations;

namespace QLN.Web.Shared.Pages.Company
{
    public partial class EditCompany : ComponentBase
    {

        [Inject] private ICompanyProfileService CompanyProfileService { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }
        [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; }
        [Parameter] public string id { get; set; } = string.Empty;

        protected List<QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem> breadcrumbItems = new();

        protected bool isCompanyLoading;
        private bool isSaving = false;

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
                
                await LoadCompanyProfileAsync(id);
                StateHasChanged();

            }
            await base.OnAfterRenderAsync(firstRender);
        }


        protected async Task LoadCompanyProfileAsync(string id)
        {
            isCompanyLoading = true;
            companyProfile = null;
            StateHasChanged();

            try
            {
                companyProfile = await CompanyProfileService.GetCompanyProfileByIdAsync(id);
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

                isSaving = true;

                if (companyProfile != null)
                {
                    Console.WriteLine($"Saving Company Profile: {companyProfile}");
                    var updated = await CompanyProfileService.UpdateCompanyProfileAsync(companyProfile);
                    if (updated)
                    {
                        Snackbar.Add("Company profile updated successfully", Severity.Success);
                        await LoadCompanyProfileAsync(id);
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
            finally
            {
                isSaving = false;
                StateHasChanged();
            }
        }
        private async Task OnLogoFileSelected(IBrowserFile file)
        {
            if (file != null)
            {
                if (file.Size > 10 * 1024 * 1024)
                {
                    Snackbar.Add("Logo must be less than 10MB", Severity.Warning);
                    return;
                }

                using var ms = new MemoryStream();
                await file.OpenReadStream(10 * 1024 * 1024).CopyToAsync(ms);
                var base64 = Convert.ToBase64String(ms.ToArray());
                companyProfile.CompanyLogo = base64;
            }
        }

        private void ClearLogo()
        {
            companyProfile.CompanyLogo = null;
        }



        private string? crFileName;
        private string? crDocumentBase64;

        private async Task OnCrFileSelected(IBrowserFile file)
        {
            if (file.Size > 10 * 1024 * 1024)
            {
                Snackbar.Add("File too large. Max 10MB allowed.", Severity.Warning);
                return;
            }

            using var stream = file.OpenReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);

            crDocumentBase64 = Convert.ToBase64String(ms.ToArray());
            crFileName = file.Name;

            companyProfile.CrDocument = crDocumentBase64;
        }
        private void ClearCrFile()
        {
            crFileName = null;
            crDocumentBase64 = null;
            companyProfile.CrDocument = null;
        }

        public static string GetDisplayName<TEnum>(TEnum enumValue) where TEnum : Enum
        {
            var member = typeof(TEnum).GetMember(enumValue.ToString()).FirstOrDefault();
            var displayAttr = member?.GetCustomAttributes(typeof(DisplayAttribute), false)
                                     .FirstOrDefault() as DisplayAttribute;
            return displayAttr?.Name ?? enumValue.ToString();
        }


        private List<CountryCityModel> CountryCityList = new()
{
    new CountryCityModel { Country = "Qatar", Cities = new() { "Doha", "Al Wakrah", "Al Rayyan", "Lusail", "Umm Salal" }, CountryCode = "+974" },
    new CountryCityModel { Country = "UAE", Cities = new() { "Dubai", "Abu Dhabi", "Sharjah", "Ajman", "Fujairah" }, CountryCode = "+971" },
    new CountryCityModel { Country = "India", Cities = new() { "Mumbai", "Delhi", "Bangalore", "Chennai", "Hyderabad" }, CountryCode = "+91" },
    new CountryCityModel { Country = "USA", Cities = new() { "New York", "Los Angeles", "Chicago", "Houston", "Phoenix" }, CountryCode = "+1" },
    new CountryCityModel { Country = "UK", Cities = new() { "London", "Manchester", "Birmingham", "Leeds", "Liverpool" }, CountryCode = "+44" }
};

        private List<string> AvailableCities = new();

        private void OnCountryChanged(string selectedCountry)
        {
            companyProfile.Country = selectedCountry;

            var match = CountryCityList.FirstOrDefault(c => c.Country == selectedCountry);
            AvailableCities = match?.Cities ?? new();
            companyProfile.PhoneNumberCountryCode = match?.CountryCode ?? "";
            companyProfile.WhatsAppCountryCode = match?.CountryCode ?? "";

            companyProfile.City = AvailableCities.FirstOrDefault();
        }


    }

}

