using Azure.Search.Documents.Indexes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
        public class CompanyProfileIndex
        {
            [SimpleField(IsKey = true)]
            public string CompanyId { get; set; }

            [SearchableField(IsFilterable = true, IsFacetable = true)]
            public string CompanyName { get; set; } = string.Empty;

            [SearchableField(IsFilterable = true)]
            public string Country { get; set; } = string.Empty;

            [SearchableField(IsFilterable = true)]
            public string City { get; set; } = string.Empty;

            [SearchableField(IsFilterable = true)]
            public string PhoneNumber { get; set; } = string.Empty;

            [SearchableField(IsFilterable = true)]
            public string PhoneNumberCountryCode { get; set; } = string.Empty;

            public List<string>? BranchLocations { get; set; }

            [SearchableField(IsFilterable = true)]
            public string WhatsAppNumber { get; set; } = string.Empty;

            [SearchableField(IsFilterable = true)]
            public string WhatsAppCountryCode { get; set; } = string.Empty;

            [SearchableField(IsFilterable = true)]
            public string Email { get; set; } = string.Empty;

            [SearchableField(IsFilterable = true)]
            public string? WebsiteUrl { get; set; }

            [SearchableField(IsFilterable = true)]
            public string? FacebookUrl { get; set; }

            [SearchableField(IsFilterable = true)]
            public string? InstagramUrl { get; set; }

            [SearchableField(IsFilterable = true)]
            public string CompanyLogo { get; set; } = string.Empty;

            [SearchableField]
            public string? StartDay { get; set; }

            [SearchableField]
            public string? EndDay { get; set; }

            [SimpleField(IsFilterable = true)]
            public string? StartHour { get; set; }

            [SimpleField(IsFilterable = true)]
            public string? EndHour { get; set; }

            [SearchableField(IsFilterable = true)]
            public string? UserDesignation { get; set; }

            [SearchableField(IsFilterable = true)]
            public string? AuthorisedContactPersonName { get; set; }

            [SearchableField(IsFilterable = true)]
            public string? UserName { get; set; }

            [SimpleField(IsFilterable = true)]
            public DateTime? CRExpiryDate { get; set; }

            [SearchableField(IsFilterable = true)]
            public string? CoverImage1 { get; set; }

            [SearchableField(IsFilterable = true)]
            public string? CoverImage2 { get; set; }

            [SimpleField(IsFilterable = true)]
            public bool? IsTherapeuticService { get; set; }

            [SearchableField(IsFilterable = true)]
            public string? TherapeuticCertificate { get; set; }

            [SearchableField(IsFilterable = true)]
            public string? LicenseNumber { get; set; }

            [SearchableField(IsFilterable = true)]
            public string CompanyType { get; set; }

            [SearchableField(IsFilterable = true)]
            public string CompanySize { get; set; }

            public List<string> NatureOfBusiness { get; set; } = new();

            [SearchableField(IsFilterable = true)]
            public string BusinessDescription { get; set; } = string.Empty;

            [SimpleField(IsFilterable = true)]
            public int? CRNumber { get; set; }

            [SimpleField(IsFilterable = true)]
            public string? CRDocument { get; set; } = string.Empty;

            [SearchableField(IsFilterable = true)]
            public string? UploadFeed { get; set; } = string.Empty;

            [SearchableField(IsFilterable = true)]
            public string? XMLFeed { get; set; } = string.Empty;

            [SimpleField(IsFilterable = true)]
            public string? CompanyVerificationStatus { get; set; }

            [SimpleField(IsFilterable = true)]
            public string? Status { get; set; }

            [SearchableField(IsFilterable = true)]
            public string Vertical { get; set; }

            [SearchableField(IsFilterable = true)]
            public string? SubVertical { get; set; }

            [SearchableField(IsFilterable = true)]
            public string? UserId { get; set; }

            [SimpleField(IsFilterable = true)]
            public bool? IsBasicProfile { get; set; }

            [SearchableField(IsFilterable = true)]
            public string Slug { get; set; }

            [SimpleField(IsFilterable = true)]
            public bool IsActive { get; set; } = true;

            [SearchableField(IsFilterable = true)]
            public string CreatedBy { get; set; } = string.Empty;

            [SimpleField(IsSortable = true, IsFilterable = true)]
            public DateTime CreatedUtc { get; set; }

            [SimpleField(IsFilterable = true)]
            public string? UpdatedBy { get; set; }

            [SimpleField(IsSortable = true, IsFilterable = true)]
            public DateTime? UpdatedUtc { get; set; }
        }

}
