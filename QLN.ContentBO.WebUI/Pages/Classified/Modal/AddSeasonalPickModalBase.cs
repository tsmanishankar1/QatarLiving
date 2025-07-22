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
    public class AddSeasonPickModalBase : QLComponentBase
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

        protected string? SelectedCategoryId;
        protected string? SelectedSubcategoryId;
        protected string? SelectedSectionId;
        protected string SelectedCategory { get; set; } = string.Empty;
        protected string SelectedSubcategory { get; set; } = string.Empty;
        protected string SelectedSection { get; set; } = string.Empty;
        protected string ImagePreviewUrl { get; set; }
        protected ElementReference fileInput;
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

                    Console.WriteLine("✅ Category Tree Loaded:");
                    foreach (var cat in _categoryTree)
                        Console.WriteLine($"- {cat.Name} ({cat.Id}) → {cat.Children?.Count ?? 0} subcategories");
                }
                else
                {
                    Console.WriteLine("❌ Failed to fetch category tree. Status: " + response?.StatusCode);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Exception while loading category tree: " + ex.Message);
            }
            finally
            {
                IsLoadingCategories = false;
            }
        }

        protected void OnCategoryChanged(string? categoryId)
        {
            Console.WriteLine($"➡️ Category Selected: {categoryId}");
            SelectedCategoryId = categoryId;
            SelectedSubcategoryId = null;
            SelectedSectionId = null;

            var category = _categoryTree.FirstOrDefault(c => c.Id == categoryId);
            _subcategories = category?.Children ?? new();

            Console.WriteLine($"Subcategories Count: {_subcategories.Count}");
            foreach (var sub in _subcategories)
                Console.WriteLine($" - {sub.Name} ({sub.Id})");

            _sections = new();
        }


        protected void OnSubcategoryChanged(string? subcategoryId)
        {
            Console.WriteLine($"➡️ Subcategory Selected: {subcategoryId}");
            SelectedSubcategoryId = subcategoryId;
            SelectedSectionId = null;

            var sub = _subcategories.FirstOrDefault(c => c.Id == subcategoryId);
            _sections = sub?.Children ?? new();

            Console.WriteLine($"Sections Count: {_sections.Count}");
            foreach (var sec in _sections)
                Console.WriteLine($" - {sec.Name} ({sec.Id})");
        }


        protected bool IsFormValid()
        {
            return !string.IsNullOrEmpty(SelectedCategoryId) &&
                   !string.IsNullOrEmpty(SelectedSubcategoryId) &&
                   !string.IsNullOrEmpty(SelectedSectionId);
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
        protected async Task OnLogoFileSelected(IBrowserFile file)
        {
            var allowedImageTypes = new[] { "image/png", "image/jpg" };

            if (!allowedImageTypes.Contains(file.ContentType))
            {
                Snackbar.Add("Only image files (PNG, JPG) are allowed.", Severity.Warning);
                return;
            }
            if (file != null)
            {
                if (file.Size > 10 * 1024 * 1024)
                {
                    Snackbar.Add("Logo must be less than 10MB", Severity.Warning);
                    return;
                }

                using var ms = new MemoryStream();
                await file.OpenReadStream(10 * 1024 * 1024).CopyToAsync(ms);
                var base64 = Convert.ToBase64String(ms.ToArray());
                ImagePreviewUrl = $"data:{file.ContentType};base64,{base64}";
            }
        }
    }
}
