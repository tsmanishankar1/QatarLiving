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
    public class ServicesEditFeaturedCategoryModalBase : QLComponentBase
    {
        [CascadingParameter]
        public IMudDialogInstance MudDialog { get; set; }
        [Inject]
        public IClassifiedService ClassifiedService { get; set; }
        [Inject] public IFileUploadService FileUploadService { get; set; }
        [Inject] public IServiceBOService _serviceService { get; set; }
        [Inject]
        public ISnackbar Snackbar { get; set; }
        [Parameter]
        public string Title { get; set; } = "Edit Seasonal Pick";
        [Parameter]
        public string CategoryId { get; set; }
        private DateRange? _dateRange;
        protected DateRange? dateRange
        {
            get => _dateRange;
            set
            {
                if (_dateRange != value)
                {
                    _dateRange = value;
                    StateHasChanged();
                }
            }
        }
        private bool IsBase64String(string? base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
                return false;

            Span<byte> buffer = new Span<byte>(new byte[base64.Length]);
            return Convert.TryFromBase64String(base64, buffer, out _);
        }

        protected List<CategoryTreeNode> _categoryTree = new();
        protected List<CategoryTreeNode> _subcategories = new();
        protected List<L1Category> _selectedL1Categories = new();
        protected List<CategoryTreeNode> _sections = new();
        protected bool IsLoadingCategories { get; set; } = true;
        protected string? featuredCategoryTitle;
        protected long? SelectedCategoryId;
        protected long? SelectedSubcategoryId;
        protected string SelectedCategory { get; set; } = string.Empty;
        protected string SelectedSubcategory { get; set; } = string.Empty;

        protected string ImagePreviewUrl { get; set; }
        protected string ImagePreviewWithoutBase64 { get; set; }

        protected ElementReference fileInput;
        [Parameter] public List<ServiceCategory> CategoryTrees { get; set; } = new();
        protected FeaturedCategoryDto selectedFeaturedCategory { get; set; } = new();
        protected DateTime? StartDate { get; set; } = DateTime.Today;
        protected DateTime? EndDate { get; set; } = DateTime.Today;

        protected bool IsSubmitting { get; set; } = false;

        protected override async Task OnParametersSetAsync()
        {
            try
            {
                if (CategoryTrees == null || !CategoryTrees.Any())
                    await LoadCategoryTreesAsync();

                selectedFeaturedCategory = await GetFeaturedCategoryById();
                featuredCategoryTitle = selectedFeaturedCategory.Title;
                ImagePreviewUrl = selectedFeaturedCategory.ImageUrl;
                SelectedCategoryId = selectedFeaturedCategory.CategoryId;
                SelectedCategory = selectedFeaturedCategory.CategoryName;
                SelectedSubcategoryId = selectedFeaturedCategory.L1CategoryId;
                SelectedSubcategory = selectedFeaturedCategory.L1categoryName;
                var startDate = selectedFeaturedCategory.StartDate;
                var endDate = selectedFeaturedCategory.EndDate;
                if (startDate != DateOnly.MinValue && endDate != DateOnly.MinValue)
                {
                    dateRange = new DateRange(
                        startDate.ToDateTime(TimeOnly.MinValue),
                        endDate.ToDateTime(TimeOnly.MinValue)
                    );
                }
                else
                {
                    dateRange = null;
                }

                var category = CategoryTrees.FirstOrDefault(c => c.Id == selectedFeaturedCategory.CategoryId);
                if (category != null)
                {
                    _selectedL1Categories = category.Fields;
                    if (!_selectedL1Categories.Any(s => s.Id == selectedFeaturedCategory.L1CategoryId))
                    {
                        var sub = new L1Category
                        {
                            Id = selectedFeaturedCategory.L1CategoryId,
                            CategoryName = selectedFeaturedCategory.L1categoryName
                        };
                        _selectedL1Categories.Add(sub);
                    }
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
        private async Task<FeaturedCategoryDto> GetFeaturedCategoryById()
        {
            try
            {
                var apiResponse = await ClassifiedService.GetFeaturedCategoryById(CategoryId);
                if (apiResponse.IsSuccessStatusCode)
                {
                    var response = await apiResponse.Content.ReadFromJsonAsync<FeaturedCategoryDto>();
                    return response ?? new FeaturedCategoryDto();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetFeaturedCategoryById");
            }
            return new FeaturedCategoryDto();
        }

        protected void OnCategoryChanged(long? categoryId)
        {
            SelectedCategoryId = categoryId;
            var selected = CategoryTrees.FirstOrDefault(c => c.Id == categoryId);
            SelectedCategory = selected?.CategoryName;
            _selectedL1Categories = selected?.Fields ?? new();
        }

        protected void OnSubcategoryChanged(long? subcategoryId)
        {
            SelectedSubcategoryId = subcategoryId;
            var sub = _subcategories.FirstOrDefault(c => c.Id == subcategoryId.ToString());
            SelectedSubcategory = sub?.Name ?? string.Empty;
            _sections = sub?.Children ?? new();
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
        protected bool IsFormValid()
        {
            return SelectedCategoryId.HasValue &&
                   SelectedSubcategoryId.HasValue &&
                   !string.IsNullOrWhiteSpace(featuredCategoryTitle) &&
                   StartDate.HasValue &&
                   EndDate.HasValue &&
                   !string.IsNullOrEmpty(ImagePreviewUrl);
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
            var imageUrl = ImagePreviewUrl;
            if (IsBase64String(ImagePreviewWithoutBase64))
            {
                imageUrl = await UploadImageAsync(ImagePreviewWithoutBase64);
            }
            var payload = new
            {
                id = selectedFeaturedCategory.Id,
                vertical = Vertical.Services,
                title = featuredCategoryTitle,
                categoryId = SelectedCategoryId,
                categoryName = SelectedCategory,
                l1categoryName = SelectedSubcategory,
                l1CategoryId = SelectedSubcategoryId,
                startDate = StartDate?.ToString("yyyy-MM-dd"),
                endDate = EndDate?.ToString("yyyy-MM-dd"),
                imageUrl = imageUrl,
                slotOrder = selectedFeaturedCategory.SlotOrder,
            };
            try
            {
                var response = await ClassifiedService.UpdateFeaturedCategoryAsync(payload);
                if (response?.IsSuccessStatusCode == true)
                {
                    Snackbar.Add("Featured Category Edited successfully!", Severity.Success);
                    MudDialog.Close(DialogResult.Ok(true));
                }
                else
                {
                    Snackbar.Add("Failed to Edit Featured Category.", Severity.Error);
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
    }
}
