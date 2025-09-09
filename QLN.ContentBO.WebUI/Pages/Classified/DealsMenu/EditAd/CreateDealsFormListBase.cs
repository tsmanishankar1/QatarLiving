using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Models;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using MudExRichTextEditor;
using QLN.ContentBO.WebUI.Interfaces;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Pages.Classified.DealsMenu.EditAd
{
    public class CreateDealsFormListBase : ComponentBase
    {
        [Inject] public IClassifiedService _classifiedsService { get; set; }
        protected bool IsLoadingCategories { get; set; } = true;
        protected string? ErrorMessage { get; set; }
        protected List<CategoryTreeDto> CategoryTrees { get; set; } = new();
        protected string selectedCategory = "deals";


        [Inject] ISnackbar Snackbar { get; set; }
        [Parameter]
        public DealsModal Ad { get; set; }
        protected EditContext editContext;
        private ValidationMessageStore messageStore;
        [Inject] private IJSRuntime JS { get; set; }
        protected MudExRichTextEdit Editor;
        private DotNetObjectReference<CreateDealsFormListBase>? _dotNetRef;
        [Inject] ILogger<CreateDealsFormListBase> Logger { get; set; }
        protected CountryModel SelectedPhoneCountry;
        protected CountryModel SelectedWhatsappCountry;
        public DateTime? EndDay { get; set; }

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

            Ad.FlyerFileName = file.Name;
            Ad.FlyerFileUrl = Convert.ToBase64String(ms.ToArray());
        }
        protected void ClearFile()
        {
            Ad.FlyerFileName = null;
            Ad.FlyerFileUrl = null;
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
            Ad.WhatsappNumber = model.Code;
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
        protected string locationsString
        {
            get => Ad.Locations?.Locations != null
                   ? string.Join(", ", Ad.Locations.Locations)
                   : "";
            set => Ad.Locations = new LocationsWrapper
            {
                Locations = value?.Split(',')
                                  .Select(x => x.Trim())
                                  .Where(x => !string.IsNullOrWhiteSpace(x))
                                  .ToList() ?? new List<string>()
            };
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



        protected override async Task OnInitializedAsync()
        {
            Console.WriteLine($"Initial Ad value: {JsonSerializer.Serialize(Ad)}");

            Ad ??= new DealsModal();

            editContext = new EditContext(Ad);
            messageStore = new ValidationMessageStore(editContext);

            editContext.OnValidationRequested += (_, __) =>
            {
                messageStore.Clear();
            };

        }


        protected void SubmitForm()
        {
            messageStore.Clear();

            var isValid = editContext.Validate();



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
