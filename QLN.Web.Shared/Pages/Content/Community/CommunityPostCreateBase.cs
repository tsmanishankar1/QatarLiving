using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Contracts;

namespace QLN.Web.Shared.Pages.Content.Community
{
    public class CommunityPostCreateBase : ComponentBase
    {
        [Inject] protected ICommunityService CommunityService { get; set; }
        [Inject] protected NavigationManager NavigationManager { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }

        protected CreateCommunityPostDto PostModel { get; set; } = new();
        protected EditContext editContext;

        protected List<CommunityCategoryModel> Categories { get; set; } = new();
        protected string? ImagePreviewUrl { get; set; }
        protected bool AgreedToTerms { get; set; } = true;
        protected List<QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem> breadcrumbItems = new();

        protected bool IsLoading { get; set; }

        protected override async Task OnInitializedAsync()
        {
            editContext = new EditContext(PostModel);

            Categories = await CommunityService.GetCommunityCategoriesAsync();

            breadcrumbItems = new()
            {
                new() { Label = "Community", Url = "/content/v2/community" },
                new() { Label = "Create a Post", Url = "/qln/community/post/create", IsLast = true }
            };
        }

        protected async Task HandleValidSubmit()
        {
            IsLoading = true;
            if (!editContext.Validate())
            {
                var messages = editContext.GetValidationMessages();
                foreach (var message in messages)
                {
                    Snackbar.Add(message, Severity.Error);
                }
                return;
            }

            try
            {
                var selectedCategory = Categories.FirstOrDefault(c => c.Id == PostModel.CategoryId);
                PostModel.Category = selectedCategory?.Name;
                PostModel.IsActive = true;
                var success = await CommunityService.CreateCommunityPostAsync(PostModel);
                if (success)
                {
                    Snackbar.Add("Post created successfully!", Severity.Success);
                    PostModel = new CreateCommunityPostDto();
                    editContext = new EditContext(PostModel);
                    ImagePreviewUrl = null;
                    AgreedToTerms = false;
                }
                else
                {
                    Snackbar.Add("Failed to create post.", Severity.Error);
                }
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }
        protected void Cancel()
        {
            NavigationManager.NavigateTo("/content/community");
        }

      
        protected async Task OnLogoFileSelected(IBrowserFile file)
        {
            if (file != null)
            {
                var allowedImageTypes = new[] { "image/png", "image/jpg","image/jpeg" };

                if (!allowedImageTypes.Contains(file.ContentType))
                {
                    Snackbar.Add("Only image files (PNG, JPG,JPEG) are allowed.", Severity.Warning);
                    return;
                }
                if (file.Size > 10 * 1024 * 1024)
                {
                    Snackbar.Add("Logo must be less than 10MB", Severity.Warning);
                    return;
                }

                using var ms = new MemoryStream();
                await file.OpenReadStream(10 * 1024 * 1024).CopyToAsync(ms);
                var base64 = Convert.ToBase64String(ms.ToArray());
                PostModel.ImageBase64 = base64;
                ImagePreviewUrl = $"data:{file.ContentType};base64,{base64}";

            }
        }
        protected void OnDescriptionChanged(string value)
        {
            PostModel.Description = value;
            editContext?.NotifyFieldChanged(FieldIdentifier.Create(() => PostModel.Description));
        }

    }
}
