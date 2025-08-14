using Azure.Search.Documents.Indexes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{

    public class ClassifiedStoresIndex
    {

        [SimpleField(IsFilterable = true)]
        public string? CompanyId { get; set; }
        [SimpleField(IsFilterable = true)]
        public string? SubscriptionId { get; set; }

        [SearchableField(IsFilterable = true)]
        public string? CompanyName { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? StoreSlug { get; set; }

        [SimpleField]
        public string? ContactNumber { get; set; }
        [SearchableField]
        public string? Email { get; set; }

        [SimpleField]
        public string? ImageUrl { get; set; }

        [SimpleField]
        public string? BannerUrl { get; set; }

        [SimpleField]
        public string? WebsiteUrl { get; set; }
        public List<string>? Locations { get; set; } = new List<string>();


        [SimpleField(IsKey = true)]
        public string? ProductId { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? ProductName { get; set; }

        [SearchableField(IsFilterable = true, IsFacetable = true)]
        public string? ProductSlug { get; set; }


        [SimpleField]
        public string? ProductLogo { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public double ProductPrice { get; set; } = 0;

        [SimpleField]
        public string? Currency { get; set; }

        [SearchableField(IsFacetable = true)]
        public string? ProductSummary { get; set; }

        [SearchableField]
        public string? ProductDescription { get; set; }
        public List<string>? Features { get; set; } = new List<string>();

        public List<string>? Images { get; set; } = new List<string>();

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime CreatedAt { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public DateTime UpdatedAt { get; set; }

        [SimpleField(IsFilterable = true, IsSortable = true)]
        public bool IsActive { get; set; }
    }

    public class StoreIndexDto
    {     
        public string? CompanyId { get; set; }
        public string? SubscriptionId { get; set; }
        public string? CompanyName { get; set; }     
        public string? ImageUrl { get; set; }    
        public string? BannerUrl { get; set; }
        public string? ContactNumber { get; set; }
        public string? Email { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? BranchLocations { get; set; }
        public List<string>? Locations { get; set; } = new List<string>();
    }
}
