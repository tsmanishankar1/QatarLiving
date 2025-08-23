using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using MudExRichTextEditor;
using PSC.Blazor.Components.MarkdownEditor;
using PSC.Blazor.Components.MarkdownEditor.EventsArgs;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Helpers;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Models;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Text.Json;

namespace QLN.ContentBO.WebUI.Pages.Classified.Stores.EditCompnay
{
    public class EditFormListBase : QLComponentBase
    {
        [Parameter] public CompanyProfileItem Company { get; set; } = new();
        [Inject] private IJSRuntime JS { get; set; }
        protected MudExRichTextEdit Editor;
        protected List<CountryTypeModel> countries = new();
        protected List<CityTypeModel> allCities = new();
        protected List<CityTypeModel> filteredCities = new();
        protected string selectedCountry;
        protected string selectedCity;
        [Parameter]
        public EventCallback<CompanyProfileItem> OnSubmitForm { get; set; }
        public string tempCoverImage { get; set; } = string.Empty;
        public string? tempCoverBase64Image { get; set; } = null;
        public string tempLicense { get; set; } = string.Empty;
        protected List<string> DaysOfWeek = new()
        {
            "Sunday","Monday","Tuesday","Wednesday","Thursday","Friday","Saturday"
        };
        public List<BusinessType> NatureOfBusinessOptions { get; set; } = [];
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
                countries = await LoadCountriesAsync();
                allCities = await LoadCitiesAsync();
                filteredCities = allCities;
                CompanyProfileOptions = new Dictionary<string, List<(int, string)>>()
                {
                    { "Company Size", GetEnumOptions<CompanySize>() },
                    { "Company Type", GetEnumOptions<CompanyType>() }
                };
                NatureOfBusinessOptions = BusinessTypesProvider.GetAll();

                foreach (var key in CompanyProfileOptions.Keys)
                {
                    if (!SelectedProfileValues.ContainsKey(key))
                    {
                        SelectedProfileValues[key] = 0;
                    }
                }
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
                tempCoverBase64Image = Company.CoverImage1 ?? string.Empty;
                tempLicense = Company.CrDocument ?? string.Empty;
                selectedCountry = Company.Country ?? string.Empty;
                selectedCity = Company.City ?? string.Empty;
                 var selectedCountryObj = countries
                ?.FirstOrDefault(c => c.Country_Name.Equals(selectedCountry, StringComparison.OrdinalIgnoreCase));
            filteredCities = allCities
                .Where(c => c.Country_Id == selectedCountryObj?.Id)
                .ToList();
                SelectedProfileValues["Company Size"] = Company.CompanySize;
                SelectedProfileValues["Company Type"] = Company.CompanyType;
            }
        }
        protected void OnCountryChanged(string country)
        {
            selectedCountry = country;
            var selectedCountryObj = countries
                ?.FirstOrDefault(c => c.Country_Name.Equals(country, StringComparison.OrdinalIgnoreCase));
            filteredCities = allCities
                .Where(c => c.Country_Id == selectedCountryObj?.Id)
                .ToList();
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
            if (string.IsNullOrWhiteSpace(Company.City))
            {
                Snackbar.Add("City is required.", Severity.Error);
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
             if (string.IsNullOrWhiteSpace(Company.PhoneNumberCountryCode))
            {
                Snackbar.Add("Country Code is required for Phone Number", Severity.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(Company.WhatsAppNumber))
            {
                Snackbar.Add("Whatsapp number is required.", Severity.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(Company.WhatsAppCountryCode))
            {
                Snackbar.Add("Country Code is required for Whatsapp Number", Severity.Error);
                return;
            }
             if (string.IsNullOrWhiteSpace(Company.CompanyLogo))
            {
                Snackbar.Add("Company Logo is required", Severity.Error);
                return;
            }
             if (string.IsNullOrWhiteSpace(Company.BusinessDescription))
            {
                Snackbar.Add("Business Description is required", Severity.Error);
                return;
            }
            if (Company.CrNumber == null)
            {
                Snackbar.Add("CR Number is required", Severity.Error);
                return;
            }
             if (!string.IsNullOrEmpty(tempCoverBase64Image) && IsBase64String(tempCoverBase64Image))
            {
                Company.CoverImage1 = await UploadImageAsync(tempCoverBase64Image);
            }
            if (IsBase64String(tempLicense))
            {
                    Company.CrDocument = await UploadCertificateAsync();
            }
            Company.CompanySize = SelectedProfileValues["Company Size"];
            Company.CompanyType = SelectedProfileValues["Company Type"];

            if (OnSubmitForm.HasDelegate)
            {
                await OnSubmitForm.InvokeAsync(Company);
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
                tempCoverBase64Image = Convert.ToBase64String(memoryStream.ToArray());
                tempCoverImage = $"data:{file.ContentType};base64,{tempCoverBase64Image}";

                _coverImageError = null;
            }
        }
        private async Task<List<CountryTypeModel>> LoadCountriesAsync()
        {
            var baseUri = Navigation.BaseUri;
            var json = await Http.GetStringAsync($"{baseUri}data/country.json");
            var countries = JsonSerializer.Deserialize<List<CountryTypeModel>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return countries ?? new List<CountryTypeModel>();
        }
        private async Task<List<CityTypeModel>> LoadCitiesAsync()
        {
            var baseUri = Navigation.BaseUri;
            var json = await Http.GetStringAsync($"{baseUri}data/city.json");
            var countries = JsonSerializer.Deserialize<List<CityTypeModel>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return countries ?? new List<CityTypeModel>();
        }
        protected void RemoveCoverImage()
        {
            Company.CoverImage1 = null;
            tempCoverBase64Image = tempCoverImage;
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
