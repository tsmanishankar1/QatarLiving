using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using QLN.Web.Shared.Models.QLN.Web.Shared.Models;
using QLN.Web.Shared.Services.Interface;


namespace QLN.Web.Shared.Pages.Company
{
    public  partial class AddCompany : ComponentBase
    {

        [Inject] private IJSRuntime _jsRuntime { get; set; }
        [Inject] private ICompanyProfileService CompanyProfileService { get; set; } = default!;

        private IBrowserFile uploadedLogoFile;
        private MudForm _form;
        private List<QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem> breadcrumbItems = new();
        private string logoPreviewUrl;
        private CompanyModel _model = new();

        private  IBrowserFile? uploadedDocumentFile;
        private string? documentPreviewBase64;
        private string? crFileName;

        protected override void OnInitialized()
        {
            breadcrumbItems = new()
            {
                new() { Label = "Classifieds", Url = "classifieds" },
                new() { Label = "Company", Url = "/company" },
                new() { Label = "Add Company", Url = "/add-company", IsLast = true },
            };
        }

        private async Task TriggerFileUpload()
        {
            await _jsRuntime.InvokeVoidAsync("document.getElementById('doc-upload').click");
        }

       

        private async Task HandleLogoUpload(InputFileChangeEventArgs e)
        {
            uploadedLogoFile = e.File;

            var buffer = new byte[uploadedLogoFile.Size];
            await uploadedLogoFile.OpenReadStream().ReadAsync(buffer);
            var imageType = uploadedLogoFile.ContentType;
            logoPreviewUrl = $"data:{imageType};base64,{Convert.ToBase64String(buffer)}";
        }
      
        private async Task HandleDocumentUpload(InputFileChangeEventArgs e)
        {
            uploadedDocumentFile = e.File;
            crFileName = uploadedDocumentFile.Name;

            var buffer = new byte[uploadedDocumentFile.Size];
            await uploadedDocumentFile.OpenReadStream(10 * 1024 * 1024).ReadAsync(buffer);
            var docType = uploadedDocumentFile.ContentType;
            documentPreviewBase64 = $"data:{docType};base64,{Convert.ToBase64String(buffer)}";
        }


        private async Task SubmitCompanyAsync()
        {
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");

            if (string.IsNullOrWhiteSpace(token))
            {
                Console.WriteLine("Auth token is missing.");
                return;
            }

            var success = await CompanyProfileService.CreateCompanyProfileAsync(
                _model,
                uploadedLogoFile,
                uploadedDocumentFile,
                token);

            if (success)
            {
            }
        }

    }


}

