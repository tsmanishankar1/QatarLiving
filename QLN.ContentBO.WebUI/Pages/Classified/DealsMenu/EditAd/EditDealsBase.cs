using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using MudBlazor;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Components.News;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Services;
using System.Net;
using System.Text.Json;
using System.Web;

namespace QLN.ContentBO.WebUI.Pages.Classified.Items.EditAd
{
    public class EditDealsBase : QLComponentBase
    {
        [Parameter]
        public string? AdId { get; set; }
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

        protected async override Task OnParametersSetAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(AdId))
                {
                    await LoadDealAdData(AdId);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnParametersSetAsync");
            }
        }

        private async Task LoadDealAdData(string adId)
        {
            try
            {
                IsLoading = true;
                var response = await ClassifiedService.GetDealsByIdAsync("classifieds", adId);

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
                var response = await ClassifiedService.UpdateDealsAsync("classifieds", adPostModel);
                if (response != null && response.IsSuccessStatusCode)
                {
                    await LoadDealAdData(adPostModel.Id);
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

            adPostModel.FlyerFileName = file.Name;
            adPostModel.FlyerFileUrl = Convert.ToBase64String(ms.ToArray());
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
            get => adPostModel.Locations != null ? string.Join(", ", adPostModel.Locations) : "";
            set => adPostModel.Locations = value?.Split(',')
                                    .Select(x => x.Trim())
                                    .Where(x => !string.IsNullOrWhiteSpace(x))
                                    .ToList() ?? [];
        }
    }
}
