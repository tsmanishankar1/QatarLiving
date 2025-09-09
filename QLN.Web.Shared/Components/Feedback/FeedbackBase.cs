using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.Web.Shared.Models.FeedbackRequest;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services;
using System.Security.Claims;

public partial class FeedbackBase : ComponentBase
{
    protected FeedbackFormModel formModel = new();
    protected bool isSubmitting = false;
    protected bool? submissionResult = null;
    protected bool ShowForm = false;
    protected MudForm form;

    [Inject] protected FeedbackService FeedbackService { get; set; } = default!;
    [Inject] protected CookieAuthStateProvider CookieAuthenticationStateProvider { get; set; } = default!;

    protected readonly List<string> Categories = new()
    {
        "Account Blocked",
        "Account Deletion",
        "Ad Approval",
        "Ad Unpublished/Removed",
        "Ads posted in incorrect categories",
        "Cannot Edit/Update Ad",
        "Cannot Post",
        "Cannot Refresh Ad",
        "Cannot View Ad",
        "Can't Login",
        "Closed Account",
        "Default (Do Not Use)",
        "Double payment",
        "Email Update",
        "Fraudulent/Infringement Report",
        "General Complaints",
        "General Inquiry",
        "Illegal Content Reports",
        "Issue in Payment (Cannot Pay)",
        "Mobile Number Update",
        "Multiple Ad Posting",
        "Number Removal",
        "Payment pending",
        "Post Ad",
        "Refund request",
        "Request for Ad Removal",
        "Request for Comment Removal",
        "Spam",
        "Subscription/Account Issues",
        "System Downtime/Outage",
        "Unauthorized use of contact details by another user",
        "Unauthorized/Unusual Login",
        "Username Update",
        "Verification",
        "Wrong Category"
    };
    private string Name { get; set; } = String.Empty;
    private string Email { get; set; } = String.Empty;
    private string Mobile { get; set; } = String.Empty;
    private int UserId { get; set; }
    protected override async Task OnInitializedAsync()
    {
        var authState = await CookieAuthenticationStateProvider.GetAuthenticationStateAsync();
        if (authState != null)
        {
            var user = authState.User;
            if (user.Identity != null && user.Identity.IsAuthenticated)
            {
                Name = user.FindFirst(ClaimTypes.Name)?.Value;
                Email = user.FindFirst(ClaimTypes.Email)?.Value ?? user.FindFirst("email")?.Value;
                Mobile = user.FindFirst(ClaimTypes.MobilePhone)?.Value;
                UserId = int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : 0;
            }
            if (!string.IsNullOrEmpty(Name))
            {
                formModel.Name = Name;
            }
            if (!string.IsNullOrEmpty(Email))
            {
                formModel.Email = Email;
            }
            if (!string.IsNullOrEmpty(Mobile))
            {
                formModel.Mobile = Mobile;
            }
        }
    }
    protected async Task HandleValidSubmit()
    {
        isSubmitting = true;
        submissionResult = null;
        StateHasChanged();

        await form.Validate();

        if (!form.IsValid)
        {
            isSubmitting = false;
            StateHasChanged();
            return;
        }

        string userId = UserId != 0 ? UserId.ToString() : "";

        try
        {
            submissionResult = await FeedbackService.SubmitFeedbackAsync(formModel, userId);
        }
        catch
        {
            submissionResult = false;
        }

        isSubmitting = false;
        StateHasChanged();
    }

    protected void ResetForm()
    {
        if(string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(Email))
        {
            formModel = new FeedbackFormModel();
        }
        submissionResult = null;
        ShowForm = false;
        StateHasChanged();
    }

    protected void Open()
    {
        ShowForm = true;
        StateHasChanged();
    }
}