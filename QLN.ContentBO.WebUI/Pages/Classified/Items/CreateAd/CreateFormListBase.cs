using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Models;
using MudExRichTextEditor;
using Microsoft.JSInterop;

namespace QLN.ContentBO.WebUI.Pages.Classified.Items.CreateAd
{
    public class CreateFormListBase : ComponentBase
    {
        [Parameter]
        public string? UserEmail { get; set; }
        public AdPost Ad { get; set; } = new();
        [Inject] private IJSRuntime JS { get; set; }
        protected MudExRichTextEdit Editor;
        private DotNetObjectReference<CreateFormListBase>? _dotNetRef;
        [Inject] ILogger<CreateFormListBase> Logger { get; set; }
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

        // Dummy dropdown field options
        public Dictionary<string, List<string>> FieldOptions { get; set; } = new()
        {
            { "Color", new() { "Red", "Blue", "Green" } },
            { "Brand", new() { "Brand A", "Brand B", "Brand C" } },
            { "Condition", new() { "New", "Used", "Refurbished" } }
        };

        protected void SubmitForm()
        {
            // Submit logic or validation here
        }
    }
}
