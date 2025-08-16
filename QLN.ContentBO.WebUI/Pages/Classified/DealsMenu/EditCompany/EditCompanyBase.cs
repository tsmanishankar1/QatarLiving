using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Components.SuccessModal;
using Microsoft.AspNetCore.Components.Forms;
using QLN.ContentBO.WebUI.Interfaces;
using System.Net;   
using System.Text.Json;
using System.Net.Http;
using MudBlazor;

namespace QLN.ContentBO.WebUI.Pages.Classified.DealsMenu.EditCompany
{
    public class EditCompanyBase : QLComponentBase
    {
        [Inject] public NavigationManager Navigation { get; set; }
        protected string? LocalLogoBase64 { get; set; }
        [Inject]
        public IClassifiedService ClassifiedService { get; set; }
        [Inject] public IDialogService DialogService { get; set; }
        protected CompanyProfileItem CompanyDetails { get; set; } = new();
        [Parameter]
        public Guid? CompanyId { get; set; }
        private bool IsBase64String(string? base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
                return false;

            Span<byte> buffer = new Span<byte>(new byte[base64.Length]);
            return Convert.TryFromBase64String(base64, buffer, out _);
        }

        protected void GoBack()
        {
            NavManager.NavigateTo("/manage/classified/stores");
        }
        protected override async Task OnParametersSetAsync()
        {
            if (CompanyId.HasValue)
            {
                CompanyDetails = await GetServiceById();
            }
            else
            {
                CompanyDetails = new CompanyProfileItem();
            }
        }
        private async Task<CompanyProfileItem> GetServiceById()
        {
            try
            {
                var apiResponse = await ClassifiedService.GetCompanyProfileById(CompanyId ?? Guid.Empty);
                if (apiResponse.IsSuccessStatusCode)
                {
                    var response = await apiResponse.Content.ReadFromJsonAsync<CompanyProfileItem>();
                    return response ?? new CompanyProfileItem();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetEventsLocations");
            }
            return new CompanyProfileItem();
        }
        protected async Task ProcessImageAsync(string fileOrBase64)
        {
            LocalLogoBase64 = fileOrBase64;
        }
        protected async Task SubmitForm(CompanyProfileItem company)
        {
            try
            {

                if (IsBase64String(LocalLogoBase64))
                {
                    company.CompanyLogo = await UploadImageAsync(LocalLogoBase64);
                }
                var response = await ClassifiedService.UpdateCompanyProfile(company);
                if (response != null && response.IsSuccessStatusCode)
                {
                    await ShowSuccessModal("Company Updated Successfully");
                }
                else if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    var errorDetailJson = await response.Content.ReadAsStringAsync();

                    try
                    {
                        var errorObj = JsonSerializer.Deserialize<Dictionary<string, object>>(errorDetailJson);
                        if (errorObj != null && errorObj.ContainsKey("detail"))
                        {
                            Snackbar.Add(errorObj["detail"]?.ToString() ?? "Bad Request", Severity.Error);
                        }
                        else
                        {
                            Snackbar.Add("Bad Request", Severity.Error);
                        }
                    }
                    catch
                    {
                        Snackbar.Add("Bad Request", Severity.Error);
                    }

                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Snackbar.Add("You are unauthorized to perform this action");
                }
                else if (response.StatusCode == HttpStatusCode.InternalServerError)
                {
                    Snackbar.Add("Internal API Error");
                }
                else
                {
                    Snackbar.Add("Failed to update company profile", Severity.Error);
                }

            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SubmitForm");
            }
        }
        private async Task ShowSuccessModal(string title)
        {
            var parameters = new DialogParameters
            {
                { nameof(SuccessModalBase.Title), title },
            };

            var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.ExtraSmall,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<SuccessModal>("", parameters, options);
            var result = await dialog.Result;
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
        protected async Task UpdateStatus(CompanyUpdateActions actionRequest)
        {
            var response = await ClassifiedService.UpdateCompanyActions(actionRequest);
            if (response.IsSuccessStatusCode)
            {
                Snackbar.Add(
                actionRequest.CompanyVerificationStatus switch
                {
                    VerifiedStatus.Approved => "User Profile Approved Successfully",
                    VerifiedStatus.Rejected => "User Profile Rejected Successfully",
                    VerifiedStatus.NeedChanges => "Status Moved to Need Changes Successfully",
                    VerifiedStatus.OnHold => "User Profile is moved to on Hold Successfully",
                    _ => "User Profile Updated Successfully"
                },
                    Severity.Success
                );
                Navigation.NavigateTo($"/manage/classified/stores/view/stores");
                
            }
            else
            {
                Snackbar.Add("Failed to update ad status", Severity.Error);
            }
        }

    }
}
