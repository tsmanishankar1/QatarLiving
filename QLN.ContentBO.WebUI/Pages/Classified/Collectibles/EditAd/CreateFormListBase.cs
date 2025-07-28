using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Models;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using MudExRichTextEditor;
using QLN.ContentBO.WebUI.Interfaces;

namespace QLN.ContentBO.WebUI.Pages.Classified.Collectibles.EditAd
{
    public class CreateFormListBase : ComponentBase
    {
        [Inject] public IClassifiedService _classifiedsService { get; set; } 
        protected bool IsLoadingCategories { get; set; } = true;
        protected string? ErrorMessage { get; set; }
        protected List<CategoryTreeDto> CategoryTrees { get; set; } = new();
        protected CategoryTreeDto SelectedCategory => CategoryTrees.FirstOrDefault(x => x.Id.ToString() == Ad.SelectedCategoryId);
        protected CategoryTreeDto SelectedSubcategory => SelectedCategory?.Children?.FirstOrDefault(x => x.Id.ToString() == Ad.SelectedSubcategoryId);
        protected CategoryTreeDto SelectedSubSubcategory => SelectedSubcategory?.Children?.FirstOrDefault(x => x.Id.ToString() == Ad.SelectedSubSubcategoryId);

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
        [Inject] ISnackbar Snackbar { get; set; }
        [Parameter] public EditAdPost Ad { get; set; } = new();
        protected EditContext editContext;
        private ValidationMessageStore messageStore;
        [Inject] private IJSRuntime JS { get; set; }
        protected MudExRichTextEdit Editor;
        private DotNetObjectReference<CreateFormListBase>? _dotNetRef;
        [Inject] ILogger<CreateFormListBase> Logger { get; set; }
        protected CountryModel SelectedPhoneCountry;
        protected CountryModel SelectedWhatsappCountry;
         protected string ShortFileName(string name, int max)
         {
                if (string.IsNullOrEmpty(name)) return "";
                return name.Length <= max ? name : name.Substring(0, max) + "...";
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
        protected async Task OnCategoryChanged(string categoryId)
        {
            Ad.SelectedCategoryId = categoryId;
            Ad.SelectedSubcategoryId = null;
            Ad.SelectedSubSubcategoryId = null;
            Ad.DynamicFields.Clear();
            DynamicFieldErrors.Clear();

            // Notify validation and refresh UI
            editContext.NotifyValidationStateChanged();
            StateHasChanged();
        }

        protected async Task OnSubCategoryChanged(string subcategoryId)
        {
            Ad.SelectedSubcategoryId = subcategoryId;
            Ad.SelectedSubSubcategoryId = null;
            Ad.DynamicFields.Clear();
            DynamicFieldErrors.Clear();

            editContext.NotifyValidationStateChanged();
            StateHasChanged();
        }

        protected async Task OnSubSubCategoryChanged(string subsubcategoryId)
        {
            Ad.SelectedSubSubcategoryId = subsubcategoryId;
            Ad.DynamicFields.Clear();
            DynamicFieldErrors.Clear();

            editContext.NotifyValidationStateChanged();
            StateHasChanged();
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


            StateHasChanged(); // Reflect changes in UI
            return Task.CompletedTask;
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
       protected override async Task OnInitializedAsync()
        {
            editContext = new EditContext(Ad);
            messageStore = new ValidationMessageStore(editContext);

            editContext.OnValidationRequested += (_, __) =>
            {
                messageStore.Clear();
                ValidateDynamicFields();
            };

            await LoadCategoryTreesAsync();
        }
        private void ValidateDynamicFields()
        {
            if (AvailableFields == null)
                return; // Nothing to validate yet

            foreach (var field in AvailableFields.Where(f => AllowedFields.Contains(f.Name)))
            {
                if (string.IsNullOrWhiteSpace(Ad.DynamicFields.GetValueOrDefault(field.Name)))
                {
                    messageStore.Add(new FieldIdentifier(Ad.DynamicFields, field.Name), $"{field.Name} is required.");
                }
            }
        }

          private async Task LoadCategoryTreesAsync()
        {
            try
            {
                var response = await _classifiedsService.GetAllCategoryTreesAsync("collectibles");

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


         protected void SubmitForm()
        {
            messageStore.Clear();
            DynamicFieldErrors.Clear();

            // Run automatic validation
            var isValid = editContext.Validate();

            if (SelectedCategory?.Children?.Any() == true && string.IsNullOrEmpty(Ad.SelectedSubcategoryId))
            {
                messageStore.Add(() => Ad.SelectedSubcategoryId, "Subcategory is required.");
                isValid = false;
            }

            if (SelectedSubcategory?.Children?.Any() == true && string.IsNullOrEmpty(Ad.SelectedSubSubcategoryId))
            {
                messageStore.Add(() => Ad.SelectedSubSubcategoryId, "Sub Subcategory is required.");
                isValid = false;
            }

            // Manual validation: Dynamic fields
            foreach (var field in AvailableFields.Where(f => AllowedFields.Contains(f.Name)))
            {
                var value = Ad.DynamicFields.ContainsKey(field.Name) ? Ad.DynamicFields[field.Name] : null;

                if (string.IsNullOrWhiteSpace(value))
                {
                    messageStore.Add(new FieldIdentifier(Ad.DynamicFields, field.Name), $"{field.Name} is required.");
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
                Snackbar.Add("Please fill all required fields.", Severity.Error);
                return;
            }

            // All good!
            Snackbar.Add("Form is valid and ready to submit!", Severity.Success);
            // Proceed with form submission
        }


    }
}
