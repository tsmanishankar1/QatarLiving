using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services;
using QLN.Web.Shared.Services.Interface;
using System.Text.Json.Serialization;
using static QLN.Web.Shared.Models.ClassifiedsDashboardModel;

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
                _authToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6Ijk3NTQ1NGI1LTAxMmItNGQ1NC1iMTUyLWUzMGYzNmYzNjNlMiIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJNVUpBWSIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL2VtYWlsYWRkcmVzcyI6Im11amF5LmFAa3J5cHRvc2luZm9zeXMuY29tIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbW9iaWxlcGhvbmUiOiIrOTE3NzA4MjA0MDcxIiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjpbIkNvbXBhbnkiLCJTdWJzY3JpYmVyIl0sIlVzZXJJZCI6Ijk3NTQ1NGI1LTAxMmItNGQ1NC1iMTUyLWUzMGYzNmYzNjNlMiIsIlVzZXJOYW1lIjoiTVVKQVkiLCJFbWFpbCI6Im11amF5LmFAa3J5cHRvc2luZm9zeXMuY29tIiwiUGhvbmVOdW1iZXIiOiIrOTE3NzA4MjA0MDcxIiwiZXhwIjoxNzUwNTk2Njg3LCJpc3MiOiJRYXRhciBMaXZpbmciLCJhdWQiOiJRYXRhciBMaXZpbmcifQ.yR3NTs7yVNFZPS_qbDU9vmclI7rEKxlJ5fC5zq7llEo";
                await LoadSubscriptionDetailsAsync(3); 
                 SetHardcodedBusinessProfile();     
                StateHasChanged();

            }

            await base.OnAfterRenderAsync(firstRender);
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
                var response = await ClassfiedDashboardService.GetItemDashboard();

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
                    //publishedAds = response.ItemsAds?.PublishedAds ?? new();

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


       
        
    }


}