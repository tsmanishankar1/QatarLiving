using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using QLN.ContentBO.WebUI.Models;
using MudBlazor;

namespace QLN.ContentBO.WebUI.Pages.Classified.Stores.EditSubscription
{
    public class EditSubscriptionLogoBase : ComponentBase
    {
        [Parameter] public EditCompany Company { get; set; } = new();

        protected string? LocalLogoBase64 { get; set; }

        [Inject] protected ISnackbar Snackbar { get; set; }

        // ✅ Use this in Razor to check if logo exists
        protected bool HasLogo => !string.IsNullOrEmpty(LocalLogoBase64) || !string.IsNullOrEmpty(Company.CompanyLogoBase64);

        // ✅ Used for <img src="@LogoSource" />
        protected string LogoSource =>
            !string.IsNullOrEmpty(LocalLogoBase64)
                ? $"data:image/*;base64,{LocalLogoBase64}"
                : Company.CompanyLogoBase64 ?? string.Empty;

        protected async Task HandleLogoFileSelected(IBrowserFile file)
        {
            var allowedImageTypes = new[] { "image/png", "image/jpg", "image/jpeg" };

            if (!allowedImageTypes.Contains(file.ContentType))
            {
                Snackbar.Add("Only PNG and JPG images are allowed.", Severity.Warning);
                return;
            }

            if (file.Size > 10 * 1024 * 1024)
            {
                Snackbar.Add("Logo must be less than 10MB.", Severity.Warning);
                return;
            }

            using var ms = new MemoryStream();
            await file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024).CopyToAsync(ms);
            var base64 = Convert.ToBase64String(ms.ToArray());

            LocalLogoBase64 = base64;
            Company.CompanyLogoBase64 = $"data:{file.ContentType};base64,{base64}";
        }

        protected void ClearLogo()
        {
            Company.CompanyLogoBase64 = null;
            LocalLogoBase64 = null;
        }
    }
}
