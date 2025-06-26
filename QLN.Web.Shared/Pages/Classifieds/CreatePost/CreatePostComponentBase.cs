using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using System.Text.Json;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services.Interface;
using System.Net.Http.Json;
using MudBlazor;
using Microsoft.JSInterop;

namespace QLN.Web.Shared.Pages.Classifieds.CreatePost
{
    public class CreatePostComponentBase : ComponentBase
    {
        [Inject] private IClassifiedsServices _classifiedsService { get; set; } = default!;
        [Inject] IJSRuntime JS { get; set; }

        [Inject] private ILogger<CreatePostComponentBase> Logger { get; set; }
        protected List<QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem> breadcrumbItems = new();
        protected bool IsLoadingCategories { get; set; } = true;
        protected string? ErrorMessage { get; set; }
        protected List<CategoryTreeDto> CategoryTrees { get; set; } = new();
        protected AdPost adPostModel = new();
        protected List<string> photoUrls = new() { "", "", "", "", "", "" };

        protected bool IsSaving { get; set; } = false;
        protected string SnackbarMessage { get; set; } = string.Empty;
        [Inject] public ISnackbar Snackbar { get; set; } = default!;

        protected string selectedVertical;

        protected override void OnInitialized()
        {
            breadcrumbItems = new()
            {
                new () { Label = "Classifieds", Url = "/qln/classifieds" },
                new () { Label = "Create Form", Url = "/qln/classifieds/createform", IsLast = true }
            };


        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await JS.InvokeVoidAsyncWithErrorHandling("initMap", 25.32, 51.54);
            }
        }

      
        public static Task UpdateLatLng(double lat, double lng)
        {
            Console.WriteLine($"New location: {lat}, {lng}");
            return Task.CompletedTask;
        }

        private Dictionary<string, string> dynamicFieldValues = new(); // Dynamic field values

        protected async void HandleCategoryChanged(string newValue)
        {
            selectedVertical = newValue;
            photoUrls = new List<string> { "", "", "", "", "", "" }; // Reset photo slots
            dynamicFieldValues = new Dictionary<string, string>(); // Clear dynamic fields
            await LoadCategoryTreesAsync();

            StateHasChanged(); // Re-render after data is loaded
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

        public class ValidationResult
        {
            public bool IsValid => !Messages.Any();
            public List<string> Messages { get; set; } = new();
        }

        private ValidationResult ValidateForm()
        {
            var result = new ValidationResult();
            var vertical = selectedVertical?.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(vertical))
            {
                result.Messages.Add("Vertical is required.");
                return result;
            }

            // Shared Validation
            if (string.IsNullOrWhiteSpace(adPostModel.Title))
                result.Messages.Add("Title is required.");

            if (!adPostModel.IsAgreed)
                result.Messages.Add("You must agree to the terms and conditions.");

            // Vertical-specific
            switch (vertical)
            {
                case "deals":
                    ValidateDealsFields(result);
                    break;

                default:
                    ValidateDefaultFields(result);
                    break;
            }

            return result;
        }

        private void ValidateDealsFields(ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(adPostModel.Certificate))
                result.Messages.Add("Certificate is required for Deals.");

            if (string.IsNullOrWhiteSpace(adPostModel.FlyerLocation))
                result.Messages.Add("Flyer Location is required for Deals.");

            if (photoUrls.All(string.IsNullOrWhiteSpace))
                result.Messages.Add("At least one image is required for Deals.");

            if (string.IsNullOrWhiteSpace(adPostModel.Phone) && string.IsNullOrWhiteSpace(adPostModel.Whatsapp))
                result.Messages.Add("At least one contact (Phone or WhatsApp) is required for Deals.");
        }

        private void ValidateDefaultFields(ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(adPostModel.ItemDescription))
                result.Messages.Add("Description is required.");

            var hasCategory = !string.IsNullOrWhiteSpace(adPostModel.SelectedCategoryId)
                        || !string.IsNullOrWhiteSpace(adPostModel.SelectedSubcategoryId)
                        || !string.IsNullOrWhiteSpace(adPostModel.SelectedSubSubcategoryId);

            if (!hasCategory)
                result.Messages.Add("At least one category must be selected.");

            if (string.IsNullOrWhiteSpace(adPostModel.Phone))
                result.Messages.Add("Phone number is required.");

            if (string.IsNullOrWhiteSpace(adPostModel.Whatsapp))
                result.Messages.Add("WhatsApp number is required.");
            if (string.IsNullOrWhiteSpace(adPostModel.Certificate))
                result.Messages.Add("Certificate is required.");

            if (string.IsNullOrWhiteSpace(adPostModel.Zone) ||
                string.IsNullOrWhiteSpace(adPostModel.StreetNumber) ||
                string.IsNullOrWhiteSpace(adPostModel.BuildingNumber))
                result.Messages.Add("Location details (Zone, Street, Building) are required.");

            var filledPhotos = photoUrls.Count(url => !string.IsNullOrWhiteSpace(url));
            if (filledPhotos < 4)
                result.Messages.Add("Please upload at least 4 images.");
        }

        protected async void SaveForm()
        {
            IsSaving = true;
            ErrorMessage = string.Empty;
            try
            {
                var validation = ValidateForm();
                if (!validation.IsValid)
                {
                    foreach (var msg in validation.Messages)
                        Snackbar.Add(msg, Severity.Warning);

                    return;
                }
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
                    CertificateBase64 = adPostModel.Certificate,
                    CertificateFileName = adPostModel.CertificateFileName,
                    BatteryPercentage = int.TryParse(adPostModel.BatteryPercentage, out var percent) ? percent : 0,
                    PhoneNumber = adPostModel.Phone,
                    WhatsAppNumber = adPostModel.Whatsapp,
                    Zone = adPostModel.Zone,
                    StreetNumber = adPostModel.StreetNumber,
                    BuildingNumber = adPostModel.BuildingNumber,
                    TearmsAndCondition = adPostModel.IsAgreed,
                    Latitude = adPostModel.Latitude,
                    Longitude = adPostModel.Longitude,

                    AdImagesBase64 = photoUrls
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
                            adPostModel.StreetNumber,
                            adPostModel.BuildingNumber,
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
                    photoUrls = new List<string> { "", "", "", "", "", "" };
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
    }
}
