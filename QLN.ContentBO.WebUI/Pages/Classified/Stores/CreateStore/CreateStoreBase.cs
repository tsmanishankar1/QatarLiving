using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using QLN.ContentBO.WebUI.Models;
using MudExRichTextEditor;
using Microsoft.JSInterop;
using MudBlazor;
using static QLN.ContentBO.WebUI.Pages.Classified.Stores.ViewStores.ViewStoresBase;

namespace QLN.ContentBO.WebUI.Pages.Classified.Stores.CreateStore
{
    public class CreateStoreBase : ComponentBase
    {
        [Parameter]
        public string? CompanyName { get; set; }

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
        public string _localLogoBase64 { get; set; }
        public string CompanyLogo { get; set; }

        protected override void OnInitialized()
        {
            // Simulated "get by company name" logic
            if (!string.IsNullOrWhiteSpace(CompanyName))
            {
                var company = CompanyName.Trim().ToLower();

                if (company == "lulu")
                {
                    PrepopulateForm(new SubscriptionOrder
                    {
                        CompanyName = "Lulu",
                        Email = "lulu@example.com",
                        WebUrl = "https://luluhypermarket.com",
                        Mobile = "1234567890",
                        Whatsapp = "9876543210",
                        StartDate = new DateTime(2025, 7, 1),
                        EndDate = new DateTime(2025, 12, 31)
                    });
                }
                else if (company == "carrefour")
                {
                    PrepopulateForm(new SubscriptionOrder
                    {
                        CompanyName = "Carrefour",
                        Email = "carrefour@example.com",
                        WebUrl = "https://carrefour.com",
                        Mobile = "2223334444",
                        Whatsapp = "4443332222",
                        StartDate = new DateTime(2025, 8, 1),
                        EndDate = new DateTime(2025, 11, 30)
                    });
                }
                else
                {
                    PrepopulateForm(new SubscriptionOrder
                    {
                        CompanyName = "Carrefour",
                        Email = "carrefour@example.com",
                        WebUrl = "https://carrefour.com",
                        Mobile = "2223334444",
                        Whatsapp = "4443332222",
                        StartDate = new DateTime(2025, 8, 1),
                        EndDate = new DateTime(2025, 11, 30)
                    });
                }
            }
        }

        private void PrepopulateForm(SubscriptionOrder order)
        {
            Ad.Title = order.CompanyName;
            Email = order.Email;
            WebsiteUrl = order.WebUrl;
            Ad.PhoneNumber = order.Mobile;
            Ad.WhatsappNumber = order.Whatsapp;
            Ad.PhoneCode = "+974"; // or parse from Mobile
            Ad.WhatsappCode = "+974"; // or parse from Whatsapp

            // Dummy mapping
            StartDay = order.StartDate;
            EndDay = order.EndDate;
            UserDesignation = "Manager"; // Placeholder

            // If you store more fields in SubscriptionOrder, map them here
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
        protected void ClearLogo()
        {
            CompanyLogo = null;
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
                 CompanyLogo = base64;
            }
        }
    }
}
