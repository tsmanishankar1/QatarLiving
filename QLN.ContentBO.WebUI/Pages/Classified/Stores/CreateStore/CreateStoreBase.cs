using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using QLN.ContentBO.WebUI.Models;
using MudExRichTextEditor;
using Microsoft.JSInterop;

namespace QLN.ContentBO.WebUI.Pages.Classified.Stores.CreateStore
{
    public class CreateStoreBase : ComponentBase
    {
        [Parameter]
        public string? UserEmail { get; set; }
        public string? Location { get; set; }
        public AdPost Ad { get; set; } = new();
        [Inject] private IJSRuntime JS { get; set; }
        protected MudExRichTextEdit Editor;
        protected string? _coverImageError;
        public string? CoverImage { get; set; }
        private DotNetObjectReference<CreateStoreBase>? _dotNetRef;
        [Inject] ILogger<CreateStoreBase> Logger { get; set; }
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
            StateHasChanged();
            return Task.CompletedTask;
        }

        public Dictionary<string, List<string>> FieldOptions { get; set; } = new()
        {
            { "Country", new() { "Qatar", "USA", "UK" } },
            { "City", new() { "Doha", "Dubai" } },
        };

        protected void SubmitForm()
        {

        }
        protected async Task HandleFilesChanged(InputFileChangeEventArgs e)
        {
            var file = e.File;
            if (file != null)
            {
                using var stream = file.OpenReadStream(5 * 1024 * 1024);
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var base64 = Convert.ToBase64String(memoryStream.ToArray());
                CoverImage = $"data:{file.ContentType};base64,{base64}";
                _coverImageError = null;
            }
        }
        protected void EditImage()
        {
            CoverImage = null;
        }
    }
}
