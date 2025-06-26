using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using MudBlazor;
using QLN.Web.Shared.Components.BreadCrumb;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services.Interface;
using System.ComponentModel.DataAnnotations;
using static QLN.Web.Shared.Models.ClassifiedsDashboardModel;
using static QLN.Web.Shared.Pages.Subscription.SubscriptionDetails;

namespace QLN.Web.Shared.Pages.Classifieds.Dashboards
{
    public class StoresDashboardBase : ComponentBase
    {
        [Inject] protected NavigationManager Navigation { get; set; } = default!;
        [Inject] protected IClassifiedDashboardService ClassfiedDashboardService { get; set; }
        [Inject] protected ICompanyProfileService CompanyProfileService { get; set; }
        [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }

        protected List<QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem> breadcrumbItems = new();
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


        private int currentPage = 1;
        private int pageSize = 12;
        private string searchTerm = string.Empty;
        private int sortOption = 2;

        protected bool _isPublishedLoading = false;
        protected bool _isUnpublishedLoading = false;



        protected override void OnInitialized()
        {
            breadcrumbItems = new()
            {
                new() { Label = "Classifieds", Url = "qln/classifieds" },
                new() { Label = "Dashboard", Url = "/qln/classified/dashboard/preloved", IsLast = true }
            };
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                
                var subscriptionTask = LoadSubscriptionDetailsAsync(3);
                var companyProfileTask = LoadCompanyProfileAsync();
                await LoadPublishedAds();
                await LoadUnpublishedAds();
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
                companyProfile = await CompanyProfileService.GetCompanyProfileAsync();
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
        private async Task LoadPublishedAds()
        {
            _isPublishedLoading = true;
            StateHasChanged();

            try
            {
                publishedAds = await ClassfiedDashboardService
                    .GetStoresPublishedAds(currentPage, pageSize, searchTerm, sortOption)
                    ?? new();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading published ads: " + ex.Message);
                _errorMessage = "Something went wrong while loading published ads.";
                publishedAds = new();
            }
            finally
            {
                _isPublishedLoading = false;
                StateHasChanged();
            }
        }

        private async Task LoadUnpublishedAds()
        {
            _isUnpublishedLoading = true;
            StateHasChanged();

            try
            {
                unpublishedAds = await ClassfiedDashboardService
                    .GetStoresUnPublishedAds(currentPage, pageSize, searchTerm, sortOption)
                    ?? new();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading unpublished ads: " + ex.Message);
                _errorMessage = "Something went wrong while loading unpublished ads.";
                unpublishedAds = new();
            }
            finally
            {
                _isUnpublishedLoading = false;
                StateHasChanged();
            }
        }
        protected void OnPublishAd(string adId)
        {
            Console.WriteLine($"Publish clicked for ad ID: {adId}");
        }

        protected void OnEditAd(string adId)
        {
            Navigation.NavigateTo($"/qln/dashboard/ad/edit/{adId}");
        }
        protected void UnPublishAd(string adId)
        {
            Console.WriteLine($"Publish clicked for ad ID: {adId}");
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
            var categoryId = 2;


            Navigation.NavigateTo($"/qln/dashboard/company/create/{verticalId}/{categoryId}");
        }

        protected void NavigateToAdPost()
        {
            Navigation.NavigateTo("/classifieds/createform");
        }
        public static string GetDisplayName<TEnum>(TEnum enumValue) where TEnum : Enum
        {
            var member = typeof(TEnum).GetMember(enumValue.ToString()).FirstOrDefault();
            var displayAttr = member?.GetCustomAttributes(typeof(DisplayAttribute), false)
                                     .FirstOrDefault() as DisplayAttribute;
            return displayAttr?.Name ?? enumValue.ToString();
        }
    }

}
