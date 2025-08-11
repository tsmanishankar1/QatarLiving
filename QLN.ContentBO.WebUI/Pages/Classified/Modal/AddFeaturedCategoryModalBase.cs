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
    public class AddFeaturedCategoryModalBase : QLComponentBase
    {
        [CascadingParameter]
        public IMudDialogInstance MudDialog { get; set; }
        [Inject]
        public IClassifiedService ClassifiedService { get; set; }
        [Inject]
        public ISnackbar Snackbar { get; set; }

        [Parameter]
        public string Title { get; set; } = "Add Seasonal Pick";
        protected List<CategoryTreeNode> _categoryTree = new();
        protected List<CategoryTreeNode> _subcategories = new();
        protected List<CategoryTreeNode> _sections = new();
        protected bool IsLoadingCategories { get; set; } = true;
        public string TitleName { get; set; }

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

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var response = await ClassifiedService.GetAllCategoryTreesAsync("items");
                if (response?.IsSuccessStatusCode == true)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    _categoryTree = JsonSerializer.Deserialize<List<CategoryTreeNode>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new();

                    foreach (var cat in _categoryTree)
                        Console.WriteLine($"- {cat.Name} ({cat.Id}) → {cat.Children?.Count ?? 0} subcategories");
                }
                else
                {
                    Console.WriteLine(" Failed to fetch category tree. Status: " + response?.StatusCode);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(" Exception while loading category tree: " + ex.Message);
            }
            finally
            {
                IsLoadingCategories = false;
            }
        }

        protected void OnCategoryChanged(long? categoryId)
        {
            SelectedCategoryId = categoryId;
            SelectedSubcategoryId = null;
            SelectedSectionId = null;
            var category = _categoryTree.FirstOrDefault(c => c.Id == categoryId.ToString());
            SelectedCategory = category?.Name ?? string.Empty;
            _subcategories = category?.Children ?? new();
            _sections = new();
        }


        protected void OnSubcategoryChanged(long? subcategoryId)
        {
            SelectedSubcategoryId = subcategoryId;
            SelectedSectionId = null;
            var sub = _subcategories.FirstOrDefault(c => c.Id == subcategoryId.ToString());
            SelectedSubcategory = sub?.Name ?? string.Empty;
            _sections = sub?.Children ?? new();
        }
        protected void OnSectionChanged(long? sectionId)
        {
            SelectedSectionId = sectionId;
            var section = _sections.FirstOrDefault(c => c.Id == sectionId.ToString());
            SelectedSection = section?.Name ?? string.Empty;
        }



        protected bool IsFormValid()
        {
            return SelectedCategoryId.HasValue;
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
                vertical = "classifieds",
                title=Title,
                categoryId = SelectedCategoryId,
                categoryName = SelectedCategory,
                l1CategoryId = SelectedSubcategoryId,
                l1categoryName = SelectedSubcategory,
                //l2categoryId = SelectedSectionId,
                //l2categoryName = SelectedSection,
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
