using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Components;
using QLN.ContentBO.WebUI.Models;
namespace QLN.ContentBO.WebUI.Pages.Classified.PreLoved.UserProfile.VerificationPreview
{
    public class UserProfileFieldsBase : QLComponentBase
    {
       [Parameter]
        public CompanyProfileItem CompanyDetails { get; set; } = new();
        // Basic Info
        protected string CompanyName => CompanyDetails.CompanyName ?? "Default Company Name";
        protected string Country => CompanyDetails.Country ?? "Default Country";
        protected string City => CompanyDetails.City ?? "Default City";
        protected string Location => CompanyDetails.BranchLocations.Any() 
        ? string.Join(", ", CompanyDetails.BranchLocations) 
        : "No Locations Available";
        protected string PhoneNumber => CompanyDetails.PhoneNumber ?? "Default Phone Number";
        protected string WhatsAppNumber => CompanyDetails.WhatsAppNumber ?? "Default WhatsApp Number";
        protected string WebsiteUrl => CompanyDetails.WebsiteUrl ?? "https://defaultwebsite.com";

        // Operating Hours
        protected string StartDay => CompanyDetails.StartDay ?? "Monday";
        protected string EndDay => CompanyDetails.EndDay ?? "Friday";
        protected string StartHour =>  CompanyDetails.StartHour ?? "9:00 AM";
        protected string EndHour => CompanyDetails.EndHour ?? "5:00 PM";

        // Company Profile
        protected string BusinessNature => CompanyDetails.NatureOfBusiness.Any() 
            ? string.Join(", ", CompanyDetails.NatureOfBusiness.Select(n => n.ToString())) 
            : "Not Specified";
        protected string CompanySize => CompanyDetails.CompanySize switch
        {
            1 => "0-1",
            2 => "11–50",
            3 => "51–200",
            4 => "201–500",
            5 => "500+",
            _ => "Not Specified"
        };
        protected string CompanyType => CompanyDetails.CompanyType switch
        {
            1 => "SME",
            2 => "Enterprise",
            3 => "MNC",
            4 => "Government",
            _ => "Not Specified"
        };
        protected string UserDesignation => CompanyDetails.UserDesignation ?? "Not Specified";
        protected string BusinessDescription => CompanyDetails.BusinessDescription ?? "No Description Available";

        // License & Visibility
        protected string CRNumber => CompanyDetails.CrNumber.ToString() ?? "Not Specified";
        protected string CompanyReferenceFileName
        {
            get
            {
                var crDoc = CompanyDetails?.CrDocument;

                if (string.IsNullOrWhiteSpace(crDoc))
                    return string.Empty;

                if (Uri.TryCreate(crDoc, UriKind.Absolute, out var uri))
                {
                    return Path.GetFileName(uri.AbsolutePath);
                }
                return crDoc;
            }
        }

    }
}
