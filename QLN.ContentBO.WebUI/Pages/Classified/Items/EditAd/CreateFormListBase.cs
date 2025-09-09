using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Models;
using PSC.Blazor.Components.MarkdownEditor;
using PSC.Blazor.Components.MarkdownEditor.EventsArgs;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using MudExRichTextEditor;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Pages.Classified.Items.EditAd
{
    public class CreateFormListBase : ComponentBase
    {
        [Parameter] public List<ClassifiedsCategory> CategoryTrees { get; set; } = new();
        [Parameter] public List<LocationZoneDto> Zones { get; set; } = new();
        protected string[] HiddenIcons = ["fullscreen"];
        protected MarkdownEditor MarkdownEditorRef;
        protected MudFileUpload<IBrowserFile> _markdownfileUploadRef;
         protected ClassifiedsCategory SelectedCategory =>
                CategoryTrees.FirstOrDefault(x => x.Id == Ad.CategoryId)
                ?? new ClassifiedsCategory();
        protected ClassifiedsCategoryField? SelectedSubcategory =>
    SelectedCategory?.Fields?.FirstOrDefault(x => x.Id == Ad.L1CategoryId);

protected ClassifiedsCategoryField? SelectedSubSubcategory =>
    SelectedSubcategory?.Fields?.FirstOrDefault(x => x.Id == Ad.L2CategoryId);


        protected List<ClassifiedsCategoryField> AvailableFields =>
                                        SelectedSubSubcategory?.Fields ??
                                        SelectedSubcategory?.Fields ??
                                        SelectedCategory?.Fields ??
                                        new List<ClassifiedsCategoryField>();
        [Parameter] public string[] ExcludedFields { get; set; } = Array.Empty<string>();
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
        protected string UploadImageButtonName { get; set; } = "uploadImage";
        [Inject] private IJSRuntime JS { get; set; }
        protected MudExRichTextEdit Editor;
        private DotNetObjectReference<CreateFormListBase>? _dotNetRef;
        [Inject] ILogger<CreateFormListBase> Logger { get; set; }
        protected CountryModel SelectedPhoneCountry;
        protected CountryModel SelectedWhatsappCountry;
  
        protected Task OnPhoneCountryChanged(CountryModel model)
        {
            SelectedPhoneCountry = model;
            Ad.ContactNumberCountryCode = model.Code;
            return Task.CompletedTask;
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
         protected async Task OnCategoryChanged(long? categoryId)
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

        protected async Task OnSubCategoryChanged(long? subcategoryId)
        {
            Ad.L1CategoryId = subcategoryId;
            Ad.L2CategoryId = null;
            Ad.DynamicFields.Clear();
            DynamicFieldErrors.Clear();

            editContext.NotifyValidationStateChanged();
            StateHasChanged();
        }

        protected async Task OnSubSubCategoryChanged(long? subsubcategoryId)
        {
            Ad.L2CategoryId = subsubcategoryId;
            Ad.DynamicFields.Clear();
            DynamicFieldErrors.Clear();

            editContext.NotifyValidationStateChanged();
            StateHasChanged();
        }
        protected override async Task OnInitializedAsync()
        {
          if (CategoryTrees == null || !CategoryTrees.Any())
            Logger.LogWarning("CategoryTrees is null or empty");

        foreach (var cat in CategoryTrees)
        {
            if (cat == null)
                Logger.LogWarning("CategoryTree has a null item");

            if (cat?.Id == null || cat?.CategoryName == null)
                Logger.LogWarning("CategoryTree has null Id or Name: {@cat}", cat);
        }


            await base.OnInitializedAsync();
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
            Ad.Longitude = lng;
            Ad.Latitude = lat;
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
