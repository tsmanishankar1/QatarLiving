using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services;
using QLN.Web.Shared.Services.Interface;
using System.Text.Json.Serialization;

namespace QLN.Web.Shared.Pages.Subscription
{
    public partial class SubscriptionDetails : ComponentBase
    {
        [Inject] private NavigationManager Navigation { get; set; } = default!;


        [Inject] private HttpClient Http { get; set; } = default!;
        [Inject] private ApiService Api { get; set; } = default!;
        [Inject] private IClassifiedDashboardService ClassfiedDashboardService { get; set; } 

        private List<StatItem> stats = new();
        private string _authToken;

        private int _activeTabIndex;
        private bool _isChecked = false;
        private int _selectedAdsTab = 0;
        private void SetTab(int index)
        {
            _activeTabIndex = index;
        }
        private List<QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem> breadcrumbItems = new();
        private string? _errorMessage;
        private BusinessProfile? _businessProfile;
        private List<AdModal> publishedAds = new();
        private List<AdModal> unpublishedAds = new();
        protected bool _isLoading { get; set; } = true;

        protected override async void OnInitialized()
        {
            breadcrumbItems = new()
        {
            new() { Label = "Classifieds", Url = "classifieds" },
            new() { Label = "Dashboard", Url = "/subscription/details",IsLast=true }
        };
          
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _authToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6Ijk3NTQ1NGI1LTAxMmItNGQ1NC1iMTUyLWUzMGYzNmYzNjNlMiIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJNVUpBWSIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL2VtYWlsYWRkcmVzcyI6Im11amF5LmFAa3J5cHRvc2luZm9zeXMuY29tIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbW9iaWxlcGhvbmUiOiIrOTE3NzA4MjA0MDcxIiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjpbIlVzZXIiLCJTdWJzY3JpYmVyIl0sIlVzZXJJZCI6Ijk3NTQ1NGI1LTAxMmItNGQ1NC1iMTUyLWUzMGYzNmYzNjNlMiIsIlVzZXJOYW1lIjoiTVVKQVkiLCJFbWFpbCI6Im11amF5LmFAa3J5cHRvc2luZm9zeXMuY29tIiwiUGhvbmVOdW1iZXIiOiIrOTE3NzA4MjA0MDcxIiwiZXhwIjoxNzQ5NzE5MjYzLCJpc3MiOiJRYXRhciBMaXZpbmciLCJhdWQiOiJRYXRhciBMaXZpbmcifQ.2-lNmn8fpybnPmgn3jP0ftrvKVV09ydrnPjTJ8DkStw";
                await LoadSubscriptionDetailsAsync(3); 
                 SetHardcodedBusinessProfile();     
                StateHasChanged();

            }

            await base.OnAfterRenderAsync(firstRender);
        }

        public class StatItem
        {
            public string Title { get; set; }
            public string Value { get; set; }
            public string Icon { get; set; }
        }

        private void NavigateToEditProfile()
        {
            Navigation.NavigateTo("/edit-company");
        }
        private void NavigateToAdPost()
        {
            Navigation.NavigateTo("/classifieds/createform");
        }

      

        private void SetHardcodedBusinessProfile()
        {
            _businessProfile = new BusinessProfile
            {
                Name = "Luxury Store",
                CategoryName = "Preloved",
                Duration = "6 month Plus",
                ValidFrom = "2025-04-27",
                ValidTo = "2025-10-27",
                LogoUrl = "qln-images/subscription/CompanyLogo.svg"
            };
            StateHasChanged();
        }
        private async Task LoadSubscriptionDetailsAsync(int verticalId)
        {
            _isLoading = true;

            try
            {
                var response = await ClassfiedDashboardService.GetItemDashboard(_authToken);

                if (response?.ItemsDashboard != null)
                {
                    stats = new List<StatItem>
            {
                new() { Title = "Published Ads", Value = $"{response.ItemsDashboard.PublishedAds}", Icon = "PublishedAds.svg" },
                new() { Title = "Promoted Ads", Value = $"{response.ItemsDashboard.PromotedAds}", Icon = "PromotedAds.svg" },
                new() { Title = "Featured Ads", Value = $"{response.ItemsDashboard.FeaturedAds}", Icon = "FeaturedAds.svg" },
                new() { Title = "Refreshes", Value = $"{response.ItemsDashboard.Refreshes} / {response.ItemsDashboard.TotalAllowedRefreshes}", Icon = "Refreshes.svg" },
                new() { Title = "Impressions", Value = $"{response.ItemsDashboard.Impressions:N0}", Icon = "Impressions.svg" },
                new() { Title = "Views", Value = $"{response.ItemsDashboard.Views:N0}", Icon = "Views.svg" },
                new() { Title = "WhatsApp", Value = $"{response.ItemsDashboard.WhatsAppClicks}", Icon = "WhatsApp.svg" },
                new() { Title = "Calls", Value = $"{response.ItemsDashboard.Calls}", Icon = "Calls.svg" },
            };
                    publishedAds = response.ItemsAds?.PublishedAds ?? new();

                }
                else
                {
                    _errorMessage = "No subscription details found.";
                    stats = new();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading subscription details: " + ex.Message);
                _errorMessage = "Something went wrong while loading subscription stats.";
                stats = new();
            }
            finally 
            {
                _isLoading = false;

            }
           }


        public class SubscriptionDetailsResponse
        {
            public int CategoryId { get; set; }
            public string CategoryName { get; set; }
            public BusinessProfile BusinessProfile { get; set; }
            public SubscriptionStatistics SubscriptionStatistics { get; set; }
        }

        public class SubscriptionStatistics
        {
            public UsageTotal PublishedAds { get; set; }
            public UsageTotal PromotedAds { get; set; }
            public UsageTotal FeaturedAds { get; set; }
            public UsageTotal Refreshes { get; set; }
        }

        public class UsageTotal
        {
            public int Usage { get; set; }
            public int Total { get; set; }
        }

        public class ItemDashboardResponse
        {
            [JsonPropertyName("itemsDashboard")]
            public ItemsDashboard ItemsDashboard { get; set; }

            [JsonPropertyName("itemsAds")]
            public ItemsAds ItemsAds { get; set; }
        }

        public class ItemsDashboard
        {
            [JsonPropertyName("publishedAds")]
            public int PublishedAds { get; set; }

            [JsonPropertyName("promotedAds")]
            public int PromotedAds { get; set; }

            [JsonPropertyName("featuredAds")]
            public int FeaturedAds { get; set; }

            [JsonPropertyName("refreshes")]
            public int Refreshes { get; set; }

            [JsonPropertyName("remainingRefreshes")]
            public int RemainingRefreshes { get; set; }

            [JsonPropertyName("totalAllowedRefreshes")]
            public int TotalAllowedRefreshes { get; set; }

            [JsonPropertyName("refreshExpiry")]
            public DateTime RefreshExpiry { get; set; }

            [JsonPropertyName("impressions")]
            public int Impressions { get; set; }

            [JsonPropertyName("views")]
            public int Views { get; set; }

            [JsonPropertyName("whatsAppClicks")]
            public int WhatsAppClicks { get; set; }

            [JsonPropertyName("calls")]
            public int Calls { get; set; }
        }

        public class ItemsAds
        {
            [JsonPropertyName("publishedAds")]
            public List<AdModal> PublishedAds { get; set; }

            [JsonPropertyName("unpublishedAds")]
            public List<AdModal> UnpublishedAds { get; set; }
        }

        public class AdModal
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("title")]
            public string Title { get; set; }

            [JsonPropertyName("category")]
            public string Category { get; set; }

            [JsonPropertyName("description")]
            public string Description { get; set; }

            [JsonPropertyName("location")]
            public string Location { get; set; }

            [JsonPropertyName("subVertical")]
            public string SubVertical { get; set; }

            [JsonPropertyName("price")]
            public decimal Price { get; set; }

            [JsonPropertyName("phoneNumber")]
            public string PhoneNumber { get; set; }

            [JsonPropertyName("whatsappNumber")]
            public string WhatsappNumber { get; set; }

            [JsonPropertyName("createdDate")]
            public DateTime CreatedDate { get; set; }

            [JsonPropertyName("expiryDate")]
            public DateTime ExpiryDate { get; set; }

            [JsonPropertyName("userId")]
            public string UserId { get; set; }

            [JsonPropertyName("isFeatured")]
            public bool IsFeatured { get; set; }

            [JsonPropertyName("isPromoted")]
            public bool IsPromoted { get; set; }

            [JsonPropertyName("refreshExpiry")]
            public DateTime? RefreshExpiry { get; set; }

            [JsonPropertyName("remainingRefreshes")]
            public int RemainingRefreshes { get; set; }

            [JsonPropertyName("totalAllowedRefreshes")]
            public int TotalAllowedRefreshes { get; set; }

            [JsonPropertyName("impressions")]
            public int Impressions { get; set; }

            [JsonPropertyName("views")]
            public int Views { get; set; }

            [JsonPropertyName("calls")]
            public int Calls { get; set; }

            [JsonPropertyName("whatsAppClicks")]
            public int WhatsAppClicks { get; set; }

            [JsonPropertyName("shares")]
            public int Shares { get; set; }

            [JsonPropertyName("saves")]
            public int Saves { get; set; }

            [JsonPropertyName("imageUrls")]
            public List<string> ImageUrls { get; set; }
        }

    }


}