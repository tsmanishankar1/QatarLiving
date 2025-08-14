using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Models;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using PSC.Blazor.Components.MarkdownEditor;
using PSC.Blazor.Components.MarkdownEditor.EventsArgs;
using MudExRichTextEditor;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Components.SuccessModal;
using QLN.ContentBO.WebUI.Components.FilePreviewDialog;
using System.Text.Json;
using System.Net;
using DocumentFormat.OpenXml.EMMA;
using Nextended.Core.Extensions;


namespace QLN.ContentBO.WebUI.Pages.Services.EditService
{
    public class EditServiceFormBase : ComponentBase
    {
        [Inject] public IServiceBOService _serviceService { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; }
        [Inject] public IFileUploadService FileUploadService { get; set; }
        [Inject] public IDialogService DialogService { get; set; }
        [Inject] public NavigationManager Navigation { get; set; }
        [Parameter] public EventCallback OnCommentDialogClose { get; set; }
        protected MudFileUpload<IBrowserFile> _markdownfileUploadRef;
        [Inject] public IClassifiedService ClassifiedService { get; set; }
        protected bool _priceOnRequest = false;
        protected bool IsLoadingCategories { get; set; } = true;
        protected string[] HiddenIcons = ["fullscreen"];
        protected string UploadImageButtonName { get; set; } = "uploadImage";
        protected MarkdownEditor MarkdownEditorRef;

        protected List<LocationZoneDto> Zones { get; set; } = new();
        public bool IsAgreed { get; set; } = true;
        protected string? ErrorMessage { get; set; }
        protected Double latitude = 25.32;
        protected Double Longitude = 51.54;
        protected Dictionary<string, List<string>> DynamicFieldErrors { get; set; } = new();
        private bool IsBase64String(string? base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
                return false;

            Span<byte> buffer = new Span<byte>(new byte[base64.Length]);
            return Convert.TryFromBase64String(base64, buffer, out _);
        }

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
        protected string? selectedFileName { get; set; } = string.Empty;
        protected string? _selectedL1CategoryId;
        protected string? _selectedL2CategoryId;
        private bool _shouldUpdateMap = true;

        protected List<L1Category> _selectedL1Categories = new();
        protected List<L2Category> _selectedL2Categories = new();
        protected override async Task OnParametersSetAsync()
        {
            try
            {
                if (selectedService == null)
                    return;

                if (CategoryTrees == null || !CategoryTrees.Any())
                    await LoadCategoryTreesAsync();

                await LoadZonesAsync();
                if (selectedService?.Lattitude != 0 && selectedService?.Longitude != 0)
                {
                    latitude = (double)selectedService.Lattitude;
                    Longitude = (double)selectedService.Longitude;
                    _shouldUpdateMap = true;
                     await JS.InvokeVoidAsync("updateMapCoordinates", latitude, Longitude);
                }
                selectedFileName = GetFileNameFromUrl(selectedService.LicenseCertificate);
                // var selectedCategory = CategoryTrees.FirstOrDefault(c => c.Id == selectedService?.CategoryId);
                // _selectedL1Categories = selectedCategory?.Fields ?? new();
                // var selectedL1 = _selectedL1Categories.FirstOrDefault(l1 => l1.Id == selectedService?.L1CategoryId);
                // _selectedL2Categories = selectedL1?.Fields ?? new();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnParametersSetAsync");
            }
        }
        protected void TriggerCustomImageUpload()
        {
            _markdownfileUploadRef.OpenFilePickerAsync();
        }
        protected async void ToggleMarkdownPreview()
        {
            if (MarkdownEditorRef != null)
            {
                await MarkdownEditorRef.TogglePreviewAsync();
            }
        }
        protected Task OnCustomButtonClicked(MarkdownButtonEventArgs eventArgs)
        {
            if (eventArgs.Name is not null)
            {
                if (eventArgs.Name == UploadImageButtonName)
                {
                    TriggerCustomImageUpload();
                }

                if (eventArgs.Name == "CustomPreview")
                {
                    ToggleMarkdownPreview();
                }
            }
            return Task.CompletedTask;
        }

        protected void OnCategoryChanged(long categoryId)
        {
            selectedService.CategoryId = categoryId;
            _selectedL1CategoryId = null;
            _selectedL2CategoryId = null;
            _selectedL2Categories.Clear();

            var selectedCategory = CategoryTrees.FirstOrDefault(c => c.Id == categoryId);
            // _selectedL1Categories = selectedCategory?.Fields ?? new();
        }


        protected void OnL1CategoryChanged(long subcategoryId)
        {
            selectedService.L1CategoryId = subcategoryId;
            _selectedL2CategoryId = null;

            var selectedL1 = _selectedL1Categories.FirstOrDefault(l1 => l1.Id == subcategoryId);
            // _selectedL2Categories = selectedL1?.Fields ?? new();
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
            selectedService.LicenseCertificate = Convert.ToBase64String(ms.ToArray());
            selectedFileName = file.Name;
        }
        private string GetFileNameFromUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            try
            {
                return Path.GetFileName(new Uri(url).AbsolutePath);
            }
            catch
            {
                return string.Empty;
            }
        }
        protected async Task PreviewFile(string fileUrl)
        {
            var parameters = new DialogParameters
            {
                ["PdfUrl"] = fileUrl
            };

            var options = new DialogOptions { MaxWidth = MaxWidth.ExtraLarge, FullWidth = true };

            await DialogService.ShowAsync<FilePreviewDialog>("File Preview", parameters, options);
        }


        protected void ClearFile()
        {
            selectedService.LicenseCertificate = null;
            selectedFileName = null;
        }

        protected Task OnPhoneCountryChanged(CountryModel model)
        {
            SelectedPhoneCountry = model;
            selectedService.PhoneNumberCountryCode = model.Code;
            return Task.CompletedTask;
        }

        protected Task OnWhatsappCountryChanged(CountryModel model)
        {
            SelectedWhatsappCountry = model;
            selectedService.WhatsappNumberCountryCode = model.Code;
            return Task.CompletedTask;
        }
        protected Task OnPhoneChanged(string phone)
        {
            selectedService.PhoneNumber = phone;
            return Task.CompletedTask;
        }

        protected Task OnWhatsappChanged(string phone)
        {
            selectedService.WhatsappNumber = phone;
            return Task.CompletedTask;
        }
        protected void OnSubCategoryChanged(long subcategoryId)
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
            try
            {
                if (_shouldUpdateMap)
                {
                    _shouldUpdateMap = false;
                    _dotNetRef = DotNetObjectReference.Create(this);
                    await JS.InvokeVoidAsync("resetLeafletMap");
                    await JS.InvokeVoidAsync("initializeMap", _dotNetRef);
                    await Task.Delay(300);
                    await JS.InvokeVoidAsync("updateMapCoordinates", latitude, Longitude);

                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnAfterRenderAsync");
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
            try
            {
                editContext = new EditContext(Ad);
                messageStore = new ValidationMessageStore(editContext);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnInitializedAsync");
            }

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
        private async Task ShowSuccessModal(string title)
        {
            var parameters = new DialogParameters
            {
                { nameof(SuccessModalBase.Title), title },
            };

            var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.ExtraSmall,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<SuccessModal>("", parameters, options);
            var result = await dialog.Result;
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
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "LoadCategoryTreesAsync");
            }
            finally
            {
                IsLoadingCategories = false;
                StateHasChanged();
            }
        }


        protected async Task SubmitForm()
        {
            if (string.IsNullOrWhiteSpace(selectedService.Title))
                Snackbar.Add("Title is required.", Severity.Error);

            if (selectedService.Price == null && !selectedService.IsPriceOnRequest)
                Snackbar.Add("Price is required unless 'Price on request' is checked.", Severity.Error);

            if (string.IsNullOrWhiteSpace(selectedService.PhoneNumber))
                Snackbar.Add("Phone number is required.", Severity.Error);

            if (!IsAgreed)
                Snackbar.Add("You must agree to the terms and conditions.", Severity.Error);

            if (string.IsNullOrWhiteSpace(selectedService.CategoryId.ToString()))
                Snackbar.Add("Category must be selected.", Severity.Error);

            if (string.IsNullOrWhiteSpace(selectedService.L1CategoryId.ToString()))
                Snackbar.Add("Subcategory must be selected.", Severity.Error);

            if (string.IsNullOrWhiteSpace(selectedService.L2CategoryId.ToString()))
                Snackbar.Add("Section must be selected.", Severity.Error);
            if (!IsAgreed)
                Snackbar.Add("Please agree to the terms and conditions before proceeding.", Severity.Error);
            try
            {
                if (selectedService?.PhotoUpload != null)
                {
                    selectedService.PhotoUpload = await UploadImagesAsync(selectedService.PhotoUpload);
                }
                if (IsBase64String(selectedService?.LicenseCertificate))
                {
                    string? certificateUrl = await UploadCertificateAsync();
                    selectedService.LicenseCertificate = certificateUrl;
                }
                var response = await _serviceService.UpdateService(selectedService);
                if (response != null && response.IsSuccessStatusCode)
                {
                    await ShowSuccessModal("Service Ad Updated Successfully");
                    await JS.InvokeVoidAsync("resetLeafletMap");
                    await JS.InvokeVoidAsync("initializeMap", _dotNetRef);
                    var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true };
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Snackbar.Add("You are unauthorized to perform this action");
                }
                else if (response.StatusCode == HttpStatusCode.InternalServerError)
                {
                    Snackbar.Add("Internal API Error");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SubmitForm");
            }
        }
        private async Task<List<ImageDto>> UploadImagesAsync(List<ImageDto> images)
        {
            var uploadedImages = new List<ImageDto>();

            var orderedImages = images
                .Where(img => !string.IsNullOrWhiteSpace(img.Url))
                .OrderBy(img => img.Order)
                .ToList();

            for (int i = 0; i < orderedImages.Count; i++)
            {
                var image = orderedImages[i];

                if (IsBlobUrl(image.Url))
                {
                    uploadedImages.Add(new ImageDto
                    {
                        Url = image.Url,
                        Order = i
                    });
                    continue;
                }

                var uploadPayload = new FileUploadModel
                {
                    Container = "services-images",
                    File = image.Url
                };

                var uploadResponse = await FileUploadService.UploadFileAsync(uploadPayload);

                if (uploadResponse.IsSuccessStatusCode)
                {
                    var result = await uploadResponse.Content.ReadFromJsonAsync<FileUploadResponseDto>();

                    if (result?.IsSuccess == true)
                    {
                        uploadedImages.Add(new ImageDto
                        {
                            Url = result.FileUrl,
                            Order = i
                        });
                    }
                    else
                    {
                        Logger.LogWarning("Image upload failed: {Message}", result?.Message);
                    }
                }
                else
                {
                    Logger.LogWarning("Image upload HTTP error at index {Index}", i);
                }
            }

            return uploadedImages;
        }

        private bool IsBlobUrl(string url)
        {
            return url.Contains(".blob.core.windows.net", StringComparison.OrdinalIgnoreCase);
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
                Logger.LogError(ex, "LoadZonesAsyn");
            }
        }
        private async Task<List<object>> UploadImagesAsync(List<AdImage> images)
        {
            var uploadedImages = new List<object>();

            var orderedImages = images
                .Where(img => !string.IsNullOrWhiteSpace(img.Url))
                .OrderBy(img => img.Order)
                .ToList();

            for (int i = 0; i < orderedImages.Count; i++)
            {
                var image = orderedImages[i];

                var uploadPayload = new FileUploadModel
                {
                    Container = "services-images",
                    File = image.Url
                };

                var uploadResponse = await FileUploadService.UploadFileAsync(uploadPayload);

                if (uploadResponse.IsSuccessStatusCode)
                {
                    var result = await uploadResponse.Content.ReadFromJsonAsync<FileUploadResponseDto>();

                    if (result?.IsSuccess == true)
                    {
                        uploadedImages.Add(new
                        {
                            url = result.FileUrl,
                            order = i
                        });
                    }
                    else
                    {
                        Logger.LogWarning("Image upload failed: " + result?.Message);
                    }
                }
                else
                {
                    Logger.LogWarning("Image upload HTTP error at index " + i);
                }
            }

            return uploadedImages;
        }
        private async Task<string?> UploadCertificateAsync()
        {
            if (selectedService.LicenseCertificate == null)
                return null;

            var fileUploadModel = new FileUploadModel
            {
                Container = "services-images",
                File = selectedService.LicenseCertificate
            };

            var uploadResponse = await FileUploadService.UploadFileAsync(fileUploadModel);

            if (uploadResponse.IsSuccessStatusCode)
            {
                var result = await uploadResponse.Content.ReadFromJsonAsync<FileUploadResponseDto>();
                if (result?.IsSuccess == true)
                {
                    return result.FileUrl;
                }
                Logger.LogWarning("Certificate upload failed: " + result?.Message);
            }
            else
            {
                Logger.LogWarning("Certificate upload HTTP error");
            }

            return null;
        }
        protected async Task OnZoneChanged(string zoneId)
        {
            var isChanged = selectedService.ZoneId != zoneId;
            selectedService.ZoneId = zoneId;

            if (isChanged)
            {
                await TrySetCoordinatesFromAddressAsync();
            }
        }

        protected async Task OnStreetNumberChanged(string? street)
        {
            var changed = selectedService.StreetNumber != street;
            selectedService.StreetNumber = street;

            if (!string.IsNullOrEmpty(selectedService.ZoneId) && changed)
            {
                await TrySetCoordinatesFromAddressAsync();
            }
        }

        protected async Task OnBuildingNumberChanged(string? building)
        {
            var changed = selectedService.BuildingNumber != building;
            selectedService.BuildingNumber = building;

            if (!string.IsNullOrEmpty(selectedService.ZoneId) && changed)
            {
                await TrySetCoordinatesFromAddressAsync();
            }
        }
        protected async Task TrySetCoordinatesFromAddressAsync()
        {
            // Ensure all required fields are available
            if (string.IsNullOrWhiteSpace(selectedService.ZoneId) ||
                string.IsNullOrWhiteSpace(selectedService.StreetNumber) ||
                string.IsNullOrWhiteSpace(selectedService.BuildingNumber))
            {
                return;
            }

            try
            {
                var zone = int.TryParse(selectedService.ZoneId, out var zoneInt) ? zoneInt : 0;
                if (zoneInt == 0) return;

                HttpResponseMessage? response = null;

                if (int.TryParse(selectedService.StreetNumber, out int streetNumber) &&
                    int.TryParse(selectedService.BuildingNumber, out int buildingNumber))
                {
                    response = await ClassifiedService.GetAddressByDetailsAsync(
                        zone: zoneInt,
                        street: streetNumber,
                        building: buildingNumber,
                        location: ""
                    );
                }

                if (response?.IsSuccessStatusCode == true)
                {
                    var coords = await response.Content.ReadFromJsonAsync<List<string>>();

                    if (coords is { Count: 2 } &&
                        double.TryParse(coords[0], out var latitude) &&
                        double.TryParse(coords[1], out var longitude))
                    {
                        selectedService.Lattitude = (decimal)latitude;
                        selectedService.Longitude = (decimal)longitude;

                        await JS.InvokeVoidAsync("updateMapCoordinates", latitude, longitude);
                    }
                }
                else
                {
                    Logger?.LogWarning("Failed to fetch coordinates from API.");
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Error fetching coordinates from address.");
            }
        }

    }
}
