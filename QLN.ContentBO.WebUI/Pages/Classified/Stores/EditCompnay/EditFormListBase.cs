using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using QLN.ContentBO.WebUI.Models;
using MudExRichTextEditor;
using Microsoft.JSInterop;
using MudBlazor;

namespace QLN.ContentBO.WebUI.Pages.Classified.Stores.EditCompnay
{
    public class EditFormListBase : ComponentBase
    {
        [Parameter] public EditCompany Company { get; set; } = new();
        [Inject] private IJSRuntime JS { get; set; }
        protected MudExRichTextEdit Editor;
         [Inject] ISnackbar Snackbar { get; set; }
        protected string? _coverImageError;
        public string _localLogoBase64 { get; set; }
        protected CountryModel SelectedPhoneCountry;
        protected CountryModel SelectedWhatsappCountry;
        private DotNetObjectReference<EditFormListBase>? _dotNetRef;
        [Inject] ILogger<EditFormListBase> Logger { get; set; }
        [Inject]
        public NavigationManager NavigationManager { get; set; } = default!;

        protected string ShortFileName(string name, int max)
         {
                if (string.IsNullOrEmpty(name)) return "";
                return name.Length <= max ? name : name.Substring(0, max) + "...";
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _dotNetRef = DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("resetLeafletMap");
                await JS.InvokeVoidAsync("initializeMap", _dotNetRef);
            }
        }
        [JSInvokable]
        public Task SetCoordinates(double lat, double lng)
        {
            Logger.LogInformation("Map marker moved to Lat: {Lat}, Lng: {Lng}", lat, lng);
            StateHasChanged();
            return Task.CompletedTask;
        }
        protected Task OnPhoneCountryChanged(CountryModel model)
        {
            SelectedPhoneCountry = model;
            Company.PhoneCode = model.Code;
            return Task.CompletedTask;
        }

        protected Task OnWhatsappCountryChanged(CountryModel model)
        {
            SelectedWhatsappCountry = model;
            Company.WhatsappCode = model.Code;
            return Task.CompletedTask;
        }
        protected Task OnPhoneChanged(string phone)
        {
            Company.PhoneNumber = phone;
            return Task.CompletedTask;
        }

        protected Task OnWhatsappChanged(string phone)
        {
            Company.WhatsappNumber = phone;
            return Task.CompletedTask;
        }
        protected void ClearFile()
        {
            Company.CertificateFileName = null;
            Company.Certificate = null;
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

             Company.CertificateFileName = file.Name;
             Company.Certificate = Convert.ToBase64String(ms.ToArray());
        }



        public Dictionary<string, List<string>> FieldOptions { get; set; } = new()
        {
            { "Country", new() { "Qatar", "USA", "UK" } },
            { "City", new() { "Doha", "Dubai" } },
        };
        public Dictionary<string, List<string>> CompanyProfileOptions { get; set; } = new()
        {
            { "Nature of Business", new() { "Qatar", "USA", "UK" } },
            { "Company Size", new() { "Doha", "Dubai" } },
            { "Company Type", new() { "Doha", "Dubai" } }
        };

        protected void SubmitForm()
        {

        }
        protected async Task HandleFilesChanged(InputFileChangeEventArgs e)
        {
            var file = e.File;
            if (file != null)
            {
                using var stream = file.OpenReadStream(5 * 1024 * 1024);
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var base64 = Convert.ToBase64String(memoryStream.ToArray());
                 Company.CoverImageBase64 = $"data:{file.ContentType};base64,{base64}";
                _coverImageError = null;
            }
        }
        protected void RemoveCoverImage()
        {
            Company.CoverImageBase64 = null;
            StateHasChanged(); // ensure UI refreshes
        }

        protected async Task OnLogoFileSelected(IBrowserFile file)
        {
            var allowedImageTypes = new[] { "image/png", "image/jpg" };

            if (!allowedImageTypes.Contains(file.ContentType))
            {
                Snackbar.Add("Only image files (PNG, JPG) are allowed.", Severity.Warning);
                return;
            }
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
                 _localLogoBase64 = base64;
                 Company.CompanyLogoBase64 = base64;
            }
        }
        protected void onNavigationBack()
        {
            NavigationManager.NavigateTo("/manage/classified/stores");
        }
    }
}
