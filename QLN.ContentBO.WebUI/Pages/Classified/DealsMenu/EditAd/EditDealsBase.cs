using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using System.Net;
using System.Text.Json;
using static MudBlazor.CategoryTypes;

namespace QLN.ContentBO.WebUI.Pages.Classified.Items.EditAd
{
    public class EditDealsBase : QLComponentBase
    {
        [Parameter]
        public long AdId { get; set; }
        [Inject] public NavigationManager Navigation { get; set; }
        protected DealsModal adPostModel { get; set; } = new();

        [Inject] public IClassifiedService ClassifiedService { get; set; }
        [Inject] public IFileUploadService FileUploadService { get; set; }
        [Inject] protected IDialogService DialogService { get; set; }
        [Inject] ILogger<EditDealsBase> Logger { get; set; }
        protected List<LocationZoneDto> Zones { get; set; } = new();
        protected bool IsLoadingZones { get; set; } = true;
        protected bool IsLoadingCategories { get; set; } = true;
        protected bool IsSaving { get; set; } = false;
        protected bool IsLoadingMap { get; set; } = false;
        protected bool IsLoadingId { get; set; } = true;

        protected string? ErrorMessage { get; set; }
        [Inject] ISnackbar Snackbar { get; set; }
        protected bool IsLoading { get; set; } = true;

        protected CountryModel SelectedPhoneCountry { get; set; } = new();
        protected CountryModel SelectedWhatsappCountry { get; set; } = new();

        protected bool IsBtnDisabled { get; set; } = false;

        protected MudFileUpload<IBrowserFile> _pdfFileUploadRef = default!;
        protected MudFileUpload<IBrowserFile> _coverImageFileUploadRef = default!;

        protected int MaxPdfFileSizeInMb { get; set; } = 10; // 10 MB

        protected int MaxCoverImageSizeInMb { get; set; } = 2; // 2 MB

        protected async override Task OnParametersSetAsync()
        {
            try
            {
                if (AdId != null)
                {
                    await LoadDealAdData(AdId);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnParametersSetAsync");
            }
        }

        private async Task LoadDealAdData(long? adId)
        {
            try
            {
                IsLoading = true;
                var response = await ClassifiedService.GetDealsByIdAsync(adId);

                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        adPostModel = await response.Content.ReadFromJsonAsync<DealsModal>() ?? new();
                    }
                    else if (response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        Snackbar.Add("Invalid advertisement ID", Severity.Warning);
                    }
                    else if (response.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        Snackbar.Add("Server error while loading advertisement", Severity.Error);
                    }
                    else if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        Snackbar.Add("Advertisement not found", Severity.Warning);
                    }
                    else
                    {
                        ErrorMessage = $"Failed to load Deals data (HTTP {response.StatusCode})";
                        Snackbar.Add(ErrorMessage, Severity.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "LoadDealAdData");
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected void GoBack()
        {
            NavManager.NavigateTo("/manage/classified/deals/listing");
        }

        protected async Task HandleValidSubmit()
        {
            try
            {
                IsBtnDisabled = true;
                var response = await ClassifiedService.UpdateDealsAsync(adPostModel);
                if (response != null && response.IsSuccessStatusCode)
                {
                    // await LoadDealAdData(adPostModel.Id);
                    Snackbar.Add("Deal Data Updated", Severity.Success);
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Snackbar.Add("You are unauthorized to perform this action", Severity.Error);
                }
                else if (response.StatusCode == HttpStatusCode.InternalServerError)
                {
                    Snackbar.Add("Internal API Error", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "HandleValidSubmit");
            }
            finally
            {
                IsBtnDisabled = false;
            }
        }

        protected void ClearFile()
        {
            adPostModel.FlyerFileName = string.Empty;
            adPostModel.FlyerFileUrl = string.Empty;
            _pdfFileUploadRef?.ResetValidation();
        }

        protected Task OnPhoneCountryChanged(CountryModel model)
        {
            SelectedPhoneCountry = model;
            adPostModel.ContactNumberCountryCode = model.Code;
            return Task.CompletedTask;
        }

        protected Task OnWhatsappCountryChanged(CountryModel model)
        {
            SelectedWhatsappCountry = model;
            adPostModel.WhatsappNumberCountryCode = model.Code;
            return Task.CompletedTask;
        }
        protected Task OnPhoneChanged(string phone)
        {
            adPostModel.ContactNumber = phone;
            return Task.CompletedTask;
        }

        protected Task OnWhatsappChanged(string phone)
        {
            adPostModel.WhatsappNumber = phone;
            return Task.CompletedTask;
        }

       protected string LocationsString
        {
            get => adPostModel.Locations?.Locations != null
                   ? string.Join(", ", adPostModel.Locations.Locations)
                   : "";
            set => adPostModel.Locations = new LocationsWrapper
            {
                Locations = value?.Split(',')
                                  .Select(x => x.Trim())
                                  .Where(x => !string.IsNullOrWhiteSpace(x))
                                  .ToList() ?? new List<string>()
            };
        }



        protected async Task HandlePdfFileChanged(InputFileChangeEventArgs e)
        {
            try
            {
                var file = e.File;
                if (file != null)
                {
                    if (file.Size > MaxPdfFileSizeInMb * 1024 * 1024)
                    {
                        Snackbar.Add($"File is too large. Max {MaxPdfFileSizeInMb} MB is allowed.", Severity.Warning);
                        return;
                    }

                    using var stream = file.OpenReadStream(MaxPdfFileSizeInMb * 1024 * 1024); // 10MB limit
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    var base64 = Convert.ToBase64String(memoryStream.ToArray());
                    var uploadedImageBase64 = $"data:{file.ContentType};base64,{base64}";
                    if (!string.IsNullOrWhiteSpace(uploadedImageBase64))
                    {
                        var fileUploadData = new FileUploadModel
                        {
                            Container = ClassifiedsBlobContainerName,
                            File = uploadedImageBase64
                        };

                        var uploadedFileReponse = await FileUploadAsync(fileUploadData);
                        if (uploadedFileReponse.IsSuccess)
                        {
                            adPostModel.FlyerFileName = uploadedFileReponse.FileName;
                            adPostModel.FlyerFileUrl = uploadedFileReponse.FileUrl;
                            Snackbar.Add("PDF file uploaded successfully", Severity.Success);
                        }
                        else
                        {
                            Snackbar.Add("Failed to upload PDF file", Severity.Error);
                        }
                    }
                    _pdfFileUploadRef?.ResetValidation();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "HandlePdfFileChanged");
            }
        }

        protected async Task<FileUploadResponse> FileUploadAsync(FileUploadModel fileUploadData)
        {
            try
            {
                var response = await FileUploadService.UploadFileAsync(fileUploadData);
                var jsonString = await response.Content.ReadAsStringAsync();
                if (response != null && response.IsSuccessStatusCode)
                {
                    FileUploadResponse? result = JsonSerializer.Deserialize<FileUploadResponse>(jsonString, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return result ?? new();
                }
                else if (response?.StatusCode == HttpStatusCode.BadRequest)
                {
                    Snackbar.Add($"Bad Request: {jsonString}", Severity.Error);
                }
                else if (response?.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Snackbar.Add("You are unauthorized to perform this action", Severity.Error);
                }
                else if (response?.StatusCode == HttpStatusCode.InternalServerError)
                {
                    Snackbar.Add("Internal API Error", Severity.Error);
                }

                return new();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "FileUploadAsync");
                return new();
            }
        }


        protected async Task HandleImageFileChanged(InputFileChangeEventArgs e)
        {
            try
            {
                var file = e.File;
                if (file != null)
                {
                    if (file.Size > MaxCoverImageSizeInMb * 1024 * 1024)
                    {
                        Snackbar.Add($"Image size is too large. Max {MaxCoverImageSizeInMb} MB is allowed.", Severity.Warning);
                        return;
                    }

                    using var stream = file.OpenReadStream(MaxCoverImageSizeInMb * 1024 * 1024);
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    var base64 = Convert.ToBase64String(memoryStream.ToArray());
                    var uploadedImageBase64 = $"data:{file.ContentType};base64,{base64}";
                    if (!string.IsNullOrWhiteSpace(uploadedImageBase64))
                    {
                        var fileUploadData = new FileUploadModel
                        {
                            Container = ClassifiedsBlobContainerName,
                            File = uploadedImageBase64
                        };

                        var uploadedFileReponse = await FileUploadAsync(fileUploadData);
                        if (uploadedFileReponse.IsSuccess)
                        {
                            adPostModel.ImageUrl = uploadedFileReponse.FileUrl;
                            Snackbar.Add("Cover Image uploaded successfully", Severity.Success);
                        }
                        else
                        {
                            Snackbar.Add("Failed to upload Cover Image", Severity.Error);
                        }
                    }
                    _coverImageFileUploadRef?.ResetValidation();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "HandleImageFileChanged");
            }
        }

        protected Task RemoveImage()
        {
            adPostModel.ImageUrl = string.Empty;
            _coverImageFileUploadRef?.ResetValidation();
            return Task.CompletedTask;
        }
    }
}
