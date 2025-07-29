using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor;
using QLN.Common.DTO_s;
using QLN.Web.Shared.Components;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services.Interface;
using System.Net.Http.Json;
using System.Text.Json;

namespace QLN.Web.Shared.Pages.Classifieds.CreatePost
{
    public class CreatePostComponentBase : QLComponentBase
    {
        [Inject] private IClassifiedsServices _classifiedsService { get; set; } = default!;
        [Inject] private ILogger<CreatePostComponentBase> Logger { get; set; }
        protected List<QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem> breadcrumbItems = new();
        protected bool IsLoadingCategories { get; set; } = true;
        protected string? ErrorMessage { get; set; }
        protected List<CategoryTreeDto> CategoryTrees { get; set; } = new();
        protected AdPost adPostModel = new();
        protected EditContext? editContext;
        private ValidationMessageStore? messageStore;

        protected bool IsSaving { get; set; } = false;
        protected bool IsLoadingMap { get; set; } = false;

        [Inject] protected ISnackbar Snackbar { get; set; }
        protected List<LocationDto.LocationZoneDto> Zones { get; set; } = new();
        private bool IsLoadingZones { get; set; } = true;
        protected string selectedVertical;

        private void HandleValidationRequested(object? sender, ValidationRequestedEventArgs args)
        {
            messageStore?.Clear();

            if (adPostModel == null)
            {
                Logger?.LogWarning("adPostModel is null during validation.");
                return;
            }

            // Vertical type
            var vertical = adPostModel.SelectedVertical?.ToLowerInvariant();

            // Shared validations
            if (string.IsNullOrWhiteSpace(adPostModel.Title))
                messageStore.Add(() => adPostModel.Title, "Title is required.");

            if (!adPostModel.IsAgreed)
                messageStore.Add(() => adPostModel.IsAgreed, "You must agree to the terms.");

            // Deals-specific validation
            if (vertical == "deals")
            {
                if (string.IsNullOrWhiteSpace(adPostModel.FlyerLocation))
                    messageStore.Add(() => adPostModel.FlyerLocation, "Flyer Location is required for Deals.");

                if (string.IsNullOrWhiteSpace(adPostModel.Certificate))
                    messageStore.Add(() => adPostModel.Certificate, "Certificate is required for Deals.");

                if (string.IsNullOrWhiteSpace(adPostModel.PhoneNumber))
                    messageStore.Add(() => adPostModel.PhoneNumber, "Phone number is required or must be a valid number.");

                if (string.IsNullOrWhiteSpace(adPostModel.WhatsappNumber))
                    messageStore.Add(() => adPostModel.WhatsappNumber, "WhatsApp number is required or must be a valid number.");

                var photoUrlsField = new FieldIdentifier(adPostModel, nameof(adPostModel.PhotoUrls));

                if (adPostModel.PhotoUrls == null || adPostModel.PhotoUrls.Count(url => !string.IsNullOrWhiteSpace(url)) < 1)
                {
                    messageStore.Add(photoUrlsField, "Please select at least 1 images.");
                }

            }

            // Items-specific validation
            else if (vertical == "items")
            {
                if (adPostModel.BatteryPercentage < 1 || adPostModel.BatteryPercentage > 100)
                {
                    messageStore.Add(new FieldIdentifier(adPostModel, nameof(adPostModel.BatteryPercentage)), "Battery Percentage must be between 1 and 100.");
                }

            }

            // Common verticals (non-deals)
            if (vertical != "deals")
            {
                if (string.IsNullOrWhiteSpace(adPostModel.SelectedCategoryId))
                    messageStore.Add(() => adPostModel.SelectedCategoryId, "Category is required.");

                // Attempt to resolve category tree nodes
                var selectedCategory = CategoryTrees.FirstOrDefault(x => x.Id.ToString() == adPostModel.SelectedCategoryId);
                var selectedSubcategory = selectedCategory?.Children?.FirstOrDefault(x => x.Id.ToString() == adPostModel.SelectedSubcategoryId);
                var selectedSubSubcategory = selectedSubcategory?.Children?.FirstOrDefault(x => x.Id.ToString() == adPostModel.SelectedSubSubcategoryId);

                // Subcategory required if category has children
                if (selectedCategory?.Children?.Any() == true && string.IsNullOrWhiteSpace(adPostModel.SelectedSubcategoryId))
                {
                    messageStore.Add(() => adPostModel.SelectedSubcategoryId, "Subcategory is required.");
                }

                // Sub-Subcategory required if subcategory has children
                if (selectedSubcategory?.Children?.Any() == true && string.IsNullOrWhiteSpace(adPostModel.SelectedSubSubcategoryId))
                {
                    messageStore.Add(() => adPostModel.SelectedSubSubcategoryId, "Sub Subcategory is required.");
                }

                if (string.IsNullOrWhiteSpace(adPostModel.ItemDescription))
                    messageStore.Add(() => adPostModel.ItemDescription, "Description is required.");

                if (string.IsNullOrWhiteSpace(adPostModel.PhoneNumber))
                    messageStore.Add(() => adPostModel.PhoneNumber, "Phone number is required or must be a valid number.");

                if (string.IsNullOrWhiteSpace(adPostModel.WhatsappNumber))
                    messageStore.Add(() => adPostModel.WhatsappNumber, "WhatsApp number is required or must be a valid number.");

                if (string.IsNullOrWhiteSpace(adPostModel.Certificate))
                    messageStore.Add(() => adPostModel.Certificate, "Certificate is required.");

                if (string.IsNullOrWhiteSpace(adPostModel.Zone))
                    messageStore.Add(() => adPostModel.Zone, "Zone is required.");

                if (adPostModel.StreetNumber is null)
                    messageStore.Add(() => adPostModel.StreetNumber, "Street Number is required.");

                if (adPostModel.BuildingNumber is null)
                    messageStore.Add(() => adPostModel.BuildingNumber, "Building Number is required.");


                if (adPostModel.Price <= 0)
                    messageStore.Add(() => adPostModel.Price, "Price must be greater than 0.");
                else if (adPostModel.Price.ToString().Length > 10)
                    messageStore.Add(() => adPostModel.Price, "Price cannot exceed 10 digits.");

                var photoUrlsField = new FieldIdentifier(adPostModel, nameof(adPostModel.PhotoUrls));

                if (adPostModel.PhotoUrls == null || adPostModel.PhotoUrls.Count(url => !string.IsNullOrWhiteSpace(url)) < 4)
                {
                    messageStore.Add(photoUrlsField, "Please select at least 4 images.");
                }

                if (!string.IsNullOrWhiteSpace(adPostModel.Title) && adPostModel.Title.Length > 50)
                    messageStore.Add(() => adPostModel.Title, "Title cannot exceed 50 characters.");

                if (!string.IsNullOrWhiteSpace(adPostModel.Zone) && adPostModel.Zone.Length > 50)
                    messageStore.Add(() => adPostModel.Zone, "Zone cannot exceed 50 characters.");
            }
            // Add similar logs for every other validation rule...
            editContext?.NotifyValidationStateChanged();
        }

        protected override void OnInitialized()
        {
            AuthorizedPage();

            adPostModel ??= new AdPost();

            // Initialize EditContext only once
            editContext = new EditContext(adPostModel);

            breadcrumbItems = new()
        {
            new() { Label = "Classifieds", Url = "/qln/classifieds" },
            new() { Label = "Create Form", Url = "/qln/classifieds/createform", IsLast = true }
        };
        }

        private Dictionary<string, string> dynamicFieldValues = new(); // Dynamic field values
        private void ResetForm(string newVertical)
        {
            adPostModel = new AdPost
            {
                SelectedVertical = newVertical
            };
            editContext = new EditContext(adPostModel);
            messageStore = new ValidationMessageStore(editContext);
            editContext.OnValidationRequested += HandleValidationRequested;

            dynamicFieldValues = new Dictionary<string, string>();
            selectedVertical = newVertical;
        }

        protected async void HandleCategoryChanged(string newValue)
        {
            ResetForm(newValue);
            await LoadCategoryTreesAsync();
            StateHasChanged();
        }


        public async Task LogObjectToConsoleAsync<T>(T obj)
        {
            var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            // await JSRuntime.InvokeVoidAsync("eval", $"console.log({json})");
        }

        private void EnsureCustomValidationAttached()
        {
            if (editContext != null && messageStore == null)
            {
                messageStore = new ValidationMessageStore(editContext);
                editContext.OnValidationRequested += HandleValidationRequested;
                Logger?.LogInformation("Custom validation attached.");
            }
        }


        protected async Task HandleSubmit(EditContext context)
        {
            EnsureCustomValidationAttached();
            var isValid = context.Validate();

            if (!isValid)
            {
                var validationMessages = new List<(string FieldName, string Message)>();

                var model = context.Model;
                var properties = model.GetType().GetProperties();

                foreach (var property in properties)
                {
                    var fieldIdentifier = new FieldIdentifier(model, property.Name);
                    var messages = context.GetValidationMessages(fieldIdentifier);

                    // foreach (var message in messages)
                    // {
                    //     validationMessages.Add((property.Name, message));
                    //     Logger?.LogWarning("Validation failed for property: {Field} - Error: {Error}", property.Name, message);
                    // }
                }
                Snackbar.Add("Please check highlighted fields before publishing Ad.", Severity.Warning);
                return;
            }

            await SaveFormApi();
        }
        protected async Task SaveFormApi()
        {
            IsSaving = true;
            ErrorMessage = string.Empty;
            try
            {

                var selectedCategory = CategoryTrees.FirstOrDefault(x => x.Id.ToString() == adPostModel.SelectedCategoryId);
                var selectedSubcategory = selectedCategory?.Children?.FirstOrDefault(x => x.Id.ToString() == adPostModel.SelectedSubcategoryId);
                var selectedSubSubcategory = selectedSubcategory?.Children?.FirstOrDefault(x => x.Id.ToString() == adPostModel.SelectedSubSubcategoryId);
                var vertical = char.ToUpper(adPostModel.SelectedVertical[0]) + adPostModel.SelectedVertical.Substring(1).ToLower();
                var dto = new ClassifiedPostDto
                {
                    SubVertical = vertical,
                    Title = adPostModel.Title,
                    Description = adPostModel.ItemDescription,
                    CategoryId = Guid.TryParse(
                        adPostModel.SelectedSubSubcategoryId ??
                        adPostModel.SelectedSubcategoryId ??
                        adPostModel.SelectedCategoryId, out var categoryGuid
                    ) ? categoryGuid : Guid.Empty,

                    // These are the new fields you mentioned
                    Category = selectedCategory?.Name,
                    SubCategory = selectedSubcategory?.Name,
                    L2Category = selectedSubSubcategory?.Name,

                    Brand = adPostModel.DynamicFields.TryGetValue("Brand", out var brand) ? brand : null,
                    Model = adPostModel.DynamicFields.TryGetValue("Model", out var model) ? model : null,
                    Condition = adPostModel.DynamicFields.TryGetValue("Condition", out var cond) ? cond : null,
                    Ram = adPostModel.DynamicFields.TryGetValue("Ram", out var ram) ? ram : null,
                    Capacity = adPostModel.DynamicFields.TryGetValue("Capacity", out var cap) ? cap : null,
                    Processor = adPostModel.DynamicFields.TryGetValue("Processor", out var proc) ? proc : null,
                    Color = dynamicFieldValues.TryGetValue("Color", out var color) ? color : null,
                    Storage = dynamicFieldValues.TryGetValue("Storage", out var storage) ? storage : null,
                    Coverage = dynamicFieldValues.TryGetValue("Coverage", out var coverage) ? coverage : null,
                    Resolution = dynamicFieldValues.TryGetValue("Resolution", out var resolution) ? resolution : null,
                    Gender = dynamicFieldValues.TryGetValue("Gender", out var gender) ? gender : null,


                    Price = adPostModel.Price,
                    CertificateUrl = adPostModel.Certificate,
                    CertificateFileName = adPostModel.CertificateFileName,
                    BatteryPercentage = adPostModel.BatteryPercentage,
                    PhoneNumber = $"{adPostModel.PhoneCode}{adPostModel.PhoneNumber}",
                    WhatsAppNumber = $"{adPostModel.WhatsappCode}{adPostModel.WhatsappNumber}",
                    Zone = adPostModel.Zone,
                    StreetNumber = (adPostModel.StreetNumber ?? 0).ToString(),
                    BuildingNumber = (adPostModel.BuildingNumber ?? 0).ToString(),
                    TearmsAndCondition = adPostModel.IsAgreed,
                    Latitude = adPostModel.Latitude ?? 0.0,
                    Longitude = adPostModel.Longitude ?? 0.0,

                    ImageUrls = adPostModel.PhotoUrls
                        .Where(url => !string.IsNullOrEmpty(url))
                        .Take(9)
                        .Select((url, index) => new AdImageDto
                        {
                            AdImageFileNames = $"image_{index + 1}.png",
                            Url = url,
                            Order = index
                        }).ToList(),

                    Status = 1,
                    AcceptsOffers = "No",
                    CountryOfOrigin = "Qatar",
                    Language = "en",
                    Location = new List<string?>
                        {
                            adPostModel.Zone,
                            adPostModel.FlyerLocation
                        }
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList()

                };
                var jsonDto = JsonSerializer.Serialize(dto, new JsonSerializerOptions
                {
                    WriteIndented = true // Optional: makes it pretty-printed
                });

                await LogObjectToConsoleAsync(dto);

                // Logger.LogInformation("Submitting ClassifiedPostDto as JSON:\n{DtoJson}", jsonDto);

                var response = await _classifiedsService.PostClassifiedItemAsync(adPostModel.SelectedVertical.ToLower(), dto);

                if (response?.IsSuccessStatusCode == true)
                {
                    Snackbar.Add($"{char.ToUpper(adPostModel.SelectedVertical[0]) + adPostModel.SelectedVertical.Substring(1).ToLower()} Ad published successfully!", Severity.Success);
                    adPostModel = new AdPost();
                    dynamicFieldValues = new Dictionary<string, string>();
                    selectedVertical = string.Empty;
                    StateHasChanged();
                }
                else
                {
                    Snackbar.Add($"Submission failed. Status: {response?.StatusCode}", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error while submitting post: {ex.Message}";
            }
            finally
            {
                IsSaving = false;
                StateHasChanged();
            }
        }

        protected override async Task OnInitializedAsync()
        {
            await LoadCategoryTreesAsync();

            if (Zones == null || Zones.Count == 0)
            {
                await LoadZonesAsync();
            }
        }


        private async Task LoadCategoryTreesAsync()
        {
            try
            {
                var response = await _classifiedsService.GetAllCategoryTreesAsync(selectedVertical);

                if (response is { IsSuccessStatusCode: true })
                {
                    var result = await response.Content.ReadFromJsonAsync<List<CategoryTreeDto>>();
                    CategoryTrees = result ?? new();
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
            }
        }
        private async Task LoadZonesAsync()
        {
            try
            {
                var response = await _classifiedsService.GetAllZonesAsync();

                if (response?.IsSuccessStatusCode == true)
                {
                    var result = await response.Content.ReadFromJsonAsync<LocationDto.LocationZoneListDto>();
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

                var response = await _classifiedsService.GetAddressByDetailsAsync(
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
                        await JSRuntime.InvokeVoidAsync("updateMapCoordinates", latitude, longitude);
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
