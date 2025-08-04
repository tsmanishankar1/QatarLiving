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
        public string AdId { get; set; }
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
        protected override async Task OnInitializedAsync()
        {
            if (!string.IsNullOrEmpty(AdId))
            {
                await LoadAdData(AdId);
            }
        }

        protected async Task LoadAdData(string adId)
        {
            try
            {
                IsLoading = true;
                StateHasChanged();

                Logger.LogInformation("Loading ad data for {AdId}", adId);
                var response = await ClassifiedService.GetDealsByIdAsync("classifieds", adId);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    adPostModel = JsonSerializer.Deserialize<DealsModal>(content);
                    HasData = adPostModel != null;

                    if (!HasData)
                    {
                        Logger.LogWarning("No data returned for ad {AdId}", adId);
                        Snackbar.Add("Advertisement data not found", Severity.Warning);
                    }
                }
                else
                {
                    Logger.LogWarning("Failed to load ad {AdId}. Status: {StatusCode}",
                        adId, response.StatusCode);
                    ErrorMessage = $"Failed to load data (HTTP {response.StatusCode})";
                    Snackbar.Add("Failed to load advertisement", Severity.Error);
                }
            }
            catch (JsonException jsonEx)
            {
                Logger.LogError(jsonEx, "JSON parsing error for ad {AdId}", adId);
                ErrorMessage = "Invalid data format received";
                Snackbar.Add("Error parsing advertisement data", Severity.Error);
            }
            catch (HttpRequestException httpEx)
            {
                Logger.LogError(httpEx, "HTTP error loading ad {AdId}", adId);
                ErrorMessage = "Network error loading data";
                Snackbar.Add("Network error occurred", Severity.Error);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error loading ad {AdId}", adId);
                ErrorMessage = "Unexpected error occurred";
                Snackbar.Add("An unexpected error occurred", Severity.Error);
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        protected void GoBack()
        {
            Navigation.NavigateTo("/manage/classified/deals/listing");
        }
        
    }
}
