using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using QLN.ContentBO.WebUI.Models;
using PSC.Blazor.Components.MarkdownEditor;
using PSC.Blazor.Components.MarkdownEditor.EventsArgs;
using System.ComponentModel.DataAnnotations;
using MudExRichTextEditor;
using Microsoft.JSInterop;
using System.Text.Json;
using QLN.ContentBO.WebUI.Models;
using System.Linq.Expressions;
using MudBlazor;
using QLN.ContentBO.WebUI.Components;

namespace QLN.ContentBO.WebUI.Pages.Classified.Stores.EditCompnay
{
    public class EditFormListBase : QLComponentBase
    {
        [Parameter] public CompanyProfileItem Company { get; set; } = new();
        [Inject] private IJSRuntime JS { get; set; }
        protected MudExRichTextEdit Editor;
        [Parameter]
        public EventCallback OnSubmitForm { get; set; }
        public string tempCoverImage { get; set; } = string.Empty;
        public string tempLicense { get; set; } = string.Empty;
        protected List<string> DaysOfWeek = new()
        {
            "Sunday","Monday","Tuesday","Wednesday","Thursday","Friday","Saturday"
        };
        public Dictionary<int, string> NatureOfBusinessOptions { get; set; } = new();
        protected TimeSpan? StartHourTime
        {
            get => TimeSpan.TryParse(Company.StartHour, out var t) ? t : (TimeSpan?)null;
            set => Company.StartHour = value?.ToString(@"hh\:mm") ?? string.Empty;
        }
        private bool IsBase64String(string? base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
                return false;

            Span<byte> buffer = new Span<byte>(new byte[base64.Length]);
            return Convert.TryFromBase64String(base64, buffer, out _);
        }

        protected TimeSpan? EndHourTime
        {
            get => TimeSpan.TryParse(Company.EndHour, out var t) ? t : (TimeSpan?)null;
            set => Company.EndHour = value?.ToString(@"hh\:mm") ?? string.Empty;
        }
        protected HashSet<int> NatureOfBusinesValues = new HashSet<int>();
        [Inject]
        protected NavigationManager Navigation { get; set; } = default!;
        protected string _value = "Nothing selected";
        [Inject]
        protected HttpClient Http { get; set; } = default!;
        [Inject] ISnackbar Snackbar { get; set; }
        protected string? _coverImageError;
        public string _localLogoBase64 { get; set; }
        protected CountryModel SelectedPhoneCountry;
        protected CountryModel SelectedWhatsappCountry;
        protected string[] HiddenIcons = ["fullscreen"];
        protected MarkdownEditor MarkdownEditorRef;
        private DotNetObjectReference<EditFormListBase>? _dotNetRef;
        [Inject] ILogger<EditFormListBase> Logger { get; set; }
        protected string UploadImageButtonName { get; set; } = "uploadImage";
        protected List<string> FilteredCountries = new();
        protected MudFileUpload<IBrowserFile> _markdownfileUploadRef;
        protected Dictionary<string, int> SelectedProfileValues = new();
        public HashSet<int> _options = new HashSet<int>();

        protected override async Task OnInitializedAsync()
        {
            try
            {
                CompanyProfileOptions = new Dictionary<string, List<(int, string)>>()
        {
            { "Company Size", GetEnumOptions<CompanySize>() },
            { "Company Type", GetEnumOptions<CompanyType>() }
        };
                NatureOfBusinessOptions = Enum.GetValues(typeof(NatureOfBusiness))
                .Cast<NatureOfBusiness>()
                .ToDictionary(e => (int)e, e => e.ToString());

                foreach (var key in CompanyProfileOptions.Keys)
                {
                    if (!SelectedProfileValues.ContainsKey(key))
                    {
                        SelectedProfileValues[key] = 0;
                    }
                }


                Countries = await FetchCountryNamesAsync();
                FilteredCountries = Countries;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading countries: {ex.Message}");
            }
        }
        protected override async Task OnParametersSetAsync()
        {
            if (Company != null)
            {
                tempCoverImage = Company.CoverImage1 ?? string.Empty;
                tempLicense = Company.CrDocument ?? string.Empty;
                SelectedProfileValues["Company Size"] = Company.CompanySize;
                SelectedProfileValues["Company Type"] = Company.CompanyType;
            }
        }
        protected bool SearchNatureOfBusiness(string searchText, int optionValue)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return true;

            var label = NatureOfBusinessOptions[optionValue];
            return label.Contains(searchText, StringComparison.OrdinalIgnoreCase);
        }

        protected void OnProfileValueChanged(string key, int newValue)
        {
            SelectedProfileValues[key] = newValue;
        }

        private List<(int, string)> GetEnumOptions<T>() where T : Enum
        {
            return Enum.GetValues(typeof(T))
                       .Cast<T>()
                       .Select(e =>
                       {
                           var displayName = e.GetType()
                               .GetField(e.ToString())?
                               .GetCustomAttributes(typeof(DisplayAttribute), false)
                               .FirstOrDefault() is DisplayAttribute display
                               ? display.Name ?? e.ToString()
                               : e.ToString();

                           return ((int)(object)e, displayName);
                       })
                       .ToList();
        }

        protected Task<IEnumerable<string>> SearchCountries(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                FilteredCountries = Countries;
            else
                FilteredCountries = Countries
                    .Where(c => c.Contains(value, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            return Task.FromResult(FilteredCountries.AsEnumerable());
        }

        protected string ShortFileName(string name, int max)
        {
            if (string.IsNullOrEmpty(name)) return "";
            return name.Length <= max ? name : name.Substring(0, max) + "...";
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
        protected async void ToggleMarkdownPreview()
        {
            if (MarkdownEditorRef != null)
            {
                await MarkdownEditorRef.TogglePreviewAsync();
            }
        }
        protected void TriggerCustomImageUpload()
        {
            _markdownfileUploadRef.OpenFilePickerAsync();
        }
        private async Task<string?> UploadCertificateAsync()
        {
            if (tempLicense == null)
                return null;

            var fileUploadModel = new FileUploadModel
            {
                Container = "services-images",
                File = Company.CrDocument
            };
            var uploadResponse = await FileUploadService.UploadFileAsync(fileUploadModel);
            if (uploadResponse.IsSuccessStatusCode)
            {
                var result = await uploadResponse.Content.ReadFromJsonAsync<FileUploadResponseDto>();
                if (result?.IsSuccess == true)
                {
                    return result.FileUrl;
                }
                Logger.LogWarning("Certificate upload failed: " + result?.Message);
            }
            else
            {
                Logger.LogWarning("Certificate upload HTTP error");
            }

            return null;
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
            StateHasChanged();
            return Task.CompletedTask;
        }
        protected Task OnPhoneCountryChanged(CountryModel model)
        {
            SelectedPhoneCountry = model;
            Company.PhoneNumberCountryCode = model.Code;
            return Task.CompletedTask;
        }

        protected Task OnWhatsappCountryChanged(CountryModel model)
        {
            SelectedWhatsappCountry = model;
            Company.WhatsAppCountryCode = model.Code;
            return Task.CompletedTask;
        }
        protected Task OnPhoneChanged(string phone)
        {
            Company.PhoneNumber = phone;
            return Task.CompletedTask;
        }

        protected Task OnWhatsappChanged(string phone)
        {
            Company.WhatsAppNumber = phone;
            return Task.CompletedTask;
        }
        protected void ClearFile()
        {
            Company.CrDocument = string.Empty;
            Company.TherapeuticCertificate = string.Empty;
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

            Company.CrDocument = file.Name;
            tempLicense = Convert.ToBase64String(ms.ToArray());
        }

        protected List<string> Countries = new();

        public Dictionary<string, List<string>> FieldOptions { get; set; } = new()
        {
            { "Country", new() { "Qatar", "USA", "UK" } },
            { "City", new() { "Doha", "Dubai" } },
        };
        public Dictionary<string, List<(int Value, string Label)>> CompanyProfileOptions { get; set; } = new();
        protected async Task SubmitForm()
        {
            if (string.IsNullOrWhiteSpace(Company.CompanyName))
            {
                Snackbar.Add("Company Name is required.", Severity.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(Company.Country))
            {
                Snackbar.Add("Country is required.", Severity.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(Company.Email))
            {
                Snackbar.Add("Email is required.", Severity.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(Company.PhoneNumber))
            {
                Snackbar.Add("Phone number is required.", Severity.Error);
                return;
            }
             if (IsBase64String(tempCoverImage))
                {
                    Company.CompanyLogo = await UploadImageAsync(tempCoverImage);
                }
                if (IsBase64String(tempLicense))
                {
                    Company.CrDocument = await UploadCertificateAsync();
                }



            if (OnSubmitForm.HasDelegate)
            {
                await OnSubmitForm.InvokeAsync();
            }
        }
        private async Task<string?> UploadImageAsync(string fileOrBase64, string containerName = "services-images")
        {
            var uploadPayload = new FileUploadModel
            {
                Container = containerName,
                File = fileOrBase64
            };
            var uploadResponse = await FileUploadService.UploadFileAsync(uploadPayload);
            if (uploadResponse.IsSuccessStatusCode)
            {
                var result = await uploadResponse.Content.ReadFromJsonAsync<FileUploadResponseDto>();
                if (result?.IsSuccess == true)
                {
                    Logger.LogInformation("Image uploaded successfully: {FileUrl}", result.FileUrl);
                    return result.FileUrl;
                }
                else
                {
                    Logger.LogWarning("Image upload failed: {Message}", result?.Message);
                }
            }
            else
            {
                Logger.LogWarning("Image upload HTTP error.");
            }
            return null;
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
                Company.CoverImage1 = $"data:{file.ContentType};base64,{base64}";
                _coverImageError = null;
            }
        }
        private async Task<List<string>> FetchCountryNamesAsync()
        {
            try
            {
                var baseUri = Navigation.BaseUri;
                var json = await Http.GetStringAsync($"{baseUri}data/countries.json");

                var countriesJson = JsonSerializer.Deserialize<List<JsonElement>>(json);
                var countryNames = new List<string>();

                foreach (var country in countriesJson!)
                {
                    if (country.TryGetProperty("name", out var nameDict) &&
                        nameDict.TryGetProperty("common", out var nameElement))
                    {
                        string name = nameElement.GetString() ?? "Unknown";
                        countryNames.Add(name);
                    }
                }
                countryNames.Sort();
                return countryNames;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading countries: {ex.Message}");
                return new List<string>();
            }
        }

        protected void RemoveCoverImage()
        {
            Company.CoverImage1 = null;
            StateHasChanged();
        }

        protected async Task OnLogoFileSelected(IBrowserFile file)
        {
            var allowedImageTypes = new[] { "image/png", "image/jpg" };

            if (!allowedImageTypes.Contains(file.ContentType))
            {
                Snackbar.Add("Only image files (PNG, JPG) are allowed.", Severity.Warning);
                return;
            }
            if (file != null)
            {
                if (file.Size > 10 * 1024 * 1024)
                {
                    Snackbar.Add("Logo must be less than 10MB", Severity.Warning);
                    return;
                }

                using var ms = new MemoryStream();
                await file.OpenReadStream(10 * 1024 * 1024).CopyToAsync(ms);
                var base64 = Convert.ToBase64String(ms.ToArray());
                _localLogoBase64 = base64;
                Company.CompanyLogo = base64;
            }
        }

        protected void onNavigationBack()
        {
            NavManager.NavigateTo("/manage/classified/stores");
        }
    }
}
