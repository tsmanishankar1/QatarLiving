using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using System.Web;
using QLN.ContentBO.WebUI.Models;
using System.Text.Json;
using QLN.ContentBO.WebUI.Services;
using Microsoft.JSInterop;
using MudBlazor;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Components;

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
        [Inject] private IJSRuntime JS { get; set; }
        protected bool IsLoading { get; set; } = true;

        protected bool HasData { get; set; }

        protected async override Task OnParametersSetAsync()
        {
            IsLoading = true;
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
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadDealAdData(string adId)
        {
            try
            {
                var response = await ClassifiedService.GetDealsByIdAsync("classifieds", adId);

                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        adPostModel = await response.Content.ReadFromJsonAsync<DealsModal>() ?? new();
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        Snackbar.Add("Invalid advertisement ID", Severity.Warning);
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                    {
                        Snackbar.Add("Server error while loading advertisement", Severity.Error);
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
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
        }

        protected void GoBack()
        {
            NavManager.NavigateTo("/manage/classified/deals/listing");
        }

    }
}
