using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.JSInterop;
using MudBlazor;
using QLN.Web.Shared.Helpers;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace QLN.Web.Shared.Pages.Subscription
{
    public partial class SubscriptionDetails : ComponentBase
    {
        [Inject] private NavigationManager Navigation { get; set; } = default!;


        [Inject] protected IJSRuntime _jsRuntime { get; set; }
        [Inject] private HttpClient Http { get; set; } = default!;
        [Inject] private ApiService Api { get; set; } = default!;

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

        //private List<StatItem> stats = new()
        //{
        //     new() { Title = "Published Ads", Value = "5 out of 15", Icon = "PublishedAds.svg" },
        //new() { Title = "Promoted Ads", Value = "1 out of 2", Icon = "PromotedAds.svg" },
        //new() { Title = "Featured Ads", Value = "2 out of 2", Icon = "FeaturedAds.svg" },
        //new() { Title = "Refreshes", Value = "5 out on 75", Icon = "Refreshes.svg" },
        //new() { Title = "Impressions", Value = "52,034", Icon = "Impressions.svg" },
        //new() { Title = "Views", Value = "52,034", Icon = "Views.svg" },
        //new() { Title = "WhatsApp", Value = "52,034", Icon = "WhatsApp.svg" },
        //new() { Title = "Calls", Value = "52,034", Icon = "Calls.svg" },
        //};

        protected override async void OnInitialized()
        {
            breadcrumbItems = new()
        {
            new() { Label = "Classifieds", Url = "classifieds" },
            new() { Label = "Dashboard", Url = "/subscription-details",IsLast=true }
        };
          
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _authToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
                if (string.IsNullOrWhiteSpace(_authToken))
                {
                    _authToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjU0NTZhZTY0LTNjMGMtNDJjYS04MGIxLTBjOWQ2YjBkYmY5MiIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJqYXNyMjciLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9lbWFpbGFkZHJlc3MiOiJqYXN3YW50aC5yQGtyeXB0b3NpbmZvc3lzLmNvbSIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL21vYmlsZXBob25lIjoiKzkxOTAwMzczODEzOCIsIlVzZXJJZCI6IjU0NTZhZTY0LTNjMGMtNDJjYS04MGIxLTBjOWQ2YjBkYmY5MiIsIlVzZXJOYW1lIjoiamFzcjI3IiwiRW1haWwiOiJqYXN3YW50aC5yQGtyeXB0b3NpbmZvc3lzLmNvbSIsIlBob25lTnVtYmVyIjoiKzkxOTAwMzczODEzOCIsImV4cCI6MTc0NjY5NTE0NywiaXNzIjoiUWF0YXIgTGl2aW5nIiwiYXVkIjoiUWF0YXIgTGl2aW5nIn0.KYxgzCBr5io7jm9SDzh2GE7GADKZ38k3kivgx6gC3PQ";
                }
                await LoadSubscriptionDetailsAsync(3); // Now moved here
                await LoadBusinessProfileAsync();      // Token is now ready
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

        private async Task LoadBusinessProfileAsync()
        {
            try
            {
                var response = await Api.GetAsyncWithToken<SubscriptionDetailsResponse>("api/subscription/details?verticalId=3", _authToken);
                if (response?.BusinessProfile is not null)
                {
                    _businessProfile = response.BusinessProfile;
                }
                else
                {
                    SetHardcodedBusinessProfile();
                }
            }
            catch
            {
                SetHardcodedBusinessProfile();
            }
            StateHasChanged();
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
                LogoUrl = "images/subscription/CompanyLogo.svg"
            };
            StateHasChanged();
        }
        private async Task LoadSubscriptionDetailsAsync(int verticalId)
        {
            try
            {
                var url = $"api/subscription/details?verticalId={verticalId}";

                var response = await Api.GetAsyncWithToken<SubscriptionDetailsResponse>(url,_authToken);

                if (response != null)
                {
                    _errorMessage = null;
                    stats = new List<StatItem>()
            {
                new() { Title = "Published Ads", Value = $"{response.SubscriptionStatistics.PublishedAds.Usage} out of {response.SubscriptionStatistics.PublishedAds.Total}", Icon = "PublishedAds.svg" },
                new() { Title = "Promoted Ads", Value = $"{response.SubscriptionStatistics.PromotedAds.Usage} out of {response.SubscriptionStatistics.PromotedAds.Total}", Icon = "PromotedAds.svg" },
                new() { Title = "Featured Ads", Value = $"{response.SubscriptionStatistics.FeaturedAds.Usage} out of {response.SubscriptionStatistics.FeaturedAds.Total}", Icon = "FeaturedAds.svg" },
                new() { Title = "Refreshes", Value = $"{response.SubscriptionStatistics.Refreshes.Usage} out of {response.SubscriptionStatistics.Refreshes.Total}", Icon = "Refreshes.svg" },
                new() { Title = "Impressions", Value = "52,034", Icon = "Impressions.svg" },
                new() { Title = "Views", Value = "52,034", Icon = "Views.svg" },
                new() { Title = "WhatsApp", Value = "52,034", Icon = "WhatsApp.svg" },
                new() { Title = "Calls", Value = "52,034", Icon = "Calls.svg" },
            };
                }
                else
                {
                    _errorMessage = "No subscription details found.";
                    stats = new List<StatItem>();
                }
            }
            catch (HttpRequestException ex)
            {
                HttpErrorHelper.HandleHttpException(ex, Snackbar);
            }
        }

        private List<AdItem> publishedAds = new()
    {
        new() {
            Category = "Pre-loved / Bags",
            Title = "Used Gucci bag authentic",
            Location = "Westbay",
            Price = "5,000 QAR",
            ExpiryDate = "04/12/25 - 3:15 PM",
            IsFeatured = true,
            Impressions = "1,034",
            Views = "42",
            Calls = "40",
            WhatsApp = "40",
            Shares = "40",
            Saves = "40",
            ImageUrl = ".images/subscription/Gucci.jpg"
        },
        new() {
            Category = "Pre-loved / Bags",
            Title = "Used Gucci bag authentic",
            Location = "Westbay",
            Price = "5,000 QAR",
            ExpiryDate = "04/12/25 - 3:15 PM",
            IsFeatured = false,
            Impressions = "1,034",
            Views = "42",
            Calls = "40",
            WhatsApp = "40",
            Shares = "40",
            Saves = "40",
            ImageUrl = "images/subscription/Gucci.jpg"
        }
    };
        private List<AdItem> unpublishedAds = new()
    {
        new() {
            Category = "Pre-loved / Bags",
            Title = "Used Gucci bag authentic",
            Location = "Westbay",
            Price = "5,000 QAR",
            ImageUrl = "images/subscription/Gucci.jpg"
        },
        new() {
            Category = "Pre-loved / Bags",
            Title = "Used Gucci bag authentic",
            Location = "Westbay",
            Price = "5,000 QAR",
            ImageUrl = "images/subscription/Gucci.jpg"
        },
    };
     
        public class AdItem
        {
            public string Category { get; set; }
            public string Title { get; set; }
            public string Location { get; set; }
            public string Price { get; set; }
            public string ExpiryDate { get; set; }
            public bool IsFeatured { get; set; }
            public string Impressions { get; set; }
            public string Views { get; set; }
            public string Calls { get; set; }
            public string WhatsApp { get; set; }
            public string Shares { get; set; }
            public string Saves { get; set; }
            public string ImageUrl { get; set; }
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
    }
}