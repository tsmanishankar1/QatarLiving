using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using MudBlazor;
using QLN.Common.DTO_s;
using QLN.Web.Shared.Components.BreadCrumb;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services.Interface;
using System.ComponentModel.DataAnnotations;
using static QLN.Web.Shared.Models.ClassifiedsDashboardModel;
using static QLN.Web.Shared.Pages.Subscription.SubscriptionDetails;

namespace QLN.Web.Shared.Pages.Classifieds.Dashboards
{
    public class ItemDashboardBase : ComponentBase
    {

        [Inject] protected NavigationManager Navigation { get; set; } = default!;
        [Inject] protected ISnackbar Snackbar { get; set; }

        [Inject] protected IClassifiedDashboardService ClassfiedDashboardService { get; set; }
        [Inject] protected ICompanyProfileService CompanyProfileService { get; set; }
        [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; }

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
        protected bool _isPublishedLoading = false;
        protected bool _isUnpublishedLoading = false;

        private string _authToken;

        protected bool isCompanyLoading;
        protected CompanyProfileModel? companyProfile;


    
        private string searchTerm = string.Empty;
        private int sortOption = 2;


        public EventCallback<int> OnPageChange { get; set; }
        public EventCallback<int> OnPageSizeChange { get; set; }
        public int CurrentPage = 1;
        public int PageSize = 12;

        public int TotalItems = 10;

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
                  await LoadSubscriptionDetailsAsync(3);
                await LoadUnpublishedAds();
                await LoadPublishedAds();

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
                    .GetPublishedAds(CurrentPage, PageSize, searchTerm, sortOption)
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
                    .GetUnpublishedAds(CurrentPage, PageSize, searchTerm, sortOption)
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

        protected async Task OnPublishAd(string adId)
        {
            try
            {
                var result = await ClassfiedDashboardService.PublishAdAsync(adId);
                if (result)
                {
                    Snackbar.Add("Ad published successfully", Severity.Success);
                    await LoadUnpublishedAds();
                    await LoadPublishedAds();  
                }
                else
                {
                    Snackbar.Add("Failed to publish ad.", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in OnPublishAd: " + ex.Message);
                Snackbar.Add("An error occurred.", Severity.Error);
            }
        }


        protected async Task UnPublishAd(string adId)
        {
            try
            {
                var result = await ClassfiedDashboardService.UnPublishAdAsync(adId);
                if (result)
                {
                    Snackbar.Add("Ad unpublished successfully", Severity.Success);
                    await LoadUnpublishedAds();
                    await LoadPublishedAds();
                }
                else
                {
                    Snackbar.Add("Failed to un-publish ad.", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in OnPublishAd: " + ex.Message);
                Snackbar.Add("An error occurred.", Severity.Error);
            }
        }
        protected async void HandlePageChange(int newPage)
        {
            await OnPageChange.InvokeAsync(newPage);
        }

        protected async void HandlePageSizeChange(int newSize)
        {
            await OnPageSizeChange.InvokeAsync(newSize);
        }

        protected void OnEditAd(string adId)
        {
            Navigation.NavigateTo($"/qln/classifieds/editform{adId}");
        }
        protected void onPreview(string adId)
        {
            Navigation.NavigateTo($"/qln/classifieds/items/details/{adId}");
        }

        protected void onRemove(string adId)
        {
            throw new NotImplementedException("Remove functionality is not implemented yet.");
        }
        protected void SetTab(int index)
        {
            _activeTabIndex = index;
        }
        protected void NavigateToEditProfile(string id)
        {
            Navigation.NavigateTo($"/qln/dashboard/company/edit/{id}");
        }
        protected void NavigateToPurshaseRefresh()
        {
            Navigation.NavigateTo("/qln/dashboard/refresh/purchase");
        }
        protected void NavigateToCreateProfile()
        {
            var verticalId = 3;
            var categoryId = 1;


            Navigation.NavigateTo($"/qln/dashboard/company/create/{verticalId}/{categoryId}");
        }

        protected void NavigateToAdPost()
        {
            Navigation.NavigateTo("/qln/classifieds/createform");
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
        public static string GetDisplayName<TEnum>(TEnum enumValue) where TEnum : Enum
        {
            var member = typeof(TEnum).GetMember(enumValue.ToString()).FirstOrDefault();
            var displayAttr = member?.GetCustomAttributes(typeof(DisplayAttribute), false)
                                     .FirstOrDefault() as DisplayAttribute;
            return displayAttr?.Name ?? enumValue.ToString();
        }

    }
}
