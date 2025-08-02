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

namespace QLN.ContentBO.WebUI.Pages.Classified.Collectibles.CreateAd
{
    public class CreateAdBase : ComponentBase
    {
        [Inject] public NavigationManager Navigation { get; set; }
        [Inject] public IClassifiedService ClassifiedService { get; set; }
        [Inject] public IFileUploadService FileUploadService { get; set; }
        [Inject] protected IDialogService DialogService { get; set; }

        protected List<LocationZoneDto> Zones { get; set; } = new();
        [Inject] ILogger<CreateAdBase> Logger { get; set; }
        protected bool IsLoadingZones { get; set; } = true;
        protected bool IsLoadingCategories { get; set; } = true;
        protected bool IsSaving { get; set; } = false;
        protected bool IsLoadingMap { get; set; } = false;

        protected string? ErrorMessage { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] private IJSRuntime JS { get; set; }

        protected void GoBack()
        {
            Navigation.NavigateTo("/manage/classified/collectibles/view/listing");
        }
        protected CollectiblesAdPost adPostModel { get; set; } = new();
        protected EditContext editContext;
        private ValidationMessageStore messageStore;


        protected string? UserEmail { get; set; }

        protected override async Task OnInitializedAsync()
        {
            editContext = new EditContext(adPostModel);
            messageStore = new ValidationMessageStore(editContext);

            editContext.OnValidationRequested += (_, __) =>
            {
                messageStore.Clear();
                ValidateDynamicFields();
            };

            var uri = Navigation.ToAbsoluteUri(Navigation.Uri);

            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("email", out var email))
            {
                UserEmail = email;
            }

            if (Zones == null || Zones.Count == 0)
            {
                await LoadZonesAsync();
            }

            await LoadCategoryTreesAsync();
        }

        protected List<CategoryTreeDto> CategoryTrees { get; set; } = new();
        protected CategoryTreeDto SelectedCategory => CategoryTrees.FirstOrDefault(x => x.Id.ToString() == adPostModel.SelectedCategoryId);
        protected CategoryTreeDto SelectedSubcategory => SelectedCategory?.Children?.FirstOrDefault(x => x.Id.ToString() == adPostModel.SelectedSubcategoryId);
        protected CategoryTreeDto SelectedSubSubcategory => SelectedSubcategory?.Children?.FirstOrDefault(x => x.Id.ToString() == adPostModel.SelectedSubSubcategoryId);

        protected List<CategoryField> AvailableFields =>
                                        SelectedSubSubcategory?.Fields ??
                                        SelectedSubcategory?.Fields ??
                                        SelectedCategory?.Fields ??
                                        new List<CategoryField>();
        protected string[] ExcludedFields => new[]
        {
                "L2 Category" // Add any other fields you want to hide here

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

        private void ValidateDynamicFields()
        {
            if (AvailableFields == null)
                return; // Nothing to validate yet

            foreach (var field in AvailableFields.Where(f => !ExcludedFields.Contains(f.Name)))

            {
                if (string.IsNullOrWhiteSpace(adPostModel.DynamicFields.GetValueOrDefault(field.Name)))
                {
                    messageStore.Add(new FieldIdentifier(adPostModel.DynamicFields, field.Name), $"{field.Name} is required.");
                }
            }
        }
        protected async Task SubmitForm()
        {
            messageStore.Clear();
            DynamicFieldErrors.Clear();
            // Run automatic validation
            var isValid = editContext.Validate();

            if (SelectedCategory?.Children?.Any() == true && string.IsNullOrEmpty(adPostModel.SelectedSubcategoryId))
            {
                messageStore.Add(() => adPostModel.SelectedSubcategoryId, "Subcategory is required.");
                isValid = false;
            }

            if (SelectedSubcategory?.Children?.Any() == true && string.IsNullOrEmpty(adPostModel.SelectedSubSubcategoryId))
            {
                messageStore.Add(() => adPostModel.SelectedSubSubcategoryId, "Section is required.");
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
            foreach (var field in AvailableFields.Where(f => !ExcludedFields.Contains(f.Name)))

            {
                var value = adPostModel.DynamicFields.ContainsKey(field.Name) ? adPostModel.DynamicFields[field.Name] : null;

                if (string.IsNullOrWhiteSpace(value))
                {
                    messageStore.Add(new FieldIdentifier(adPostModel.DynamicFields, field.Name), $"{field.Name} is required.");
                    if (!DynamicFieldErrors.ContainsKey(field.Name))
                        DynamicFieldErrors[field.Name] = new List<string>();
                    DynamicFieldErrors[field.Name].Add($"{field.Name} is required.");
                    isValid = false;
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
                { "Title", "Confirm Create Ad" },
                { "Descrption", "Are you sure you want to create this ad?" },
                { "ButtonTitle", "Yes, Create Ad" },
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
            await PostAdToApiAsync();
        }
          protected async Task OpenDiscardDialog()
        {
            var parameters = new DialogParameters
            {
                { "Title", "Discard Ad" },
                { "Description", "Are you sure you want to discard this Ad? Any unsaved changes will be gone." },
                { "OnDiscard", EventCallback.Factory.Create(this, HandleDiscardAsync) }
            };

                 var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = DialogService.Show<DiscardDialog>("", parameters, options);
            var result = await dialog.Result;

        }

        private void ResetFormState()
        {
            // Clear the ad model
            adPostModel = new CollectiblesAdPost();

            // Reset the EditContext with the new model
            editContext = new EditContext(adPostModel);

            // Reassign validation store and handlers
            messageStore = new ValidationMessageStore(editContext);
            editContext.OnValidationRequested += (_, __) =>
            {
                messageStore.Clear();
                ValidateDynamicFields();
            };

            DynamicFieldErrors.Clear();

            // Notify UI to re-render
            StateHasChanged();
        }
        private async Task HandleDiscardAsync()
        {
            ResetFormState();
            Snackbar.Add("Ad creation form discarded successfully.", Severity.Info);
        }


         private string GetCategoryNameById(string? id)
        {
            if (string.IsNullOrEmpty(id)) return string.Empty;

            if (SelectedCategory?.Id.ToString() == id) return SelectedCategory?.Name ?? string.Empty;
            if (SelectedSubcategory?.Id.ToString() == id) return SelectedSubcategory?.Name ?? string.Empty;
            if (SelectedSubSubcategory?.Id.ToString() == id) return SelectedSubSubcategory?.Name ?? string.Empty;

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

        private async Task PostAdToApiAsync()
        {
            try
            {
                IsSaving = true;
                var uploadedImages = await UploadImagesAsync(adPostModel.Images);
                string? certificateUrl = await UploadCertificateAsync();
                adPostModel.Certificate = certificateUrl; // save it
                var payload = new
                {
                    adType = 1,
                    title = adPostModel.Title,
                    description = adPostModel.Description,
                    price = adPostModel.Price,
                    priceType = "QAR",
                    categoryId = adPostModel.SelectedCategoryId,
                    category = GetCategoryNameById(adPostModel.SelectedCategoryId),
                    l1CategoryId = adPostModel.SelectedSubcategoryId,
                    l1Category = GetCategoryNameById(adPostModel.SelectedSubcategoryId),
                    l2CategoryId = adPostModel.SelectedSubSubcategoryId,
                    l2Category = GetCategoryNameById(adPostModel.SelectedSubSubcategoryId),

                    brand = adPostModel.DynamicFields.GetValueOrDefault("Brand"),
                    model = adPostModel.DynamicFields.GetValueOrDefault("Model"),
                    condition = adPostModel.DynamicFields.GetValueOrDefault("Condition"),
                    color = adPostModel.DynamicFields.GetValueOrDefault("Color"),
                    location = adPostModel.DynamicFields.GetValueOrDefault("Location"),

                    latitude = adPostModel.Latitude ?? 0,
                    longitude = adPostModel.Longitude ?? 0,
                    hasAuthenticityCertificate = adPostModel.HasAuthenticityCertificate,
                    authenticityCertificateUrl = adPostModel.Certificate,
                    hasWarranty = adPostModel.HasWarranty,
                    isHandmade = adPostModel.IsHandmade,
                    contactNumber = adPostModel.PhoneNumber,
                    contactNumberCountryCode = adPostModel.PhoneCode,
                    contactEmail = string.IsNullOrWhiteSpace(UserEmail) ? null : UserEmail,
                    whatsAppNumber = adPostModel.WhatsappNumber,
                    whatsappNumberCountryCode = adPostModel.WhatsappCode,
                    streetNumber = adPostModel.StreetNumber?.ToString(),
                    buildingNumber = adPostModel.BuildingNumber?.ToString(),
                    zone = adPostModel.Zone,
                    images = uploadedImages,
                    attributes = adPostModel.DynamicFields
                        .Where(kv => !IsBasicField(kv.Key))
                        .ToDictionary(kv => kv.Key, kv => (object)kv.Value)
                };
                // await JS.InvokeVoidAsync("console.log", payload);
                var response = await ClassifiedService.PostAdAsync("collectibles", payload);

                if (response?.IsSuccessStatusCode == true)
                {
                    Snackbar.Add("Ad posted successfully!", Severity.Success);
                    ResetFormState();
                }
                else
                {
                    Snackbar.Add("Failed to post ad.", Severity.Error);
                }

            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error posting ad");
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
            if (adPostModel.Certificate == null)
                return null;

            var fileUploadModel = new FileUploadModel
            {
                Container = "classifieds-images",
                File = adPostModel.Certificate
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

        private async Task LoadCategoryTreesAsync()
        {
            try
            {
                var response = await ClassifiedService.GetAllCategoryTreesAsync("collectibles");

                if (response is { IsSuccessStatusCode: true })
                {
                    var result = await response.Content.ReadFromJsonAsync<List<CategoryTreeDto>>();
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
                !adPostModel.StreetNumber.HasValue ||
                !adPostModel.BuildingNumber.HasValue)
            {
                // Logger?.LogWarning("Address fields are incomplete, skipping coordinates lookup.");
                return;
            }
            IsLoadingMap = true;
            try
            {
                var zone = int.TryParse(adPostModel.Zone, out var zoneInt) ? zoneInt : 0;
                if (zoneInt == 0) return;

                var response = await ClassifiedService.GetAddressByDetailsAsync(
                    zone: zoneInt,
                    street: adPostModel.StreetNumber.Value,
                    building: adPostModel.BuildingNumber.Value,
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
