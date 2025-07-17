using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using QLN.ContentBO.WebUI.Models;
using MudExRichTextEditor;
using Microsoft.JSInterop;
using MudBlazor;

namespace QLN.ContentBO.WebUI.Pages.Classified.Stores.CreateStore
{
    public class CreateStoreBase : ComponentBase
    {
        [Parameter]
        public string? UserEmail { get; set; }
        public string? Location { get; set; }
        public AdPost Ad { get; set; } = new();
        [Inject] private IJSRuntime JS { get; set; }
        protected MudExRichTextEdit Editor;
         [Inject] ISnackbar Snackbar { get; set; }
        protected string? _coverImageError;
        protected CountryModel SelectedPhoneCountry;
        protected CountryModel SelectedWhatsappCountry;
        public string? CoverImage { get; set; }
        private DotNetObjectReference<CreateStoreBase>? _dotNetRef;
        [Inject] ILogger<CreateStoreBase> Logger { get; set; }
        public string? PhoneCode { get; set; }
        public string? PhoneNumber { get; set; }
        public string? WhatsappCode { get; set; }
        public string? WhatsappNumber { get; set; }
        public string Email { get; set; }
        public string WebsiteUrl { get; set; }
        public string FacebookUrl { get; set; }
        public string InstagramUrl { get; set; }
        public string CRNumber { get; set; }
        public string UserDesignation { get; set; }
        public DateTime? StartDay { get; set; }
        public DateTime? EndDay { get; set; }
        public TimeSpan? StartHour { get; set; }
        public TimeSpan? EndHour { get; set; }
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
            Ad.PhoneCode = model.Code;
            return Task.CompletedTask;
        }

        protected Task OnWhatsappCountryChanged(CountryModel model)
        {
            SelectedWhatsappCountry = model;
            Ad.WhatsappCode = model.Code;
            return Task.CompletedTask;
        }
        protected Task OnPhoneChanged(string phone)
        {
            Ad.PhoneNumber = phone;
            return Task.CompletedTask;
        }

        protected Task OnWhatsappChanged(string phone)
        {
            Ad.WhatsappNumber = phone;
            return Task.CompletedTask;
        }
        protected void ClearFile()
        {
            Ad.CertificateFileName = null;
            Ad.Certificate = null;
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

             Ad.CertificateFileName = file.Name;
             Ad.Certificate = Convert.ToBase64String(ms.ToArray());
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
                CoverImage = $"data:{file.ContentType};base64,{base64}";
                _coverImageError = null;
            }
        }
        protected void EditImage()
        {
            CoverImage = null;
        }
    }
}
