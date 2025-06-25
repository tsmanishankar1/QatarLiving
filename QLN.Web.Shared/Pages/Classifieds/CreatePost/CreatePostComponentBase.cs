using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using System.Text.Json;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services.Interface;
using System.Net.Http.Json;
using QLN.Web.Shared.Components.BreadCrumb;
using QLN.Web.Shared.Pages.Classifieds.CreatePost.Components;
using System.Collections.Generic;
using MudBlazor;

namespace QLN.Web.Shared.Pages.Classifieds.CreatePost
{
    public class CreatePostComponentBase : ComponentBase
    {
        [Inject] private IClassifiedsServices _classifiedsService { get; set; } = default!;

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


        private string _authToken;
        protected string selectedVertical;

        protected override void OnInitialized()
        {
            breadcrumbItems = new()
            {
                new () { Label = "Classifieds", Url = "/qln/classifieds" },
                new () { Label = "Create Form", Url = "/qln/classifieds/createform", IsLast = true }
            };
        }
        private Dictionary<string, string> dynamicFieldValues = new(); // Dynamic field values

     protected async void HandleCategoryChanged(string newValue)
        {
            selectedVertical = newValue;

            // Now load category trees for the selected vertical
            await LoadCategoryTreesAsync();

            StateHasChanged(); // Re-render after data is loaded
        }
     
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _authToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6Ijk3NTQ1NGI1LTAxMmItNGQ1NC1iMTUyLWUzMGYzNmYzNjNlMiIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJNVUpBWSIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL2VtYWlsYWRkcmVzcyI6Im11amF5LmFAa3J5cHRvc2luZm9zeXMuY29tIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbW9iaWxlcGhvbmUiOiIrOTE3NzA4MjA0MDcxIiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjpbIkNvbXBhbnkiLCJTdWJzY3JpYmVyIl0sIlVzZXJJZCI6Ijk3NTQ1NGI1LTAxMmItNGQ1NC1iMTUyLWUzMGYzNmYzNjNlMiIsIlVzZXJOYW1lIjoiTVVKQVkiLCJFbWFpbCI6Im11amF5LmFAa3J5cHRvc2luZm9zeXMuY29tIiwiUGhvbmVOdW1iZXIiOiIrOTE3NzA4MjA0MDcxIiwiZXhwIjoxNzUwODUzMjE2LCJpc3MiOiJodHRwczovL3Rlc3QucWF0YXJsaXZpbmcuY29tIiwiYXVkIjoiaHR0cHM6Ly90ZXN0LnFhdGFybGl2aW5nLmNvbSJ9.7bOn01vqsHanmw7Ji88xaqm3ML7NmE3oRENPEE2CeU0";
                StateHasChanged();

            }

            await base.OnAfterRenderAsync(firstRender);
        }
        protected async void SaveForm()
        {
             IsSaving = true;
    ErrorMessage = string.Empty;
            try
            {
  if (string.IsNullOrWhiteSpace(selectedVertical))
        {
            Snackbar.Add("Vertical is required.", Severity.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(adPostModel.Title))
        {
            Snackbar.Add("Title is required.", Severity.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(adPostModel.ItemDescription))
        {
            Snackbar.Add("Description is required.", Severity.Warning);
            return;
        }

        if (photoUrls.All(string.IsNullOrWhiteSpace))
        {
            Snackbar.Add("At least one image is required.", Severity.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(adPostModel.Certificate))
        {
            Snackbar.Add("Certificate is required.", Severity.Warning);
            return;
        }
                var selectedCategory = CategoryTrees.FirstOrDefault(x => x.Id.ToString() == adPostModel.SelectedCategoryId);
                var selectedSubcategory = selectedCategory?.Children?.FirstOrDefault(x => x.Id.ToString() == adPostModel.SelectedSubcategoryId);
                var selectedSubSubcategory = selectedSubcategory?.Children?.FirstOrDefault(x => x.Id.ToString() == adPostModel.SelectedSubSubcategoryId);

                var dto = new ClassifiedPostDto
                {
                    SubVertical = adPostModel.SelectedVertical,
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



                    CertificateBase64 = adPostModel.Certificate,
                    BatteryPercentage = int.TryParse(adPostModel.BatteryPercentage, out var percent) ? percent : 0,
                    PhoneNumber = adPostModel.Phone,
                    WhatsAppNumber = adPostModel.Whatsapp,
                    Zone = adPostModel.Zone,
                    StreetNumber = adPostModel.StreetNumber,
                    BuildingNumber = adPostModel.BuildingNumber,
                    TearmsAndCondition = adPostModel.IsAgreed,

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
                    Location = new List<string> { adPostModel.Zone, adPostModel.StreetNumber, adPostModel.BuildingNumber }
                };
                var jsonDto = JsonSerializer.Serialize(dto, new JsonSerializerOptions
                {
                    WriteIndented = true // Optional: makes it pretty-printed
                });

                Logger.LogInformation("Submitting ClassifiedPostDto as JSON:\n{DtoJson}", jsonDto);

                var response = await _classifiedsService.PostClassifiedItemAsync(adPostModel.SelectedVertical.ToLower(), dto,_authToken);

             if (response?.IsSuccessStatusCode == true)
        {
            Snackbar.Add("Post submitted successfully!", Severity.Success);
        }
        else
        {
            Snackbar.Add($"Submission failed. Status: {response?.StatusCode}", Severity.Error);
        }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error while submitting post: {ex.Message}";
            } finally
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
