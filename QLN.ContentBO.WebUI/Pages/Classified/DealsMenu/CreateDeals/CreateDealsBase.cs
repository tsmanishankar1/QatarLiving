// using Microsoft.AspNetCore.Components;
// using Microsoft.AspNetCore.Components.Forms;
// using QLN.ContentBO.WebUI.Models;
// using MudExRichTextEditor;
// using QLN.ContentBO.WebUI.Components.SuccessModal;
// using System.Net; 
// using System.Text.Json;
// using Microsoft.AspNetCore.Components;
// using QLN.ContentBO.WebUI.Components;
// using System.Net.Http;
// using QLN.ContentBO.WebUI.Interfaces;
// using Microsoft.JSInterop;
// using MudBlazor;

// namespace QLN.ContentBO.WebUI.Pages.Classified.DealsMenu.CreateDeals
// {
//     public class CreateDealsBase : ComponentBase
//     {
//         [Parameter]
//         public Guid? CompanyId { get; set; }
//         protected CompanyProfileItem CompanyDetails { get; set; } = new();
//         public string? Location { get; set; }
//         public AdPost Ad { get; set; } = new();
//         [Inject] private IJSRuntime JS { get; set; }
//         protected MudExRichTextEdit Editor;
//          [Inject] public IDialogService DialogService { get; set; }
//          [Inject]
//         public IClassifiedService ClassifiedService { get; set; }
//          [Inject] ISnackbar Snackbar { get; set; }
//         protected string? _coverImageError;
//         protected CountryModel SelectedPhoneCountry;
//         protected CountryModel SelectedWhatsappCountry;
//         public string? CoverImage { get; set; }
//         private DotNetObjectReference<CreateDealsBase>? _dotNetRef;
//         [Inject] ILogger<CreateDealsBase> Logger { get; set; }
//         [Inject]
//         public NavigationManager NavigationManager { get; set; } = default!;
//         private bool IsBase64String(string? base64)
//         {
//             if (string.IsNullOrWhiteSpace(base64))
//                 return false;

//             Span<byte> buffer = new Span<byte>(new byte[base64.Length]);
//             return Convert.TryFromBase64String(base64, buffer, out _);
//         }
//         public string? PhoneCode { get; set; }
//         public string? PhoneNumber { get; set; }
//         public string? WhatsappCode { get; set; }
//         public string? WhatsappNumber { get; set; }
//         public string Email { get; set; }
//         public string WebsiteUrl { get; set; }
//         public string FacebookUrl { get; set; }
//         public string InstagramUrl { get; set; }
//         public string CRNumber { get; set; }
//         public string UserDesignation { get; set; }
//         public DateTime? StartDay { get; set; }
//         public DateTime? EndDay { get; set; }
//         public TimeSpan? StartHour { get; set; }
//         public TimeSpan? EndHour { get; set; }
//         public string _localLogoBase64 { get; set; }
//         public string CompanyLogo { get; set; }

//         protected override async Task OnParametersSetAsync()
//         {
//             if (CompanyId.HasValue)
//             {
//                 CompanyDetails = await GetServiceById();
//             }
//             else
//             {
//                 CompanyDetails = new CompanyProfileItem();
//             }
//         }       
//         private async Task<CompanyProfileItem> GetServiceById()
//         {
//             try
//             {
//                 var apiResponse = await ClassifiedService.GetCompanyProfileById(CompanyId ?? Guid.Empty);
//                 if (apiResponse.IsSuccessStatusCode)
//                 {
//                     var response = await apiResponse.Content.ReadFromJsonAsync<CompanyProfileItem>();
//                     return response ?? new CompanyProfileItem();
//                 }
//             }
//             catch (Exception ex)
//             {
//                 Logger.LogError(ex, "GetEventsLocations");
//             }
//             return new CompanyProfileItem();
//         }
//         private async Task ShowSuccessModal(string title)
//         {
//             var parameters = new DialogParameters
//             {
//                 { nameof(SuccessModalBase.Title), title },
//             };

//             var options = new DialogOptions
//             {
//                 CloseButton = false,
//                 MaxWidth = MaxWidth.ExtraSmall,
//                 FullWidth = true
//             };

//             var dialog = await DialogService.ShowAsync<SuccessModal>("", parameters, options);
//             var result = await dialog.Result;
//         }

//         protected override async Task OnAfterRenderAsync(bool firstRender)
//         {
//             if (firstRender)
//             {
//                 _dotNetRef = DotNetObjectReference.Create(this);
//                 await JS.InvokeVoidAsync("resetLeafletMap");
//                 await JS.InvokeVoidAsync("initializeMap", _dotNetRef);
//             }
//         }
//         [JSInvokable]
//         public Task SetCoordinates(double lat, double lng)
//         {
//             Logger.LogInformation("Map marker moved to Lat: {Lat}, Lng: {Lng}", lat, lng);
//             StateHasChanged();
//             return Task.CompletedTask;
//         }
//         protected Task OnPhoneCountryChanged(CountryModel model)
//         {
//             SelectedPhoneCountry = model;
//             Ad.PhoneCode = model.Code;
//             return Task.CompletedTask;
//         }

//         protected Task OnWhatsappCountryChanged(CountryModel model)
//         {
//             SelectedWhatsappCountry = model;
//             Ad.WhatsappCode = model.Code;
//             return Task.CompletedTask;
//         }
//         protected Task OnPhoneChanged(string phone)
//         {
//             Ad.PhoneNumber = phone;
//             return Task.CompletedTask;
//         }

//         protected Task OnWhatsappChanged(string phone)
//         {
//             Ad.WhatsappNumber = phone;
//             return Task.CompletedTask;
//         }
//         protected void ClearFile()
//         {
//             Ad.CertificateFileName = null;
//             Ad.Certificate = null;
//         }
//         protected async Task OnCrFileSelected(IBrowserFile file)
//         {
//             if (file.Size > 10 * 1024 * 1024)
//             {
//                 Snackbar.Add("File too large. Max 10MB allowed.", Severity.Warning);
//                 return;
//             }

//             using var stream = file.OpenReadStream();
//             using var ms = new MemoryStream();
//             await stream.CopyToAsync(ms);

//              Ad.CertificateFileName = file.Name;
//              Ad.Certificate = Convert.ToBase64String(ms.ToArray());
//         }



//         public Dictionary<string, List<string>> FieldOptions { get; set; } = new()
//         {
//             { "Country", new() { "Qatar", "USA", "UK" } },
//             { "City", new() { "Doha", "Dubai" } },
//         };
//         public Dictionary<string, List<string>> CompanyProfileOptions { get; set; } = new()
//         {
//             { "Nature of Business", new() { "Qatar", "USA", "UK" } },
//             { "Company Size", new() { "Doha", "Dubai" } },
//             { "Company Type", new() { "Doha", "Dubai" } }
//         };

//         protected async Task SubmitForm()
//         {
//             try
//             {

//                 if (IsBase64String(LocalLogoBase64))
//                 {
//                     company.CompanyLogo = await UploadImageAsync(LocalLogoBase64);
//                 }
//                 var response = await ClassifiedService.UpdateCompanyProfile(company);
//                 if (response != null && response.IsSuccessStatusCode)
//                 {
//                     await ShowSuccessModal("Company Updated Successfully");
//                 }
//                 else if (response.StatusCode == HttpStatusCode.BadRequest)
//                 {
//                     var errorDetailJson = await response.Content.ReadAsStringAsync();

//                     try
//                     {
//                         var errorObj = JsonSerializer.Deserialize<Dictionary<string, object>>(errorDetailJson);
//                         if (errorObj != null && errorObj.ContainsKey("detail"))
//                         {
//                             Snackbar.Add(errorObj["detail"]?.ToString() ?? "Bad Request", Severity.Error);
//                         }
//                         else
//                         {
//                             Snackbar.Add("Bad Request", Severity.Error);
//                         }
//                     }
//                     catch
//                     {
//                         Snackbar.Add("Bad Request", Severity.Error);
//                     }

//                 }
//                 else if (response.StatusCode == HttpStatusCode.Unauthorized)
//                 {
//                     Snackbar.Add("You are unauthorized to perform this action");
//                 }
//                 else if (response.StatusCode == HttpStatusCode.InternalServerError)
//                 {
//                     Snackbar.Add("Internal API Error");
//                 }
//                 else
//                 {
//                     Snackbar.Add("Failed to update company profile", Severity.Error);
//                 }

//             }
//             catch (Exception ex)
//             {
//                 Logger.LogError(ex, "SubmitForm");
//             }
//         }
//         private async Task<string?> UploadImageAsync(string fileOrBase64, string containerName = "services-images")
//         {
//             var uploadPayload = new FileUploadModel
//             {
//                 Container = containerName,
//                 File = fileOrBase64
//             };

//             var uploadResponse = await FileUploadService.UploadFileAsync(uploadPayload);

//             if (uploadResponse.IsSuccessStatusCode)
//             {
//                 var result = await uploadResponse.Content.ReadFromJsonAsync<FileUploadResponseDto>();

//                 if (result?.IsSuccess == true)
//                 {
//                     Logger.LogInformation("Image uploaded successfully: {FileUrl}", result.FileUrl);
//                     return result.FileUrl;
//                 }
//                 else
//                 {
//                     Logger.LogWarning("Image upload failed: {Message}", result?.Message);
//                 }
//             }
//             else
//             {
//                 Logger.LogWarning("Image upload HTTP error.");
//             }

//             return null;
//         }
//         protected async Task HandleFilesChanged(InputFileChangeEventArgs e)
//         {
//             var file = e.File;
//             if (file != null)
//             {
//                 using var stream = file.OpenReadStream(5 * 1024 * 1024);
//                 using var memoryStream = new MemoryStream();
//                 await stream.CopyToAsync(memoryStream);
//                 var base64 = Convert.ToBase64String(memoryStream.ToArray());
//                 CoverImage = $"data:{file.ContentType};base64,{base64}";
//                 _coverImageError = null;
//             }
//         }
//         protected void EditImage()
//         {
//             CoverImage = null;
//         }
//         protected void ClearLogo()
//         {
//             CompanyLogo = null;
//         }
//         protected async Task OnLogoFileSelected(IBrowserFile file)
//         {
//             var allowedImageTypes = new[] { "image/png", "image/jpg" };

//             if (!allowedImageTypes.Contains(file.ContentType))
//             {
//                 Snackbar.Add("Only image files (PNG, JPG) are allowed.", Severity.Warning);
//                 return;
//             }
//             if (file != null)
//             {
//                 if (file.Size > 10 * 1024 * 1024)
//                 {
//                     Snackbar.Add("Logo must be less than 10MB", Severity.Warning);
//                     return;
//                 }

//                 using var ms = new MemoryStream();
//                 await file.OpenReadStream(10 * 1024 * 1024).CopyToAsync(ms);
//                 var base64 = Convert.ToBase64String(ms.ToArray());
//                 _localLogoBase64 = base64;
//                  CompanyLogo = base64;
//             }
//         }
//         protected void onNavigationBack()
//         {
//             NavigationManager.NavigateTo("/manage/classified/deals/subscription/listing",forceLoad:true);
//         }
//     }
// }
