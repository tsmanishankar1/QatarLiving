using System.Text.Json.Serialization;

namespace QLN.DataMigration.Models
{
    public class DrupalAuthor
    {
        [JsonPropertyName("uid")]
        public int Uid { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("slug")]
        public string Slug { get; set; }
    }

    public class DrupalCategory
    {
        [JsonPropertyName("tid")]
        public int Tid { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("arabic")]
        public string Arabic { get; set; }
    }

    public class DrupalCategoryParent
    {
        [JsonPropertyName("tid")]
        public int Tid { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("arabic")]
        public string Arabic { get; set; }
    }

    public class DrupalGeoLocation
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lng")]
        public double Lng { get; set; }
    }

    public class DrupalItem
    {
        [JsonPropertyName("uid")]
        public int Uid { get; set; }

        [JsonPropertyName("author")]
        public DrupalAuthor Author { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("published")]
        public bool Published { get; set; }

        [JsonPropertyName("path_redirects")]
        public List<string> PathRedirects { get; set; }

        [JsonPropertyName("offer")]
        public DrupalOffer? Offer { get; set; }

        [JsonPropertyName("building_no")]
        public string BuildingNo { get; set; }

        [JsonPropertyName("category_parent")]
        public DrupalCategoryParent CategoryParent { get; set; }

        [JsonPropertyName("category")]
        public List<DrupalCategory> Category { get; set; }

        [JsonPropertyName("linked_categories")]
        public List<DrupalCategory> LinkedCategories { get; set; } = new();

        [JsonPropertyName("price")]
        public double Price { get; set; }

        [JsonPropertyName("whatsapp")]
        public string Whatsapp { get; set; }

        [JsonPropertyName("classified_type")]
        public string ClassifiedType { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("shop_url")]
        public string ShopUrl { get; set; }

        [JsonPropertyName("phone")]
        public string Phone { get; set; }

        [JsonPropertyName("is_deleted")]
        public bool IsDeleted { get; set; }

        [JsonPropertyName("brand_new")]
        public bool BrandNew { get; set; }

        [JsonPropertyName("desc")]
        public string Desc { get; set; }

        [JsonPropertyName("location")]
        public DrupalLocation? Location { get; set; }

        [JsonPropertyName("make")]
        public DrupalMake? Make { get; set; }

        [JsonPropertyName("model")]
        public DrupalModel? Model { get; set; }

        [JsonPropertyName("geo_location")]
        public DrupalGeoLocation? GeoLocation { get; set; }

        [JsonPropertyName("DRUPAL-7-NID")]
        public string DRUPAL7NID { get; set; }

        [JsonPropertyName("sold")]
        public bool Sold { get; set; }

        [JsonPropertyName("street_no")]
        public string StreetNo { get; set; }

        [JsonPropertyName("zone")]
        public DrupalZone? Zone { get; set; }

        [JsonPropertyName("zone_old_tid")]
        public string ZoneOldTid { get; set; }

        [JsonPropertyName("promote")]
        public bool Promote { get; set; }

        [JsonPropertyName("feature")]
        public bool Feature { get; set; }

        [JsonPropertyName("slug")]
        public string Slug { get; set; }

        [JsonPropertyName("created_date")]
        public string CreatedDate { get; set; }

        [JsonPropertyName("refreshed_date")]
        public string RefreshedDate { get; set; }

        [JsonPropertyName("images")]
        public List<string> Images { get; set; }
    }

    public class DrupalLocation
    {
        [JsonPropertyName("tid")]
        public int Tid { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("arabic")]
        public string Arabic { get; set; }
    }

    public class DrupalOffer
    {
        [JsonPropertyName("tid")]
        public int Tid { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("arabic")]
        public string Arabic { get; set; }
    }

    public class DrupalItems
    {
        [JsonPropertyName("items")]
        public List<DrupalItem> Items { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }
    }

    public class DrupalZone
    {
        [JsonPropertyName("tid")]
        public int Tid { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("arabic")]
        public string Arabic { get; set; }
    }


}
