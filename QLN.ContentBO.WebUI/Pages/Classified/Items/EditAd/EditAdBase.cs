using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using Microsoft.JSInterop;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components.DiscardDialog;

namespace QLN.ContentBO.WebUI.Pages.Classified.Items.EditAd
{
    public class EditAdBase : ComponentBase
    {
        [Inject] public NavigationManager Navigation { get; set; }
        [Inject] public IClassifiedService ClassifiedService { get; set; }
        [Inject] public IFileUploadService FileUploadService { get; set; }
        [Inject] protected IDialogService DialogService { get; set; }

        protected List<LocationZoneDto> Zones { get; set; } = new();
        [Inject] ILogger<EditAdBase> Logger { get; set; }
        protected bool IsLoadingZones { get; set; } = true;
        protected bool IsLoadingCategories { get; set; } = true;
        protected bool IsSaving { get; set; } = false;
        protected bool IsLoadingMap { get; set; } = false;
        protected bool IsLoadingId { get; set; } = true;

        protected string? ErrorMessage { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] private IJSRuntime JS { get; set; }

        protected void GoBack()
        {
            Navigation.NavigateTo("/manage/classified/items/view/listing");
        }
        protected ItemEditAdPost adPostModel { get; set; } = new();
        protected EditContext editContext;
        private ValidationMessageStore messageStore;

        [Parameter] public long Id { get; set; }
        protected string? DefaultSelectedPhoneCountry { get; set; }
        protected string? DefaultSelectedWhatsappCountry { get; set; }
        public void SetDefaultDynamicFieldsFromApi()
        {
            var mainFields = new Dictionary<string, string?>
            {
                { "Location", adPostModel.Location },
                { "Brand", adPostModel.Brand },
                { "Model", adPostModel.Model },
                { "Condition", adPostModel.Condition },
                { "Color", adPostModel.Color }
            };

            foreach (var field in mainFields)
            {
                if (!string.IsNullOrWhiteSpace(field.Value) &&
                    AvailableFields.Any(f => string.Equals(f.CategoryName, field.Key, StringComparison.OrdinalIgnoreCase)))
                {
                    adPostModel.DynamicFields[field.Key] = field.Value!;
                }
            }

            if (adPostModel.Attributes != null)
            {
                foreach (var attribute in adPostModel.Attributes)
                {
                    if (!string.IsNullOrWhiteSpace(attribute.Value) &&
                        AvailableFields.Any(f => string.Equals(f.CategoryName, attribute.Key, StringComparison.OrdinalIgnoreCase)))
                    {
                        adPostModel.DynamicFields[attribute.Key] = attribute.Value;
                    }
                }
            }
        }

        private async Task LoadAdDataAsync()
        {
            try
            {
                IsLoadingId = true;
                var response = await ClassifiedService.GetAdByIdAsync(Id);
                if (response is { IsSuccessStatusCode: true })
                {
                    var json = await response.Content.ReadAsStringAsync();
                    adPostModel = JsonSerializer.Deserialize<ItemEditAdPost>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new ItemEditAdPost();
                    DefaultSelectedPhoneCountry = adPostModel.ContactNumberCountryCode;
                    DefaultSelectedWhatsappCountry = adPostModel.WhatsappNumberCountryCode;
                    SetDefaultDynamicFieldsFromApi();
                    InitializeEditContext();
                    var modelJson = JsonSerializer.Serialize(adPostModel, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    // await JS.InvokeVoidAsync("console.log", modelJson);

                }
                else
                {
                    // Handle 404 or error gracefully
                    GoBack();
                    Snackbar.Add("Please check back later. There was an issue fetching the ad.", Severity.Error);
                    adPostModel = new ItemEditAdPost();
                }
            }
            catch (JsonException jsonEx)
            {
                // Log and fallback if deserialization fails
                GoBack();
                Snackbar.Add("Please check back later. There was an issue fetching the ad.", Severity.Error);
                adPostModel = new ItemEditAdPost();
            }
            catch (Exception ex)
            {
                // General fallback
                GoBack();
                Snackbar.Add("Please check back later. There was an issue fetching the ad.", Severity.Error);
                adPostModel = new ItemEditAdPost();
            } finally {
                IsLoadingId = false;
            }
        }
        protected override async Task OnParametersSetAsync()
        {
            await LoadAdDataAsync();
        }
        private void InitializeEditContext()
        {
            editContext = new EditContext(adPostModel);
            messageStore = new ValidationMessageStore(editContext);

            editContext.OnValidationRequested += (_, __) =>
            {
                messageStore.Clear();
                ValidateDynamicFields();
            };
        }


        protected override async Task OnInitializedAsync()
        {
            InitializeEditContext();
            if (Zones == null || Zones.Count == 0)
            {
                await LoadZonesAsync();
            }

            await LoadCategoryTreesAsync();
        }


        protected List<ClassifiedsCategory> CategoryTrees { get; set; } = new();
        protected ClassifiedsCategory SelectedCategory =>
                CategoryTrees.FirstOrDefault(x => x.Id == adPostModel.CategoryId)
                ?? new ClassifiedsCategory();
        protected ClassifiedsCategoryField? SelectedSubcategory =>
    SelectedCategory?.Fields?.FirstOrDefault(x => x.Id == adPostModel.L1CategoryId);

protected ClassifiedsCategoryField? SelectedSubSubcategory =>
    SelectedSubcategory?.Fields?.FirstOrDefault(x => x.Id == adPostModel.L2CategoryId);
        protected List<ClassifiedsCategoryField> AvailableFields =>
                                        SelectedSubSubcategory?.Fields ??
                                        SelectedSubcategory?.Fields ??
                                        SelectedCategory?.Fields ??
                                        new List<ClassifiedsCategoryField>();
        protected string[] ExcludedFields => new[]
        {
            ""// Add any other fields you want to hide here
        };

        protected Dictionary<string, List<string>> DynamicFieldErrors { get; set; } = new();

        protected List<string> GetDynamicFieldErrors(string fieldName)
        {
            if (DynamicFieldErrors.TryGetValue(fieldName, out var errors))
            {
                return errors;
            }
            return new List<string>();
        }

        // private void ValidateDynamicFields()
        // {
        //     if (AvailableFields == null)
        //         return; 

        //     foreach (var field in AvailableFields
        //         .Where(f => !ExcludedFields.Contains(f.CategoryName) &&
        //                     (string.Equals(f.Type, "dropdown", StringComparison.OrdinalIgnoreCase) ||
        //                      string.Equals(f.Type, "string", StringComparison.OrdinalIgnoreCase))))
        //     {
        //         Console.WriteLine($"Validating Field: {field.CategoryName}, Type: {field.Type}");
        //         if (string.IsNullOrWhiteSpace(adPostModel.DynamicFields.GetValueOrDefault(field.CategoryName)))
        //         {
        //             messageStore.Add(
        //                 new FieldIdentifier(adPostModel.DynamicFields, field.CategoryName),
        //                 $"{field.CategoryName} is required."
        //             );
        //         }
        //     }
        // }
        private void ValidateDynamicFields()
        {
            if (AvailableFields == null)
                return; 

            foreach (var field in AvailableFields.Where(f => ExcludedFields.Contains(f.Type)))
            {
                if (field.Type == "dropdown" || field.Type == "Dropdown" || field.Type == "string")
                {
                    if (string.IsNullOrWhiteSpace(adPostModel.DynamicFields.GetValueOrDefault(field.CategoryName)))
                    {
                        messageStore.Add(new FieldIdentifier(adPostModel.DynamicFields, field.CategoryName), $"{field.CategoryName} is required.");
                    }
                }
            }
        }

        protected async Task SubmitForm()
        {
            messageStore.Clear();
            DynamicFieldErrors.Clear();
            // Run automatic validation
            var isValid = editContext.Validate();

            if (SelectedCategory?.Fields?.Any() == true && adPostModel.L1CategoryId == null)
            {
                messageStore.Add(() => adPostModel.L1CategoryId, "Subcategory is required.");
                isValid = false;
            }

            if (SelectedSubcategory?.Fields?.Any() == true && adPostModel.L2CategoryId == null)
            {
                messageStore.Add(() => adPostModel.L2CategoryId, "Section is required.");
                isValid = false;
            }
            int imagesWithUrlCount = adPostModel.Images.Count(i => !string.IsNullOrEmpty(i.Url));

            if (imagesWithUrlCount < 4)
            {
                messageStore.Add(() => adPostModel.Images, "Minimum 4 images are required.");
                isValid = false;
            }
            else if (imagesWithUrlCount > 9)
            {
                messageStore.Add(() => adPostModel.Images, "Maximum 9 images allowed.");
                isValid = false;
            }

            // Manual validation: Dynamic fields
            foreach (var field in AvailableFields.Where(f => !ExcludedFields.Contains(f.CategoryName)))
            {
                if (field.Type == "dropdown" || field.Type == "Dropdown" || field.Type == "string")
                {
                    var value = adPostModel.DynamicFields.ContainsKey(field.CategoryName) ? adPostModel.DynamicFields[field.CategoryName] : null;

                    if (string.IsNullOrWhiteSpace(value))
                    {
                        messageStore.Add(new FieldIdentifier(adPostModel.DynamicFields, field.CategoryName), $"{field.CategoryName} is required.");
                        if (!DynamicFieldErrors.ContainsKey(field.CategoryName))
                            DynamicFieldErrors[field.CategoryName] = new List<string>();
                        DynamicFieldErrors[field.CategoryName].Add($"{field.CategoryName} is required.");
                        isValid = false;
                    }
                }
            }

            // Show the errors
            editContext.NotifyValidationStateChanged();

            if (!isValid)
            {
                Snackbar.Add("Please check highlighted fields before creating ad.", Severity.Error);
                return;
            }
            // All good!
            // Snackbar.Add("Form is valid and ready to submit!", Severity.Success);
            // Proceed with form submission
            await OpenConfirmationDialog();

        }
        private async Task OpenConfirmationDialog()
        {
           var parameters = new DialogParameters
            {
                { "Title", "Confirm Update Ad" },
                { "Descrption", "Are you sure you want to update this ad?" },
                { "ButtonTitle", "Yes, Update Ad" },
                { "OnConfirmed", EventCallback.Factory.Create(this, HandleAdConfirmedAsync) }
            };

            var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = DialogService.Show<ConfirmationDialog>("", parameters, options);
            var result = await dialog.Result;
        }
         private async Task HandleAdConfirmedAsync()
        {
            await UpdateAdToApiAsync();
        }
       

         private string GetCategoryNameById(long? id)
        {
            if (id == null) return string.Empty;

            if (SelectedCategory?.Id == id) return SelectedCategory?.CategoryName ?? string.Empty;
            if (SelectedSubcategory?.Id == id) return SelectedSubcategory?.CategoryName ?? string.Empty;
            if (SelectedSubSubcategory?.Id == id) return SelectedSubSubcategory?.CategoryName ?? string.Empty;

            return string.Empty;
        }

        private bool IsBasicField(string key)
        {
            // Exclude these from the attributes payload; they go in root fields
            return key.Equals("Brand", StringComparison.OrdinalIgnoreCase)
                || key.Equals("Model", StringComparison.OrdinalIgnoreCase)
                || key.Equals("Condition", StringComparison.OrdinalIgnoreCase)
                || key.Equals("Color", StringComparison.OrdinalIgnoreCase)
                || key.Equals("Location", StringComparison.OrdinalIgnoreCase);
        }

        private async Task UpdateAdToApiAsync()
        {
            try
            {
                IsSaving = true;
                var uploadedImages = await UploadImagesAsync(adPostModel.Images);
                var payload = new
                {
                    id = adPostModel.Id,
                    adType = adPostModel.AdType,
                    subVertical = adPostModel.SubVertical,
                    title = adPostModel.Title,
                    description = adPostModel.Description,
                    price = adPostModel.Price,
                    priceType = "QAR",
                    categoryId = adPostModel.CategoryId,
                    category = GetCategoryNameById(adPostModel.CategoryId),
                    l1CategoryId = adPostModel.L1CategoryId,
                    l1Category = GetCategoryNameById(adPostModel.L1CategoryId),
                    l2CategoryId = adPostModel.L2CategoryId,
                    l2Category = GetCategoryNameById(adPostModel.L2CategoryId),

                    brand = adPostModel.DynamicFields.GetValueOrDefault("Brand"),
                    model = adPostModel.DynamicFields.GetValueOrDefault("Model"),
                    condition = adPostModel.DynamicFields.GetValueOrDefault("Condition"),
                    color = adPostModel.DynamicFields.GetValueOrDefault("Color"),
                    location = adPostModel.DynamicFields.GetValueOrDefault("Location"),

                    latitude = adPostModel.Latitude ?? 0,
                    longitude = adPostModel.Longitude ?? 0,
                    contactNumberCountryCode = adPostModel.ContactNumberCountryCode,
                    contactNumber = adPostModel.ContactNumber,
                    contactEmail = adPostModel.ContactEmail,
                    whatsAppNumber = adPostModel.WhatsappNumber,
                    whatsappNumberCountryCode = adPostModel.WhatsappNumberCountryCode,
                    streetNumber = adPostModel.StreetNumber,
                    buildingNumber = adPostModel.BuildingNumber,
                    zone = adPostModel.Zone,
                    images = uploadedImages,
                    attributes = adPostModel.DynamicFields
                        .Where(kv => !IsBasicField(kv.Key))
                        .ToDictionary(kv => kv.Key, kv => (object)kv.Value),
                };
                await JS.InvokeVoidAsync("console.log", payload);
      

                var response = await ClassifiedService.UpdateAdAsync("items", payload);

                if (response?.IsSuccessStatusCode == true)
                {
                    Snackbar.Add("Ad Updated successfully!", Severity.Success);
                    GoBack();
                }
                else
                {
                    Snackbar.Add("Failed to update ad.", Severity.Error);
                }

            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error update ad");
                Snackbar.Add("Unexpected error occurred while posting your ad.", Severity.Error);
            }
            finally
            {
                IsSaving = false;
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

                if (IsBlobUrl(image.Url))
                {
                    // Logger.LogInformation("Skipping image upload for already hosted URL: {Url}", image.Url);
                    uploadedImages.Add(new
                    {
                        url = image.Url,
                        order = i
                    });
                    continue;
                }

                var uploadPayload = new FileUploadModel
                {
                    Container = "classifieds-images",
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


        private async Task LoadCategoryTreesAsync()
        {
            try
            {
                var response = await ClassifiedService.GetServicesCategories(Vertical.Classifieds, SubVertical.Items);

                if (response is { IsSuccessStatusCode: true })
                {
                    var result = await response.Content.ReadFromJsonAsync<List<ClassifiedsCategory>>();
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
        private async Task LoadZonesAsync()
        {
            try
            {
                var response = await ClassifiedService.GetAllZonesAsync();

                if (response?.IsSuccessStatusCode == true)
                {
                    var result = await response.Content.ReadFromJsonAsync<LocationZoneListDto>();
                    Zones = result?.Zones ?? new();
                }
                else
                {
                    ErrorMessage = $"Failed to load zones. Status: {response?.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading zones: " + ex.Message);
                ErrorMessage = "An error occurred while loading zones.";
            }
            finally
            {
                IsLoadingZones = false;
            }
        }
           protected async Task TrySetCoordinatesFromAddressAsync()
        {
            // Ensure all required fields are available
            if (string.IsNullOrWhiteSpace(adPostModel.Zone) ||
                string.IsNullOrWhiteSpace(adPostModel.StreetNumber) ||
                string.IsNullOrWhiteSpace(adPostModel.BuildingNumber))
            {
                // Logger?.LogWarning("Address fields are incomplete, skipping coordinates lookup.");
                return;
            }

            IsLoadingMap = true;
            try
            {
                // Try parsing Zone to integer
                var zone = int.TryParse(adPostModel.Zone, out var zoneInt) ? zoneInt : 0;
                if (zoneInt == 0) return;

                // Parse StreetNumber and BuildingNumber to integers
                var streetNumberInt = int.TryParse(adPostModel.StreetNumber, out var streetInt) ? streetInt : 0;
                var buildingNumberInt = int.TryParse(adPostModel.BuildingNumber, out var buildingInt) ? buildingInt : 0;

                // Check if the parsed values are valid integers (not 0)
                if (streetNumberInt == 0 || buildingNumberInt == 0)
                {
                    Logger?.LogWarning("StreetNumber or BuildingNumber is invalid.");
                    return;
                }

                // Call the service with the parsed integer values
                var response = await ClassifiedService.GetAddressByDetailsAsync(
                    zone: zoneInt,
                    street: streetNumberInt,
                    building: buildingNumberInt,
                    location: ""
                );

                if (response?.IsSuccessStatusCode == true)
                {
                    var coords = await response.Content.ReadFromJsonAsync<List<string>>();

                    if (coords is { Count: 2 } &&
                        double.TryParse(coords[0], out var latitude) &&
                        double.TryParse(coords[1], out var longitude))
                    {
                        adPostModel.Latitude = latitude;
                        adPostModel.Longitude = longitude;

                        // Logger?.LogInformation($"Coordinates set: {latitude}, {longitude}");
                        // Trigger JS to update the map without manual interaction
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
            finally
            {
                IsLoadingMap = false;
            }
        }

    }
}

