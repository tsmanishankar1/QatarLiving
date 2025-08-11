using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using static QLN.ContentBO.WebUI.Models.ClassifiedLanding;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Models;
using System.Text.Json;
using QLN.ContentBO.WebUI.Interfaces;

namespace QLN.ContentBO.WebUI.Pages.Services.Modal
{
    public class ServicesEditSeasonPickModalBase : QLComponentBase
    {
        [CascadingParameter]
        public IMudDialogInstance MudDialog { get; set; }
        [Inject]
        public IClassifiedService ClassifiedService { get; set; }
        [Inject] public IServiceBOService _serviceService { get; set; }
        [Inject] ILogger<ServicesAddSeasonPickModalBase> Logger { get; set; }
        [Inject]
        public ISnackbar Snackbar { get; set; }
        [Parameter]
        public string Title { get; set; } = "Edit Seasonal Pick";
        [Parameter]
        public string CategoryId { get; set; }
        [Parameter] public List<ServiceCategory> CategoryTrees { get; set; } = new();
        protected List<L1Category> _selectedL1Categories = new();
        protected List<L2Category> _selectedL2Categories = new();
        protected bool IsLoadingCategories { get; set; } = true;
        protected SeasonalPicksDto selectedSeasonalPick { get; set; } = new();
        protected DateRange? dateRange
        {
            get => StartDate.HasValue && EndDate.HasValue ? new DateRange(StartDate, EndDate) : null;
            set
            {
                StartDate = value?.Start;
                EndDate = value?.End;
            }
        }
        protected string? featuredCategoryTitle;
        protected long? SelectedCategoryId;
        protected long? SelectedSubcategoryId;
        protected long? SelectedSectionId;
        protected string SelectedCategory { get; set; } = string.Empty;
        protected string SelectedSubcategory { get; set; } = string.Empty;
        protected string SelectedSection { get; set; } = string.Empty;
        protected string ImagePreviewUrl { get; set; }
        protected string ImagePreviewWithoutBase64 { get; set; }
        protected ElementReference fileInput;
        protected DateTime? StartDate { get; set; } = DateTime.Today;
        protected DateTime? EndDate { get; set; } = DateTime.Today;
        protected bool IsSubmitting { get; set; } = false;
        protected override async Task OnParametersSetAsync()
        {
            try
            {
                if (CategoryTrees == null || !CategoryTrees.Any())
                    await LoadCategoryTreesAsync();

                selectedSeasonalPick = await GetSeasonalPicksById();
                var json = JsonSerializer.Serialize(selectedSeasonalPick, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                if (selectedSeasonalPick != null)
                {
                    featuredCategoryTitle = selectedSeasonalPick.Title;
                    ImagePreviewUrl = selectedSeasonalPick.ImageUrl;
                    SelectedCategory = selectedSeasonalPick.CategoryName;
                    SelectedSubcategory = selectedSeasonalPick.L1categoryName;
                    SelectedSection = selectedSeasonalPick.L2categoryName;
                    SelectedSubcategoryId = selectedSeasonalPick.L1CategoryId;
                    SelectedSectionId = selectedSeasonalPick.L2categoryId;
                    var selectedCategory = CategoryTrees.FirstOrDefault(c => c.Id == selectedSeasonalPick?.CategoryId);
                    _selectedL1Categories = selectedCategory?.Fields ?? new();
                    var selectedL1 = _selectedL1Categories.FirstOrDefault(l1 => l1.Id == selectedSeasonalPick?.L1CategoryId);
                    _selectedL2Categories = selectedL1?.Fields ?? new();
                }
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "OnParametersSetAsync");
            }
            finally
            {
                IsLoadingCategories = false;
            }
        }
        private async Task LoadCategoryTreesAsync()
        {
            try
            {
                var response = await _serviceService.GetServicesCategories();
                if (response is { IsSuccessStatusCode: true })
                {
                    var result = await response.Content.ReadFromJsonAsync<List<ServiceCategory>>();
                    CategoryTrees = result ?? new();
                    StateHasChanged();
                }
                else
                {
                    Snackbar.Add("Failed to load categoryies", Severity.Warning);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "LoadCategoryTreesAsync");
            }
            finally
            {
                IsLoadingCategories = false;
                StateHasChanged();
            }
        }
        protected void OnCategoryChanged(long? categoryId)
        {
            SelectedCategoryId = categoryId;
            SelectedSubcategoryId = null;
            SelectedSectionId = null;
            _selectedL2Categories.Clear();
            var selectedCategory = CategoryTrees.FirstOrDefault(c => c.Id == categoryId);
            _selectedL1Categories = selectedCategory?.Fields ?? new();
            var selected = CategoryTrees.FirstOrDefault(c => c.Id == categoryId);
            SelectedCategory = selected?.CategoryName;
        }
        protected void OnSubcategoryChanged(long? subcategoryId)
        {
            SelectedSubcategoryId = subcategoryId;
            SelectedSectionId = null;
            var selectedL1 = _selectedL1Categories.FirstOrDefault(l1 => l1.Id == subcategoryId);
            _selectedL2Categories = selectedL1?.Fields ?? new();
            SelectedSubcategory = selectedL1?.CategoryName ?? string.Empty;
        }
        protected void OnSectionChanged(long? sectionId)
        {
            SelectedSectionId = sectionId;
            var section = _selectedL2Categories.FirstOrDefault(c => c.Id == sectionId);
            SelectedSection = section?.CategoryName ?? string.Empty;
        }
        protected bool IsFormValid()
        {
            return selectedSeasonalPick?.CategoryId.HasValue == true &&
                   SelectedSubcategoryId.HasValue &&
                   SelectedSectionId.HasValue &&
                   !string.IsNullOrWhiteSpace(featuredCategoryTitle) &&
                   StartDate.HasValue &&
                   EndDate.HasValue &&
                   !string.IsNullOrEmpty(ImagePreviewUrl);
        }
        protected void Close() => MudDialog.Cancel();
        protected void Save()
        {
            var newItem = new LandingPageItem
            {
                Category = SelectedCategory,
                Subcategory = SelectedSubcategory,
                Section = SelectedSection,
                ImageUrl = ImagePreviewUrl,
                Title = $"{SelectedCategory} - {SelectedSubcategory}",
                EndDate = DateTime.Now.AddMonths(3)
            };
            MudDialog.Close(DialogResult.Ok(newItem));
        }
        private bool IsBase64String(string? base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
                return false;

            Span<byte> buffer = new Span<byte>(new byte[base64.Length]);
            return Convert.TryFromBase64String(base64, buffer, out _);
        }

        protected async Task SaveAsync()
        {
            if (!IsFormValid())
            {
                Snackbar.Add("Please complete all required fields.", Severity.Warning);
                return;
            }
            IsSubmitting = true;
            var imageUrl = ImagePreviewUrl;
            if (IsBase64String(ImagePreviewWithoutBase64))
            {
                imageUrl = await UploadImageAsync(ImagePreviewWithoutBase64);
            }
            var payload = new
            {
                id = selectedSeasonalPick.Id,
                vertical = Vertical.Services,
                title = selectedSeasonalPick.Title,
                categoryId = selectedSeasonalPick?.CategoryId,
                categoryName = SelectedCategory,
                l1CategoryId = SelectedSubcategoryId,
                l1categoryName = SelectedSubcategory,
                l2categoryId = SelectedSectionId,
                l2categoryName = SelectedSection,
                startDate = StartDate?.ToString("yyyy-MM-dd"),
                endDate = EndDate?.ToString("yyyy-MM-dd"),
                slotOrder = selectedSeasonalPick.SlotOrder,
                imageUrl = imageUrl
            };
            try
            {
                var response = await ClassifiedService.UpdateSeasonalPicksAsync(payload);
                if (response?.IsSuccessStatusCode == true)
                {
                    Snackbar.Add("Seasonal pick Edited successfully!", Severity.Success);
                    MudDialog.Close(DialogResult.Ok(true));
                }
                else
                {
                    Snackbar.Add("Failed to add seasonal pick.", Severity.Error);
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
        private async Task<SeasonalPicksDto> GetSeasonalPicksById()
        {
            try
            {
                var apiResponse = await ClassifiedService.GetSeasonalPicksById(CategoryId);
                if (apiResponse.IsSuccessStatusCode)
                {
                    var response = await apiResponse.Content.ReadFromJsonAsync<SeasonalPicksDto>();
                    return response ?? new SeasonalPicksDto();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetFeaturedCategoryById");
            }
            return new SeasonalPicksDto();
        }


    }
}
