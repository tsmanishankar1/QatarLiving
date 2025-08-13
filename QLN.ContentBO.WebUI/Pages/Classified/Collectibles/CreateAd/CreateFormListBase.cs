using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Models;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using MudExRichTextEditor;
using QLN.ContentBO.WebUI.Interfaces;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Pages.Classified.Collectibles.CreateAd
{
    public class CreateFormListBase : ComponentBase
    {
        [Inject] public IClassifiedService _classifiedsService { get; set; }
        [Parameter]
        public string? UserEmail { get; set; }
        [Parameter] public List<LocationZoneDto> Zones { get; set; }
        [Parameter] public List<ClassifiedsCategory> CategoryTrees { get; set; } = new();
        protected ClassifiedsCategory SelectedCategory => CategoryTrees.FirstOrDefault(x => x.Id.ToString() == Ad.SelectedCategoryId);
        protected ClassifiedsCategoryField SelectedSubcategory => SelectedCategory?.Fields?.FirstOrDefault(x => x.Id.ToString() == Ad.SelectedSubcategoryId);
        protected ClassifiedsCategoryField SelectedSubSubcategory => SelectedSubcategory?.Fields?.FirstOrDefault(x => x.Id.ToString() == Ad.SelectedSubSubcategoryId);

        protected List<ClassifiedsCategoryField> AvailableFields =>
                                        SelectedSubSubcategory?.Fields ??
                                        SelectedSubcategory?.Fields ??
                                        SelectedCategory?.Fields ??
                                        new List<ClassifiedsCategoryField>();
        [Parameter] public string[] ExcludedFields { get; set; } = Array.Empty<string>();

        [Parameter] public CollectiblesAdPost Ad { get; set; } = new();
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

        protected async Task OnStreetNumberChanged(int? street)
        {
            var changed = Ad.StreetNumber != street;
            Ad.StreetNumber = street;

            if (!string.IsNullOrEmpty(Ad.Zone) && changed)
            {
                await OnAddressFieldsChanged.InvokeAsync();
            }
        }

        protected async Task OnBuildingNumberChanged(int? building)
        {
            var changed = Ad.BuildingNumber != building;
            Ad.BuildingNumber = building;

            if (!string.IsNullOrEmpty(Ad.Zone) && changed)
            {
                await OnAddressFieldsChanged.InvokeAsync();
            }
        }
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


    }
}
