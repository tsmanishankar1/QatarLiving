using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Models;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using MudExRichTextEditor;
using QLN.ContentBO.WebUI.Interfaces;


namespace QLN.ContentBO.WebUI.Pages.Services.EditService
{
    public class EditServiceFormBase : ComponentBase
    {
        [Inject] public IServiceBOService _serviceService { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Inject] public IDialogService DialogService { get; set; }
        protected bool _priceOnRequest = false;
        protected bool IsLoadingCategories { get; set; } = true;
        protected List<LocationZoneDto> Zones { get; set; } = new();

        protected string? ErrorMessage { get; set; }
        protected Dictionary<string, List<string>> DynamicFieldErrors { get; set; } = new();
        [Inject] ISnackbar Snackbar { get; set; }

        public AdPost Ad { get; set; } = new();
        [Parameter]
        public ServicesDto selectedService { get; set; } = new();
        protected EditContext editContext;
        private ValidationMessageStore messageStore;
        [Inject] private IJSRuntime JS { get; set; }
        protected MudExRichTextEdit Editor;
        private DotNetObjectReference<EditServiceFormBase>? _dotNetRef;
        [Inject] ILogger<EditServiceFormBase> Logger { get; set; }
        protected CountryModel SelectedPhoneCountry;
        protected CountryModel SelectedWhatsappCountry;
        [Parameter] public List<ServiceCategory> CategoryTrees { get; set; } = new();

        protected string? _selectedCategoryId;
        protected string? _selectedL1CategoryId;
        protected string? _selectedL2CategoryId;

        protected List<L1Category> _selectedL1Categories = new();
        protected List<L2Category> _selectedL2Categories = new();
        protected override async Task OnParametersSetAsync()
        {
            if (selectedService == null)
                return;

            if (CategoryTrees == null || !CategoryTrees.Any())
                await LoadCategoryTreesAsync();

            await LoadZonesAsync();
            await SetCoordinates(
        lat: (double)selectedService.Lattitude,
        lng: (double)selectedService.Longitude
    );
            var selectedCategory = CategoryTrees.FirstOrDefault(c => c.Id == selectedService.CategoryId);
            _selectedL1Categories = selectedCategory?.L1Categories ?? new();

            var selectedL1 = _selectedL1Categories.FirstOrDefault(l1 => l1.Id == selectedService.L1CategoryId);
            _selectedL2Categories = selectedL1?.L2Categories ?? new();
        }

        protected void OnCategoryChanged(Guid categoryId)
        {
            selectedService.CategoryId = categoryId;
            _selectedL1CategoryId = null;
            _selectedL2CategoryId = null;
            _selectedL2Categories.Clear();

            var selectedCategory = CategoryTrees.FirstOrDefault(c => c.Id == categoryId);
            _selectedL1Categories = selectedCategory?.L1Categories ?? new();
        }


        protected void OnL1CategoryChanged(Guid subcategoryId)
        {
            selectedService.L1CategoryId = subcategoryId;
            _selectedL2CategoryId = null;

            var selectedL1 = _selectedL1Categories.FirstOrDefault(l1 => l1.Id == subcategoryId);
            _selectedL2Categories = selectedL1?.L2Categories ?? new();
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
        protected void PreviewFile()
        {
            if (string.IsNullOrWhiteSpace(Ad.Certificate) || string.IsNullOrWhiteSpace(Ad.CertificateFileName))
                return;

            var fileExtension = Path.GetExtension(Ad.CertificateFileName).ToLowerInvariant();
            var mimeType = fileExtension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };

            var base64 = Ad.Certificate;
            var dataUrl = $"data:{mimeType};base64,{base64}";

            // Open in new browser tab
            JSRuntime.InvokeVoidAsync("open", dataUrl, "_blank");
        }

        protected void ClearFile()
        {
            Ad.CertificateFileName = null;
            Ad.Certificate = null;
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
        protected void OnSubCategoryChanged(Guid subcategoryId)
        {
            selectedService.L2CategoryId = subcategoryId;
            Ad.SelectedSubSubcategoryId = null;
            Ad.DynamicFields.Clear();
            DynamicFieldErrors.Clear();
            editContext.NotifyValidationStateChanged();
            StateHasChanged();
        }

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


        private void ValidateDynamicField(string fieldName)
        {
            if (!DynamicFieldErrors.ContainsKey(fieldName))
                DynamicFieldErrors[fieldName] = new List<string>();

            DynamicFieldErrors[fieldName].Clear();

            if (!Ad.DynamicFields.TryGetValue(fieldName, out var value) || string.IsNullOrWhiteSpace(value))
            {
                DynamicFieldErrors[fieldName].Add($"{fieldName} is required.");
            }
        }
        protected List<string> GetDynamicFieldErrors(string fieldName)
        {
            if (DynamicFieldErrors.TryGetValue(fieldName, out var errors))
            {
                return errors;
            }
            return new List<string>();
        }
        protected override async Task OnInitializedAsync()
        {
            editContext = new EditContext(Ad);
            messageStore = new ValidationMessageStore(editContext);
        }
         [JSInvokable]
            public Task SetCoordinates(double lat, double lng)
            {
                Logger.LogInformation("Map marker moved to Lat: {Lat}, Lng: {Lng}", lat, lng);
                selectedService.Lattitude = (decimal)lat;
                selectedService.Longitude = (decimal)lng;
                editContext.NotifyFieldChanged(FieldIdentifier.Create(() => selectedService.Lattitude));
                editContext.NotifyFieldChanged(FieldIdentifier.Create(() => selectedService.Longitude));
                StateHasChanged(); 
                return Task.CompletedTask;
            }

        private async Task LoadCategoryTreesAsync()
        {
            try
            {
                var response = await _serviceService.GetServicesCategories();

                if (response is { IsSuccessStatusCode: true })
                {
                    var result = await response.Content.ReadFromJsonAsync<List<ServiceCategory>>();
                    CategoryTrees = result ?? new();
                    StateHasChanged();
                }
                else
                {
                    ErrorMessage = $"Failed to load category trees. Status: {response?.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error loading category trees.";
            }
            finally
            {
                IsLoadingCategories = false;
                StateHasChanged();
            }
        }


        protected void SubmitForm()
        {
            if (string.IsNullOrWhiteSpace(Ad.Title))
                Snackbar.Add("Title is required.", Severity.Error);

            if (Ad.Price == null && !_priceOnRequest)
                Snackbar.Add("Price is required unless 'Price on request' is checked.", Severity.Error);

            if (string.IsNullOrWhiteSpace(Ad.PhoneNumber))
                Snackbar.Add("Phone number is required.", Severity.Error);

            if (!Ad.IsAgreed)
                Snackbar.Add("You must agree to the terms and conditions.", Severity.Error);

            if (string.IsNullOrWhiteSpace(_selectedCategoryId))
                Snackbar.Add("Category must be selected.", Severity.Error);

            if (string.IsNullOrWhiteSpace(_selectedL1CategoryId))
                Snackbar.Add("Subcategory must be selected.", Severity.Error);

            if (string.IsNullOrWhiteSpace(_selectedL2CategoryId))
                Snackbar.Add("Section must be selected.", Severity.Error);
        }
        private async Task LoadZonesAsync()
        {
            try
            {
                var response = await _serviceService.GetAllZonesAsync();
                if (response?.IsSuccessStatusCode == true)
                {
                    var result = await response.Content.ReadFromJsonAsync<LocationZoneListDto>();
                    Zones = result.Zones ?? new();
                }
                else
                {
                    ErrorMessage = $"Failed to load zones. Status: {response?.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred while loading zones.";
            }
        }



    }
}
