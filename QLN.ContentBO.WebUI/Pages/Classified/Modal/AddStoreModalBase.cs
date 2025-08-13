using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using static QLN.ContentBO.WebUI.Models.ClassifiedLanding;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Models;
using System.Text.Json;
using QLN.ContentBO.WebUI.Interfaces;

namespace QLN.ContentBO.WebUI.Pages.Classified.Modal
{
    public class AddStoreModalBase : QLComponentBase
    {
        [CascadingParameter]
        public IMudDialogInstance MudDialog { get; set; }
        [Inject]
        public IClassifiedService ClassifiedService { get; set; }
        [Inject]
        public ISnackbar Snackbar { get; set; }
        protected string? featuredCategoryTitle;
        [Inject] IServiceBOService serviceBOService { get; set; }
        public List<VerificationProfileStatus> Listings { get; set; } = new();

        [Parameter]
        public string Title { get; set; } = "Add Seasonal Pick";
        [Parameter] public List<ServiceCategory> CategoryTrees { get; set; } = new();
        protected List<L1Category> _selectedL1Categories = new();
        protected List<L2Category> _selectedL2Categories = new();
        protected bool IsLoadingCategories { get; set; } = true;
        protected DateRange? dateRange
        {
            get => StartDate.HasValue && EndDate.HasValue ? new DateRange(StartDate.Value, EndDate.Value) : null;
            set
            {
                if (value != null)
                {
                    var start = value.Start ?? DateTime.Today;
                    var end = value.End ?? start;
                    StartDate = start;
                    EndDate = end;
                }
                StateHasChanged();
            }
        }

        protected Guid? SelectedCategoryId;
        protected string SelectedCategory { get; set; } = string.Empty;
        protected string ImagePreviewUrl { get; set; }
        protected string ImagePreviewWithoutBase64 { get; set; }

        protected ElementReference fileInput;

        protected DateTime? StartDate { get; set; } 
        protected DateTime? EndDate { get; set; } 

        protected bool IsSubmitting { get; set; } = false;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                  Listings =  await LoadCategoryTreesAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnInitializedAsync");
            }
            finally
            {
                IsLoadingCategories = false;
            }
        }

        protected void OnCategoryChanged(Guid? categoryId)
        {
            SelectedCategoryId = categoryId;
            var selected = Listings.FirstOrDefault(c => c.Id == categoryId);
            SelectedCategory = selected?.CompanyName ?? string.Empty;
        }
        private bool IsBase64String(string? base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
                return false;

            Span<byte> buffer = new Span<byte>(new byte[base64.Length]);
            return Convert.TryFromBase64String(base64, buffer, out _);
        }

        protected bool IsFormValid()
        {
            return SelectedCategoryId.HasValue
                && !string.IsNullOrWhiteSpace(featuredCategoryTitle)
                && StartDate.HasValue
                && EndDate.HasValue
                && !string.IsNullOrEmpty(ImagePreviewUrl);
        }

        protected void Close() => MudDialog.Cancel();
        protected async Task SaveAsync()
        {
            if (!IsFormValid())
            {
                Snackbar.Add("Please complete all required fields.", Severity.Warning);
                return;
            }

            IsSubmitting = true;
            var imageUrl = await UploadImageAsync(ImagePreviewWithoutBase64);
            var payload = new
            {
                title = featuredCategoryTitle,
                vertical = Vertical.Classifieds,
                storeId = SelectedCategoryId,
                storeName = SelectedCategory,
                startDate = StartDate?.ToString("yyyy-MM-dd"),
                endDate = EndDate?.ToString("yyyy-MM-dd"),
                imageUrl = imageUrl
            };

            try
            {
                var response = await ClassifiedService.CreateFeaturedStoresAsync(payload);
                if (response?.IsSuccessStatusCode == true)
                {
                    Snackbar.Add("Featured Store added successfully!", Severity.Success);
                    MudDialog.Close(DialogResult.Ok(true));
                }
                else
                {
                    Snackbar.Add("Failed to add Featured Store.", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error: {ex.Message}", Severity.Error);
            }
            finally
            {
                IsSubmitting = false;
            }
        }

        protected async Task OnLogoFileSelected(IBrowserFile file)
        {
            if (file == null)
                return;

            if (!file.ContentType.StartsWith("image/"))
            {
                Snackbar.Add("Only image files are allowed.", Severity.Warning);
                return;
            }

            if (file.Size > 10 * 1024 * 1024)
            {
                Snackbar.Add("Image must be less than 10MB.", Severity.Warning);
                return;
            }

            using var ms = new MemoryStream();
            await file.OpenReadStream(10 * 1024 * 1024).CopyToAsync(ms);
            var base64 = Convert.ToBase64String(ms.ToArray());
            ImagePreviewUrl = $"data:{file.ContentType};base64,{base64}";
            ImagePreviewWithoutBase64 = base64;
        }
        private async Task<string?> UploadImageAsync(string base64Image)
        {
            if (string.IsNullOrWhiteSpace(base64Image))
                return null;

            var uploadPayload = new FileUploadModel
            {
                Container = "services-images",
                File = base64Image
            };
            var uploadResponse = await FileUploadService.UploadFileAsync(uploadPayload);
            if (uploadResponse.IsSuccessStatusCode)
            {
                var result = await uploadResponse.Content.ReadFromJsonAsync<FileUploadResponseDto>();
                if (result?.IsSuccess == true)
                {
                    return result.FileUrl;
                }
                else
                {
                    Logger.LogWarning("Image upload failed: {Message}", result?.Message);
                }
            }
            else
            {
                Logger.LogWarning("Image upload HTTP error");
            }
            return null;
        }

        private async Task<List<VerificationProfileStatus>> LoadCategoryTreesAsync()
        {
            try
            {
                var payload = new
                {
                    vertical = 3,
                    subVertical = 5,
                    pageNumber = 1,
                    pageSize = 12
                };
                var response = await serviceBOService.GetAllCompaniesAsync(payload);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<PaginatedCompanyResponse>();
                    return result.items ?? new List<VerificationProfileStatus>();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "LoadCategoryTreesAsync");
            }

            return new List<VerificationProfileStatus>();
        }
    

    }
}
