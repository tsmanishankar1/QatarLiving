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
    public class ServicesAddFeaturedCategoryModalBase : QLComponentBase
    {
        [CascadingParameter]
        public IMudDialogInstance MudDialog { get; set; }
        [Inject]
        public IClassifiedService ClassifiedService { get; set; }
         [Inject] public IServiceBOService _serviceService { get; set; }
        [Inject]
        public ISnackbar Snackbar { get; set; }

        [Parameter]
        public string Title { get; set; } = "Add Seasonal Pick";
        protected List<CategoryTreeNode> _categoryTree = new();
        protected List<CategoryTreeNode> _subcategories = new();
        protected List<CategoryTreeNode> _sections = new();
        protected bool IsLoadingCategories { get; set; } = true;

        protected string? SelectedCategoryId;
        protected string? SelectedSubcategoryId;
        protected string? SelectedSectionId;
        protected string SelectedCategory { get; set; } = string.Empty;
        protected string SelectedSubcategory { get; set; } = string.Empty;
        protected string SelectedSection { get; set; } = string.Empty;
        protected string ImagePreviewUrl { get; set; }
        protected string ImagePreviewWithoutBase64 { get; set; }

        protected ElementReference fileInput;
        [Parameter] public List<ServiceCategory> CategoryTrees { get; set; } = new();

        protected DateTime? StartDate { get; set; } = DateTime.Today;
        protected DateTime? EndDate { get; set; } = DateTime.Today;

        protected bool IsSubmitting { get; set; } = false;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                if (CategoryTrees == null || !CategoryTrees.Any())
                    await LoadCategoryTreesAsync();
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

        protected void OnCategoryChanged(string? categoryId)
        {
            SelectedCategoryId = categoryId?.ToString();
           var selected = CategoryTrees.FirstOrDefault(c => c.Id.ToString() == categoryId);
            SelectedCategory = selected?.Category;
        }


        protected void OnSubcategoryChanged(string? subcategoryId)
        {
            Console.WriteLine($"➡️ Subcategory Selected: {subcategoryId}");
            SelectedSubcategoryId = subcategoryId;
            SelectedSectionId = null;

            var sub = _subcategories.FirstOrDefault(c => c.Id == subcategoryId);
            SelectedSubcategory = sub?.Name ?? string.Empty;

            _sections = sub?.Children ?? new();

            Console.WriteLine($"Sections Count: {_sections.Count}");
            foreach (var sec in _sections)
                Console.WriteLine($" - {sec.Name} ({sec.Id})");
        }
        protected void OnSectionChanged(string? sectionId)
        {
            Console.WriteLine($"➡️ Section Selected: {sectionId}");
            SelectedSectionId = sectionId;

            var section = _sections.FirstOrDefault(c => c.Id == sectionId);
            SelectedSection = section?.Name ?? string.Empty;
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
            return !string.IsNullOrEmpty(SelectedCategoryId);
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

        protected async Task SaveAsync()
        {
            if (!IsFormValid())
            {
                Snackbar.Add("Please complete all required fields.", Severity.Warning);
                return;
            }

            IsSubmitting = true;

            var payload = new
            {
                vertical = "services",
                categoryId = SelectedCategoryId,
                categoryName = SelectedCategory,
                startDate = StartDate?.ToString("yyyy-MM-dd"),
                endDate = EndDate?.ToString("yyyy-MM-dd"),
                imageUrl = ImagePreviewWithoutBase64
            };

            try
            {
                var response = await ClassifiedService.CreateFeaturedCategoryAsync(payload);
                if (response?.IsSuccessStatusCode == true)
                {
                    Snackbar.Add("Seasonal pick added successfully!", Severity.Success);
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

    }
}
