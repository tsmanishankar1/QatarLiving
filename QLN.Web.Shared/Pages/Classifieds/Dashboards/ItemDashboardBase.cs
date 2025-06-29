using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services.Interface;
using System.ComponentModel.DataAnnotations;
using static QLN.Web.Shared.Models.ClassifiedsDashboardModel;

namespace QLN.Web.Shared.Pages.Classifieds.Dashboards
{
    public class ItemDashboardBase : ComponentBase
    {

        [Inject] protected NavigationManager Navigation { get; set; } = default!;
        [Inject] protected ISnackbar Snackbar { get; set; }

        [Inject] protected IClassifiedDashboardService ClassfiedDashboardService { get; set; }
        [Inject] protected ICompanyProfileService CompanyProfileService { get; set; }

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



        protected string searchTerm = string.Empty;

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
            publishedAds.Clear();
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
            unpublishedAds.Clear();
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


        protected async Task HandleSearch(string term)
        {
            Console.WriteLine($"[HandleSearch] Search term passed: {term}");
            CurrentPage = 1;
            await ReloadAds(term, sortOption);
        }

        protected async Task HandleSort(int option)
        {
            Console.WriteLine($"Sort option passed: {option}");
            CurrentPage = 1;
            await ReloadAds(searchTerm, option);
        }

        private async Task ReloadAds(string? search = null, int? sort = null)
        {
            var effectiveSearch = search ?? searchTerm;
            var effectiveSort = sort ?? sortOption;

            // Fetch only, do not clear current data yet
            if (_selectedAdsTab == 0)
            {
                _isPublishedLoading = true;
                StateHasChanged();

                var newAds = await ClassfiedDashboardService
                    .GetPublishedAds(CurrentPage, PageSize, effectiveSearch, effectiveSort)
                    ?? new();

                searchTerm = effectiveSearch;
                sortOption = effectiveSort;

                publishedAds = newAds;
                _isPublishedLoading = false;
            }
            else
            {
                _isUnpublishedLoading = true;
                StateHasChanged();

                var newAds = await ClassfiedDashboardService
                    .GetUnpublishedAds(CurrentPage, PageSize, effectiveSearch, effectiveSort)
                    ?? new();

                searchTerm = effectiveSearch;
                sortOption = effectiveSort;

                unpublishedAds = newAds;
                _isUnpublishedLoading = false;
            }

            StateHasChanged();
        }

        protected async Task HandlePageChange(int page)
        {
            CurrentPage = page;
            await ReloadAds();
        }



        protected async Task HandlePageSizeChange(int size)
        {
            PageSize = size;
            CurrentPage = 1;
            await ReloadAds();
        }

        private async Task ReloadAds()
        {
            Console.WriteLine($"[ReloadAds] Tab: {_selectedAdsTab}, Search: {searchTerm}, Sort: {sortOption}");

            publishedAds.Clear();
            unpublishedAds.Clear();
            StateHasChanged();

            if (_selectedAdsTab == 0)
            {
                await LoadPublishedAds();
            }
            else
            {
                await LoadUnpublishedAds();
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

        protected void OnEditAd(string adId)
        {
            Navigation.NavigateTo($"/qln/classifieds/editform/{adId}");
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
        public enum AdStatus
        {
            Draft = 0,
            PendingApproval = 1,
            Approved = 2,
            Published = 3,
            Unpublished = 4,
            Rejected = 5,
            Expired = 6,
            NeedsModification = 7
        }

        protected string GetStatusLabel(int status)
        {
            return Enum.IsDefined(typeof(AdStatus), status) ? ((AdStatus)status).ToString() : "Unknown";
        }

        protected string GetStatusStyle(int status)
        {
            return status switch
            {
                3 => "background-color: #E6F4EA; border: 1px solid #2E7D32; color: #2E7D32;",
                4 => "background-color: #FFF9E5; border: 1px solid #F9A825; color: #F9A825;",
                6 => "background-color: #FFEAEA; border: 1px solid #D32F2F; color: #D32F2F;",
                1 => "background-color: #E3F2FD; border: 1px solid #1976D2; color: #1976D2;",
                _ => "background-color: #F5F5F5; border: 1px solid #BDBDBD; color: #616161;"
            };
        }

    }
}
