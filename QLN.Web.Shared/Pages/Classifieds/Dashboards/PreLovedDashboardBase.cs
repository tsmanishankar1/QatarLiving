using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using QLN.Web.Shared.Components.BreadCrumb;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services.Interface;
using System.ComponentModel.DataAnnotations;
using static QLN.Web.Shared.Pages.Subscription.SubscriptionDetails;

namespace QLN.Web.Shared.Pages.Classifieds.Dashboards
{
    public class PreLovedDashboardBase : ComponentBase
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
                new() { Label = "Dashboard", Url = "/qln/classified/dashboard/preloved", IsLast = true }
            };
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                //var cookie = HttpContextAccessor.HttpContext?.Request.Cookies["qat"];
                _authToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6Ijk3NTQ1NGI1LTAxMmItNGQ1NC1iMTUyLWUzMGYzNmYzNjNlMiIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJNVUpBWSIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL2VtYWlsYWRkcmVzcyI6Im11amF5LmFAa3J5cHRvc2luZm9zeXMuY29tIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbW9iaWxlcGhvbmUiOiIrOTE3NzA4MjA0MDcxIiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjpbIkNvbXBhbnkiLCJTdWJzY3JpYmVyIl0sIlVzZXJJZCI6Ijk3NTQ1NGI1LTAxMmItNGQ1NC1iMTUyLWUzMGYzNmYzNjNlMiIsIlVzZXJOYW1lIjoiTVVKQVkiLCJFbWFpbCI6Im11amF5LmFAa3J5cHRvc2luZm9zeXMuY29tIiwiUGhvbmVOdW1iZXIiOiIrOTE3NzA4MjA0MDcxIiwiZXhwIjoxNzUwODUwOTg5LCJpc3MiOiJodHRwczovL3Rlc3QucWF0YXJsaXZpbmcuY29tIiwiYXVkIjoiaHR0cHM6Ly90ZXN0LnFhdGFybGl2aW5nLmNvbSJ9.QfCXZnvd6FFVYPi4vX7j9777WJ_lgkKhJSFPijKoCCg";
                ////await LoadSubscriptionDetailsAsync(3);
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
                var response = await ClassfiedDashboardService.GetPreLovedDashboard(_authToken);

                if (response?.preLovedDashboard != null)
                {
                    stats = new List<StatItem>
            {
                new() { Title = "Published Ads", Value = $"{response.preLovedDashboard.PublishedAds}", Icon = "PublishedAds.svg" },
                new() { Title = "Promoted Ads", Value = $"{response.preLovedDashboard.PromotedAds}", Icon = "PromotedAds.svg" },
                new() { Title = "Featured Ads", Value = $"{response.preLovedDashboard.FeaturedAds}", Icon = "FeaturedAds.svg" },
                new() { Title = "Refreshes", Value = $"{response.preLovedDashboard.Refreshes} / {response.preLovedDashboard.TotalAllowedRefreshes}", Icon = "Refreshes.svg" },
                new() { Title = "Impressions", Value = $"{response.preLovedDashboard.Impressions:N0}", Icon = "Impressions.svg" },
                new() { Title = "Views", Value = $"{response.preLovedDashboard.Views:N0}", Icon = "Views.svg" },
                new() { Title = "WhatsApp", Value = $"{response.preLovedDashboard.WhatsAppClicks}", Icon = "WhatsApp.svg" },
                new() { Title = "Calls", Value = $"{response.preLovedDashboard.Calls}", Icon = "Calls.svg" },
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
