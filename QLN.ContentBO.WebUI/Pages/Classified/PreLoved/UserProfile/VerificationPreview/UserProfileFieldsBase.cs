using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Components;

namespace QLN.ContentBO.WebUI.Pages.Classified.PreLoved.UserProfile.VerificationPreview
{
    public class UserProfileFieldsBase : QLComponentBase
    {
        // Basic Info
        protected string CompanyName => "Lulu Hypermarket";
        protected string Country => "Qatar";
        protected string City => "Doha";
        protected string Location => "West Bay";
        protected string PhoneNumber => "+974 2234 5675";
        protected string WhatsAppNumber => "+974 2234 5675";
        protected string WebsiteUrl => "www.website.com";

        // Operating Hours
        protected string StartDay => "Sunday";
        protected string EndDay => "Thursday";
        protected string StartHour => "8:00 AM";
        protected string EndHour => "5:00 PM";

        // Company Profile
        protected string BusinessNature => "Hypermarket";
        protected string CompanySize => "500+";
        protected string CompanyType => "MNC";
        protected string UserDesignation => "Social Media Manager";
        protected string BusinessDescription => "For sale is a gently used White Google Pixel 6 Pro XL, a top-of-the-line smartphone...";

        // License & Visibility
        protected string CRNumber => "202025";
        protected string CompanyReferenceFileName => "company_cr_2025.pdf";
        protected string CompanyReferenceUrl => $"/files/company_cr_2025.pdf";
    }
}
