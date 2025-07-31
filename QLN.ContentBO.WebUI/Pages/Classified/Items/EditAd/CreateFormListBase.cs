using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Models;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using MudExRichTextEditor;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Pages.Classified.Items.EditAd
{
    public class CreateFormListBase : ComponentBase
    {
        [Parameter] public List<LocationZoneDto> Zones { get; set; }
        [Parameter] public List<CategoryTreeDto> CategoryTrees { get; set; } = new();
        protected CategoryTreeDto SelectedCategory => CategoryTrees.FirstOrDefault(x => x.Id.ToString() == Ad.CategoryId);
        protected CategoryTreeDto SelectedSubcategory => SelectedCategory?.Children?.FirstOrDefault(x => x.Id.ToString() == Ad.L1CategoryId);
        protected CategoryTreeDto SelectedSubSubcategory => SelectedSubcategory?.Children?.FirstOrDefault(x => x.Id.ToString() == Ad.L2CategoryId);

        protected List<CategoryField> AvailableFields =>
                                        SelectedSubSubcategory?.Fields ??
                                        SelectedSubcategory?.Fields ??
                                        SelectedCategory?.Fields ??
                                        new List<CategoryField>();
        [Parameter] public string[] AllowedFields { get; set; } = Array.Empty<string>();
        [Parameter] public string? DefaultSelectedPhoneCountry { get; set; }
        [Parameter] public string? DefaultSelectedWhatsappCountry { get; set; }

        [Parameter] public ItemEditAdPost Ad { get; set; } = new();
        [Parameter] public EditContext editContext { get; set; } = default!;
        [Parameter] public Dictionary<string, List<string>> DynamicFieldErrors { get; set; } = new();

        [Parameter] public bool IsLoadingZones { get; set; } = false;
        [Parameter] public EventCallback OnAddressFieldsChanged { get; set; }
        [Parameter] public bool IsLoadingCategories { get; set; } = true;
        protected string? ErrorMessage { get; set; }

        [Inject] ISnackbar Snackbar { get; set; }
        [Inject] private IJSRuntime JS { get; set; }
        protected MudExRichTextEdit Editor;
        private DotNetObjectReference<CreateFormListBase>? _dotNetRef;
        [Inject] ILogger<CreateFormListBase> Logger { get; set; }
        protected CountryModel SelectedPhoneCountry;
        protected CountryModel SelectedWhatsappCountry;
       public void SetDefaultDynamicFieldsFromApi()
{
    // Map main field names to their values from the Ad model
    var mainFields = new Dictionary<string, string?>
    {
        { "Location", Ad.Location },
        { "Brand", Ad.Brand },
        { "Model", Ad.Model },
        { "Condition", Ad.Condition },
        { "Color", Ad.Color }
    };

    // Assign values to DynamicFields if the field exists in AvailableFields and is not empty
    foreach (var field in mainFields)
    {
        if (!string.IsNullOrWhiteSpace(field.Value) &&
            AvailableFields.Any(f => string.Equals(f.Name, field.Key, StringComparison.OrdinalIgnoreCase)))
        {
            Ad.DynamicFields[field.Key] = field.Value!;
        }
    }

    // Add any attribute values to DynamicFields, if matching field exists
    if (Ad.Attributes != null)
    {
        foreach (var attribute in Ad.Attributes)
        {
            if (!string.IsNullOrWhiteSpace(attribute.Value) &&
                AvailableFields.Any(f => string.Equals(f.Name, attribute.Key, StringComparison.OrdinalIgnoreCase)))
            {
                Ad.DynamicFields[attribute.Key] = attribute.Value;
            }
        }
    }
}


        protected Task OnPhoneCountryChanged(CountryModel model)
        {
            SelectedPhoneCountry = model;
            Ad.ContactNumberCountryCode = model.Code;
            return Task.CompletedTask;
        }

        protected Task OnWhatsappCountryChanged(CountryModel model)
        {
            SelectedWhatsappCountry = model;
            Ad.WhatsappNumberCountryCode = model.Code;
            return Task.CompletedTask;
        }
         protected Task OnPhoneChanged(string phone)
        {
            Ad.ContactNumber = phone;
            return Task.CompletedTask;
        }

        protected Task OnWhatsappChanged(string phone)
        {
            Ad.WhatsappNumber = phone;
            return Task.CompletedTask;
        }
         protected async Task OnCategoryChanged(string categoryId)
        {
            Ad.CategoryId = categoryId;
            Ad.L1CategoryId = null;
            Ad.L2CategoryId = null;
            Ad.DynamicFields.Clear();
            DynamicFieldErrors.Clear();

            // Notify validation and refresh UI
            editContext.NotifyValidationStateChanged();
            StateHasChanged();
        }

        protected async Task OnSubCategoryChanged(string subcategoryId)
        {
            Ad.L1CategoryId = subcategoryId;
            Ad.L2CategoryId = null;
            Ad.DynamicFields.Clear();
            DynamicFieldErrors.Clear();

            editContext.NotifyValidationStateChanged();
            StateHasChanged();
        }

        protected async Task OnSubSubCategoryChanged(string subsubcategoryId)
        {
            Ad.L2CategoryId = subsubcategoryId;
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
        protected async Task OnZoneChanged(string zoneId)
        {
            var isChanged = Ad.Zone != zoneId;
            Ad.Zone = zoneId;

            if (isChanged)
            {
                await OnAddressFieldsChanged.InvokeAsync();
            }
        }

        protected async Task OnStreetNumberChanged(string? street)
        {
            var changed = Ad.StreetNumber != street;
            Ad.StreetNumber = street;

            if (!string.IsNullOrEmpty(Ad.Zone) && changed)
            {
                await OnAddressFieldsChanged.InvokeAsync();
            }
        }

        protected async Task OnBuildingNumberChanged(string? building)
        {
            var changed = Ad.BuildingNumber != building;
            Ad.BuildingNumber = building;

            if (!string.IsNullOrEmpty(Ad.Zone) && changed)
            {
                await OnAddressFieldsChanged.InvokeAsync();
            }
        }

    }
}
