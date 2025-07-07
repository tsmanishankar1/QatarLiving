using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services.Interface;
using static QLN.Web.Shared.Models.ClassifiedsDashboardModel;

namespace QLN.Web.Shared.Pages.Classifieds.Dashboards
{
    public class PublishedAdsComponentBase : ComponentBase
    {
        [Parameter]
        public List<AdModal> Ads { get; set; } = new();

        [Parameter]
        public bool _isPublishedLoading { get; set; }

        //[Parameter]
        //public string DashboardType { get; set; }
        [Parameter]
        public VerticalConstants.SubVerticalId DashboardType { get; set; }


        [Parameter]
        public EventCallback<List<string>> OnPublish { get; set; }

        [Parameter]
        public EventCallback<string> OnEdit { get; set; }

        [Parameter]
        public EventCallback<string> OnPreview { get; set; }

        [Parameter]
        public EventCallback<string> OnRemove { get; set; }

        [Parameter]
        public EventCallback<List<string>> OnBulkPublish { get; set; }

        [Inject]
        protected NavigationManager Navigation { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }

        [Inject] protected IClassifiedDashboardService ClassfiedDashboardService { get; set; }

        protected bool isChecked { get; set; }


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
        public List<string> SelectedAdIds = new();
        protected void OnAdToggled(AdModal ad, bool value)
        {
            ad.IsSelected = value;

            if (value)
            {
                if (!SelectedAdIds.Contains(ad.Id))
                    SelectedAdIds.Add(ad.Id);
            }
            else
            {
                SelectedAdIds.Remove(ad.Id);
            }
        }
        protected void SelectAll()
        {
            foreach (var ad in Ads)
            {
                ad.IsSelected = true;
                if (!SelectedAdIds.Contains(ad.Id))
                    SelectedAdIds.Add(ad.Id);
            }
        }

        protected void UnselectAll()
        {
            foreach (var ad in Ads)
            {
                ad.IsSelected = false;
            }
            SelectedAdIds.Clear();
        }

        protected async Task PublishAllSelected()
        {
            if (SelectedAdIds.Any())
            {
                await OnPublish.InvokeAsync(SelectedAdIds);
                UnselectAll();
            }
        }
        protected void NavigateToFeaturePage()
        {
            Navigation.NavigateTo("/qln/paytofeature");
        }

        protected async Task OnRefreshAd(string adId)
        {
            var success = await ClassfiedDashboardService.RefreshAdAsync(adId, (int)DashboardType);

            if (success)
                Snackbar.Add("Ad refreshed successfully!", Severity.Success);
            else
                Snackbar.Add("Failed to refresh ad.", Severity.Error);
        }

    }
}