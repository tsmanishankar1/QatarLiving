using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using QLN.Web.Shared.Components.BreadCrumb;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services.Interface;
using static QLN.Web.Shared.Pages.Subscription.SubscriptionDetails;

namespace QLN.Web.Shared.Pages.Classifieds.Dashboards
{
    public class ItemDashboardBase : ComponentBase
    {

        [Inject] protected NavigationManager Navigation { get; set; } = default!;
        [Inject] protected IClassifiedDashboardService ClassfiedDashboardService { get; set; }
        [Inject] protected ICompanyProfileService CompanyProfileService { get; set; }
        [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; }

        protected List<BreadcrumbItem> breadcrumbItems = new();
        protected List<StatItem> stats = new();

        protected int _activeTabIndex;
        protected bool _isChecked = false;
        protected int _selectedAdsTab = 0;
        protected bool showCreateButton = true;
        protected string? _errorMessage;
        protected BusinessProfile? _businessProfile;
        protected List<AdModal> publishedAds = new();
        protected List<AdModal> unpublishedAds = new();
        protected bool _isLoading { get; set; } = true;
        private string _authToken;

        protected bool isCompanyLoading;
        protected CompanyProfileModel? companyProfile;

        protected override void OnInitialized()
        {
            breadcrumbItems = new()
            {
                new() { Label = "Classifieds", Url = "qln/classifieds" },
                new() { Label = "Dashboard", Url = "/qln/classified/dashboard/items", IsLast = true }
            };
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                var cookie = HttpContextAccessor.HttpContext?.Request.Cookies["qat"];
                _authToken = cookie;
                //await LoadSubscriptionDetailsAsync(3);
                ////SetHardcodedBusinessProfile();
                //await LoadCompanyProfileAsync();
                //StateHasChanged();
                var subscriptionTask = LoadSubscriptionDetailsAsync(3);
                var companyProfileTask = LoadCompanyProfileAsync();
                await Task.WhenAll(subscriptionTask, companyProfileTask);

            }

            await base.OnAfterRenderAsync(firstRender);
        }

        protected async Task LoadCompanyProfileAsync()
        {
            isCompanyLoading = true;
            StateHasChanged();

            try
            {
                companyProfile = await CompanyProfileService.GetCompanyProfileAsync(_authToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading company profile: {ex.Message}");
            }
            finally
            {
                isCompanyLoading = false;
                StateHasChanged();
            }
        }


        protected async Task LoadSubscriptionDetailsAsync(int verticalId)
        {
            _isLoading = true;
            StateHasChanged();

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
                StateHasChanged();


            }
        }
        protected void SetTab(int index)
        {
            _activeTabIndex = index;
        }
        protected void NavigateToEditProfile(string id)
        {
            Navigation.NavigateTo($"/qln/dashboard/company/edit/{id}");
        }
        protected void NavigateToCreateProfile()
        {
            var verticalId = 3;
            var categoryId = 1;


            Navigation.NavigateTo($"/qln/dashboard/company/create/{verticalId}/{categoryId}");
        }

        protected void NavigateToAdPost()
        {
            Navigation.NavigateTo("/classifieds/createform");
        }

        protected void SetHardcodedBusinessProfile()
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

    }
}
