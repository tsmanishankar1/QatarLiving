using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using QLN.Common.DTO_s;
using Microsoft.AspNetCore.Components.Forms;
using QLN.Web.Shared.Models;

namespace QLN.Web.Shared.Pages.Classifieds.CreatePost.Components
{
    public class CreateFormListbase : ComponentBase
    {
        [Inject] private IJSRuntime JS { get; set; }
        [Parameter] public AdPost adPostModel { get; set; }
        [Parameter] public EditContext EditContext { get; set; }
        [Parameter] public bool IsLoadingMap { get; set; } = false;
        [Parameter] public List<LocationDto.LocationZoneDto> Zones { get; set; }
        [Parameter] public EventCallback OnAddressFieldsChanged { get; set; }
        protected CountryModel SelectedPhoneCountry;
        protected CountryModel SelectedWhatsappCountry;

    protected async Task OnAddressChanged(int? val, string propertyName)
        {
            switch (propertyName)
            {
                case nameof(adPostModel.StreetNumber):
                    adPostModel.StreetNumber = val;
                    break;
                case nameof(adPostModel.BuildingNumber):
                    adPostModel.BuildingNumber = val;
                    break;
            }

            var fi = new FieldIdentifier(adPostModel, propertyName);
            EditContext?.NotifyFieldChanged(fi);

            await OnAddressFieldsChanged.InvokeAsync();
        }

    protected async Task OnZoneChanged(string newValue)
    {
        adPostModel.Zone = newValue;

        var fi = FieldIdentifier.Create(() => adPostModel.Zone);
        EditContext?.NotifyFieldChanged(fi);

        await OnAddressFieldsChanged.InvokeAsync();
    }

        protected Task OnPhoneCountryChanged(CountryModel model)
        {
            SelectedPhoneCountry = model;
            adPostModel.PhoneCode = model.Code;
            return Task.CompletedTask;
        }

        protected Task OnWhatsappCountryChanged(CountryModel model)
        {
            SelectedWhatsappCountry = model;
            adPostModel.WhatsappCode = model.Code;
            return Task.CompletedTask;
        }

        protected Task OnPhoneChanged(string phone)
        {
            adPostModel.PhoneNumber = phone;
            return Task.CompletedTask;
        }

        protected Task OnWhatsappChanged(string phone)
        {
            adPostModel.WhatsappNumber = phone;
            return Task.CompletedTask;
        }


        [Parameter] public List<CategoryTreeDto> CategoryTrees { get; set; }
        [Parameter] public EventCallback<string> CategoryChanged { get; set; }
        
        protected string uploadedFileBase64;

        public List<OptionItem> categoryOptions = new()
        {
            new OptionItem { Id = "items", Label = "Items" },
            new OptionItem { Id = "preloved", Label = "Preloved" },
            new OptionItem { Id = "collectibles", Label = "Collectibles" },
            new OptionItem { Id = "deals", Label = "Deals" }
        };

        protected bool mapInitialized = false;
        protected ElementReference mapDiv;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!mapInitialized && mapDiv.Context != null)
            {
                await JS.InvokeVoidAsync("initializeMap", DotNetObjectReference.Create(this));
                mapInitialized = true;
            }
        }

        protected void HandleUpload((string FileName, string Base64) fileData)
        {
            uploadedFileBase64 = fileData.Base64;
            adPostModel.Certificate = fileData.Base64;
            adPostModel.CertificateFileName = fileData.FileName;
        }

        protected async Task OnVerticalChanged(string newValue)
        {
            adPostModel.SelectedVertical = newValue;
            mapInitialized = false;
            adPostModel.Latitude = null;
            adPostModel.Longitude = null;
            await CategoryChanged.InvokeAsync(newValue);
            StateHasChanged();
        }

        protected async Task OnCategoryChanged(string newValue)
        {
            adPostModel.SelectedCategoryId = newValue;
            adPostModel.SelectedSubcategoryId = null;
            adPostModel.SelectedSubSubcategoryId = null;
            StateHasChanged();
        }

        protected async Task OnSubcategoryChanged(string newValue)
        {
            adPostModel.SelectedSubcategoryId = newValue;
            adPostModel.SelectedSubSubcategoryId = null;
            StateHasChanged();
        }
    protected string dummyField { get; set; }

   protected void OnDynamicFieldChanged(string fieldKey, string newVal)
    {
        adPostModel.DynamicFields[fieldKey] = newVal;

        var fi = new FieldIdentifier(adPostModel.DynamicFields, fieldKey);
        EditContext?.NotifyFieldChanged(fi);
    }
      protected IEnumerable<string> GetDynamicFieldErrors(string key)
{
    var fieldIdentifier = new FieldIdentifier(adPostModel.DynamicFields, key);
    return EditContext.GetValidationMessages(fieldIdentifier);
}

        protected async Task OnSubSubcategoryChanged(string newValue)
        {
            adPostModel.SelectedSubSubcategoryId = newValue;
            StateHasChanged();
        }

        [JSInvokable]
        public Task SetCoordinates(double lat, double lng)
        {
            adPostModel.Latitude = lat;
            adPostModel.Longitude = lng;
            StateHasChanged(); // This updates the UI
            return Task.CompletedTask;
        }

        public class OptionItem
        {
            public string Id { get; set; }
            public string Label { get; set; }
        }
    }
  
}
