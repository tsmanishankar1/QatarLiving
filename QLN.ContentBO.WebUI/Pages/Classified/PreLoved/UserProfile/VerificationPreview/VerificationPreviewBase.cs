using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Interfaces;
using MudBlazor;
namespace QLN.ContentBO.WebUI.Pages.Classified.PreLoved.UserProfile.VerificationPreview
{
    public partial class VerificationPreviewBase : ComponentBase
    {
         [Parameter]
        public Guid? CompanyId { get; set; }
        protected CompanyProfileItem CompanyDetails { get; set; } = new();
         [Inject]
        protected NavigationManager Navigation { get; set; }
        protected VerifiedStatus CurrentStatus
        {
            get
            {
                if (CompanyDetails != null && Enum.IsDefined(typeof(VerifiedStatus), CompanyDetails.CompanyVerificationStatus))
                {
                    return (VerifiedStatus)CompanyDetails.CompanyVerificationStatus;
                }
                return VerifiedStatus.NotVerified;
            }
        }

        [Inject] public ILogger<VerificationPreviewBase> Logger { get; set; }
         [Inject] ISnackbar Snackbar { get; set; }
        [Inject]
        public IClassifiedService ClassifiedService { get; set; }
        protected override async Task OnParametersSetAsync()
        {
            if (CompanyId != null)
            {
                CompanyDetails = await GetCompanyById();
            }
            else
            {
                CompanyDetails = new CompanyProfileItem();
            }
        }
        private async Task<CompanyProfileItem> GetCompanyById()
        {
            try
            {
                var apiResponse = await ClassifiedService.GetCompanyProfileById(CompanyId ?? Guid.Empty);
                if (apiResponse.IsSuccessStatusCode)
                {
                    var response = await apiResponse.Content.ReadFromJsonAsync<CompanyProfileItem>();
                    return response ?? new CompanyProfileItem();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetCompanyById");
            }
            return new CompanyProfileItem();
        }
         protected async Task UpdateStatus(CompanyUpdateActions actionRequest)
        {
            var response = await ClassifiedService.UpdateCompanyActions(actionRequest);
            if (response.IsSuccessStatusCode)
            {
                Snackbar.Add(
                actionRequest.CompanyVerificationStatus switch
                {
                    VerifiedStatus.Approved => "User Profile Approved Successfully",
                    VerifiedStatus.Rejected => "User Profile Rejected Successfully",
                    VerifiedStatus.NeedChanges => "Status Moved to Need Changes Successfully",
                    _ => "User Profile Updated Successfully"
                },
                    Severity.Success
                );
                Navigation.NavigateTo($"/manage/classified/items/user/verification/profile");
                
            }
            else
            {
                Snackbar.Add("Failed to update ad status", Severity.Error);
            }
        }
    }
}
