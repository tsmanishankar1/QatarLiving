using System;
using System.Collections.Generic;

namespace QLN.Common.Infrastructure.Model
{
    public class CollectiblesResponse
    {
        public Guid UserId { get; set; }

        public DashboardSummary DashboardSummary { get; set; }

        public List<CollectibleItem> PublishedAds { get; set; }

        public List<CollectibleItem> UnpublishedAds { get; set; }

        public Pagination Pagination { get; set; }
    }

    public class DashboardSummary
    {
        public int TotalListings { get; set; }
        public int ActiveAds { get; set; }
        public int FeaturedAds { get; set; }
        public int Watchlist { get; set; }
        public int RecentViews { get; set; }
        public int Favorites { get; set; }
        public int Offers { get; set; }
        public int Messages { get; set; }
    }

    public class CollectibleItem
    {
        public Guid UserId { get; set; } // Added to associate each item with a user

        public int Id { get; set; }
        public string Title { get; set; }
        public string ProductDescription { get; set; }
        public double Price { get; set; }
        public string Category { get; set; }

        public int? TradingCardCategory { get; set; }
        public int? WatchCategory { get; set; }
        public int? WatchType { get; set; }
        public int? FigurineCategory { get; set; }
        public int? Theme { get; set; }

        public string Condition { get; set; }
        public bool HasAuthenticityCertificate { get; set; }
        public string AuthenticityDetails { get; set; }
        public string YearEra { get; set; }
        public string Brand { get; set; }

        public int CountryOfOrigin { get; set; }
        public int Language { get; set; }

        public bool IsGraded { get; set; }
        public int? GradingCompany { get; set; }
        public string Grade { get; set; }

        public string Rarity { get; set; }
        public int? Package { get; set; }
        public string Material { get; set; }
        public string SerialNumber { get; set; }

        public bool IsSigned { get; set; }
        public string SignedBy { get; set; }
        public bool IsFramed { get; set; }

        public List<string> ImageUrls { get; set; }

        public string ContactNumber { get; set; }
        public string EmailAddress { get; set; }
        public string WhatsappNumber { get; set; }

        public string StreetNo { get; set; }
        public string BuildingNo { get; set; }
        public int Location { get; set; }

        public bool HasWarranty { get; set; }
        public bool AcceptsTermsAndConditions { get; set; }
        public bool IsHandmade { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string Status { get; set; } // e.g., Published, Draft, InReview
        public int Views { get; set; }
        public int Watchers { get; set; }
        public int Offers { get; set; }
    }

    public class Pagination
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int ItemsPerPage { get; set; }
        public bool HasNext { get; set; }
        public bool HasPrevious { get; set; }
    }
}
