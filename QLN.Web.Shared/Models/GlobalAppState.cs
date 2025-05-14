// Shared/Models/GlobalAppState.cs

namespace QLN.Web.Shared.Models
{
    public class GlobalAppState
    {
        public string? Email { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? MobileNumber { get; set; }
        public string? Username { get; set; }
        public string? Token { get; set; }

        public bool IsLoggedIn => !string.IsNullOrWhiteSpace(Token);

        // Optional: To notify component state updates
        public event Action? OnChange;

        public void NotifyStateChanged() => OnChange?.Invoke();
    }
}
