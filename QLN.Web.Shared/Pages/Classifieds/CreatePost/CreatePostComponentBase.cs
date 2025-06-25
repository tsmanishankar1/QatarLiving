using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Web.Shared.Services.Interface;
using System.Net.Http.Json;
using QLN.Web.Shared.Components.BreadCrumb;
using QLN.Web.Shared.Pages.Classifieds.CreatePost.Components;
using System.Collections.Generic;

namespace QLN.Web.Shared.Pages.Classifieds.CreatePost
{
    public class CreatePostComponentBase : ComponentBase
    {
        [Inject] private IClassifiedsServices _classifiedsService { get; set; } = default!;
        protected List<BreadcrumbItem> breadcrumbItems = new();
        protected bool IsLoadingCategories { get; set; } = true;
        protected string? ErrorMessage { get; set; }
        protected List<CategoryTreeDto> CategoryTrees { get; set; } = new();
        protected CreateFormList<string> formRef;
        protected UploadPhotos photoRef;
        protected string selectedVertical;

        protected override void OnInitialized()
        {
            breadcrumbItems = new()
            {
                new BreadcrumbItem { Label = "Classifieds", Url = "/qln/classifieds" },
                new BreadcrumbItem { Label = "Create Form", Url = "/qln/classifieds/createform", IsLast = true }
            };
        }

        protected void HandleCategoryChanged(string newValue)
            {
                selectedVertical = newValue;
                StateHasChanged();
            }
      protected async void SaveForm()
        {
            try
            {

                var payload = new
                {
                    Title = "Test Post", // Replace with actual form values
                    CategoryId = "123",  // Replace with bound data
                    Description = "Sample Description",
                    Price = 2500,
                    Photos = "",
                    Contact = new {
                        Phone = "55512345",
                        WhatsApp = "55512345"
                    }
                };

                if (!string.IsNullOrWhiteSpace(selectedVertical))
                {
                    var response = await _classifiedsService.PostClassifiedItemAsync(selectedVertical.ToLower(), payload);

                    if (response is { IsSuccessStatusCode: true })
                    {
                        // Handle success (navigate, show message, etc.)
                        Console.WriteLine("Post successful");
                    }
                    else
                    {
                        ErrorMessage = $"Error posting item. Status: {response?.StatusCode}";
                    }
                }
                else
                {
                    ErrorMessage = "Vertical is not selected.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Unexpected error: {ex.Message}";
            }
        }

          protected override async Task OnInitializedAsync()
    {
        await LoadCategoryTreesAsync();
    }

    private async Task LoadCategoryTreesAsync()
    {
        try
        {
            var response = await _classifiedsService.GetAllCategoryTreesAsync("Items");

            if (response is { IsSuccessStatusCode: true })
            {
                var result = await response.Content.ReadFromJsonAsync<List<CategoryTreeDto>>();
                CategoryTrees = result ?? new();
            }
            else
            {
                ErrorMessage = $"Failed to load category trees. Status: {response?.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error loading category trees.";
        }
        finally
        {
            IsLoadingCategories = false;
        }
    }
    }
}
