using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using MudBlazor;
using QLN.Web.Shared.Components;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services.Interface;
using System.ComponentModel.DataAnnotations;

namespace QLN.Web.Shared.Pages.Company
{
    public class AddCompanyBase : QLComponentBase
    {
        [Inject] private ICompanyProfileService CompanyProfileService { get; set; }

        [Inject] private ILogger<AddCompanyBase> Logger { get; set; }

        [Parameter]
        public int VerticalId { get; set; }

        [Parameter]
        public int CategoryId { get; set; }

        protected List<Components.BreadCrumb.BreadcrumbItem> breadcrumbItems = [];

        protected bool isSaving = false;

        protected CompanyProfileModelDto? companyProfile;

        protected string? crFileName;
        protected string? crDocumentBase64;

        protected List<string> AvailableCities = [];

        protected List<CountryCityModel> CountryCityList =
                [
                    new CountryCityModel { Country = "Qatar", Cities = new() { "Doha", "Al Wakrah", "Al Rayyan", "Lusail", "Umm Salal" }, CountryCode = "+974" },
                    new CountryCityModel { Country = "UAE", Cities = new() { "Dubai", "Abu Dhabi", "Sharjah", "Ajman", "Fujairah" }, CountryCode = "+971" },
                    new CountryCityModel { Country = "India", Cities = new() { "Mumbai", "Delhi", "Bangalore", "Chennai", "Hyderabad" }, CountryCode = "+91" },
                    new CountryCityModel { Country = "USA", Cities = new() { "New York", "Los Angeles", "Chicago", "Houston", "Phoenix" }, CountryCode = "+1" },
                    new CountryCityModel { Country = "UK", Cities = new() { "London", "Manchester", "Birmingham", "Leeds", "Liverpool" }, CountryCode = "+44" }
                ];


        protected override void OnInitialized()
        {
            try
            {
                AuthorizedPage();
                breadcrumbItems = new()
                {
                    new() { Label = "Classifieds", Url = "qln/classifieds" },
                    new() { Label = "Dashboard", Url = "/qln/classified/dashboard/items" },
                    new() { Label = "Create Company Profile", Url = $"/qln/dashboard/company/create",IsLast=true },

                };
                companyProfile = new CompanyProfileModelDto
                {
                    BranchLocations = new List<string> { "" },
                    Vertical = VerticalId,
                    SubVertical = CategoryId,
                    NatureOfBusiness = new List<int>()
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnInitialized");
            }
        }

        protected async Task SaveCompanyProfileAsync()
        {
            if (string.IsNullOrEmpty(companyProfile.CompanyLogo))
            {
                Snackbar.Add("Company logo is required", Severity.Error);
                return;
            }

            if (string.IsNullOrEmpty(crDocumentBase64))
            {
                Snackbar.Add("CR document is required", Severity.Error);
                return;
            }
            try
            {
                isSaving = true;

                if (companyProfile != null)
                {
                    Console.WriteLine($"Saving Company Profile: {companyProfile}");
                    var updated = await CompanyProfileService.CreateCompanyProfileAsync(companyProfile);
                    if (updated)
                    {
                        Snackbar.Add("Company profile created successfully", Severity.Success);
                    }
                    else
                    {
                        Snackbar.Add("Failed to create company profile", Severity.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SaveCompanyProfileAsync Exception: {ex.Message}");
                Snackbar.Add("An error occurred while creating", Severity.Error);
            }
            finally
            {
                isSaving = false;
                StateHasChanged();
            }
        }

        protected async Task OnLogoFileSelected(IBrowserFile file)
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

        protected void ClearLogo()
        {
            companyProfile.CompanyLogo = null;
        }

        protected async Task OnCrFileSelected(IBrowserFile file)
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

        protected void ClearCrFile()
        {
            crFileName = null;
            crDocumentBase64 = null;
            companyProfile.CrDocument = null;
        }

        protected static string GetDisplayName<TEnum>(TEnum enumValue) where TEnum : Enum
        {
            var member = typeof(TEnum).GetMember(enumValue.ToString()).FirstOrDefault();
            var displayAttr = member?.GetCustomAttributes(typeof(DisplayAttribute), false)
                                     .FirstOrDefault() as DisplayAttribute;
            return displayAttr?.Name ?? enumValue.ToString();
        }

        protected void OnCountryChanged(string selectedCountry)
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
