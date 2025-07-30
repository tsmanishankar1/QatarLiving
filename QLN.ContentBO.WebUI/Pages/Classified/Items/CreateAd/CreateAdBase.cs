using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using Microsoft.JSInterop;

namespace QLN.ContentBO.WebUI.Pages.Classified.Items.CreateAd
{
    public class CreateAdBase : ComponentBase
    {
        [Inject] public NavigationManager Navigation { get; set; }
        [Inject] public IClassifiedService ClassifiedService { get; set; }
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
            Navigation.NavigateTo("/manage/classified/items/view/listing");
        }
        protected AdPost adPostModel { get; set; } = new();
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
        protected string[] AllowedFields => new[]
        {
                "Condition", "Ram", "Model", "Capacity", "Processor", "Brand",
                "Storage", "Colour", "Gender", "Resolution", "Coverage","Battery Life",
                "Size" // <== Add these here
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

            foreach (var field in AvailableFields.Where(f => AllowedFields.Contains(f.Name)))
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
                messageStore.Add(() => adPostModel.SelectedSubSubcategoryId, "Sub Subcategory is required.");
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
            foreach (var field in AvailableFields.Where(f => AllowedFields.Contains(f.Name)))
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
            Snackbar.Add("Form is valid and ready to submit!", Severity.Success);
            // Proceed with form submission
            await PostAdToApiAsync();

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
            return key.Equals("brand", StringComparison.OrdinalIgnoreCase)
                || key.Equals("model", StringComparison.OrdinalIgnoreCase)
                || key.Equals("condition", StringComparison.OrdinalIgnoreCase)
                || key.Equals("color", StringComparison.OrdinalIgnoreCase)
                || key.Equals("location", StringComparison.OrdinalIgnoreCase);
        }

        private async Task PostAdToApiAsync()
        {
            try
            {
                IsSaving = true;

                var payload = new
                {
                    adType = "Free",
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

                    brand = adPostModel.DynamicFields.GetValueOrDefault("brand"),
                    model = adPostModel.DynamicFields.GetValueOrDefault("model"),
                    condition = adPostModel.DynamicFields.GetValueOrDefault("condition"),
                    color = adPostModel.DynamicFields.GetValueOrDefault("color"),
                    location = adPostModel.DynamicFields.GetValueOrDefault("location"),

                    latitude = adPostModel.Latitude ?? 0,
                    longitude = adPostModel.Longitude ?? 0,
                    contactNumber = $"{adPostModel.PhoneCode}{adPostModel.PhoneNumber}",
                    contactEmail = UserEmail ?? "user@example.com",
                    whatsAppNumber = $"{adPostModel.WhatsappCode}{adPostModel.WhatsappNumber}",
                    streetNumber = adPostModel.StreetNumber?.ToString(),
                    buildingNumber = adPostModel.BuildingNumber?.ToString(),
                    zone = adPostModel.Zone,

                    images = adPostModel.Images
                        .Where(img => !string.IsNullOrEmpty(img.Url))
                        .OrderBy(i => i.Order)
                        .Select(img => new { url = img.Url, order = img.Order }),

                    attributes = adPostModel.DynamicFields
                        .Where(kv => !IsBasicField(kv.Key))
                        .ToDictionary(kv => kv.Key, kv => (object)kv.Value)
                };
                await JS.InvokeVoidAsync("console.log", payload);
      

                var response = await ClassifiedService.PostAdAsync("items", payload);

                if (response?.IsSuccessStatusCode == true)
                {
                    Snackbar.Add("Ad posted successfully!", Severity.Success);
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


        private async Task LoadCategoryTreesAsync()
        {
            try
            {
                var response = await ClassifiedService.GetAllCategoryTreesAsync("items");

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
