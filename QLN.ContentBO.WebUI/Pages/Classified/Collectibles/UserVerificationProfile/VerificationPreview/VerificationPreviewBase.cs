using Microsoft.AspNetCore.Components;

namespace QLN.ContentBO.WebUI.Pages.Classified.Collectibles.UserVerificationProfile.VerificationPreview
{
    public partial class VerificationPreviewBase : ComponentBase
    {
        [Parameter]
        public int UserId { get; set; }

        protected override async Task OnInitializedAsync()
        {
            // Fetch user data based on UserId if needed
            await LoadUserData(UserId);
        }

        private async Task LoadUserData(int userId)
        {
            // TODO: Replace with real API/service logic
            Console.WriteLine($"Loading data for user ID: {userId}");
            await Task.CompletedTask;
        }
    }
}
