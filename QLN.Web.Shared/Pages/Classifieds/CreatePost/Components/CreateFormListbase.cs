using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using QLN.Common.DTO_s;
using QLN.Web.Shared.Models;

namespace QLN.Web.Shared.Pages.Classifieds.CreatePost.Components
{
    public class CreateFormListbase : ComponentBase
    {
        [Inject] private IJSRuntime JS { get; set; }
        [Parameter] public AdPost adPostModel { get; set; }
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
            await CategoryChanged.InvokeAsync(newValue);
            await JS.InvokeVoidAsyncWithErrorHandling("initMap", 25.32, 51.54);
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
    }
    public class OptionItem
    {
        public string Id { get; set; }
        public string Label { get; set; }
    }
}
