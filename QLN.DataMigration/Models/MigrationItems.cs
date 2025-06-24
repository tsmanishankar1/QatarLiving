using System.Text.Json.Serialization;

namespace QLN.DataMigration.Models
{
    public class Item
    {
        [JsonPropertyName("uid")]
        public int Uid { get; set; }

        [JsonPropertyName("author_uid")]
        public int AuthorUid { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("published")]
        public bool Published { get; set; }

        [JsonPropertyName("path_redirects")]
        public List<string> PathRedirects { get; set; }

        [JsonPropertyName("offer_id")]
        public int? OfferId { get; set; }

        [JsonPropertyName("building_no")]
        public string BuildingNo { get; set; }

        [JsonPropertyName("category_parent_id")]
        public int CategoryParentId { get; set; }

        [JsonPropertyName("category")]
        public List<int> Category { get; set; }

        [JsonPropertyName("linked_categories")]
        public List<int> LinkedCategories { get; set; } = new();

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

        [JsonPropertyName("location_id")]
        public int? LocationId { get; set; }

        [JsonPropertyName("make")]
        public int? Make { get; set; }

        [JsonPropertyName("model")]
        public int? Model { get; set; }

        [JsonPropertyName("geo_location_lat")]
        public double? GeoLocationLat { get; set; }

        [JsonPropertyName("geo_location_long")]
        public double? GeoLocationLng { get; set; }

        [JsonPropertyName("DRUPAL-7-NID")]
        public string DRUPAL7NID { get; set; }

        [JsonPropertyName("sold")]
        public bool Sold { get; set; }

        [JsonPropertyName("street_no")]
        public string StreetNo { get; set; }

        [JsonPropertyName("zone_id")]
        public int? ZoneId { get; set; }

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

    public class MigrationItems
    {
        [JsonPropertyName("items")]
        public List<Item> Items { get; set; }

        public static explicit operator MigrationItems(DrupalItems drupalItems)
        {
            var items = new List<Item>();

            foreach (var item in drupalItems.Items)
            {
                var newItem = new Item
                {
                    Uid = item.Uid,
                    AuthorUid = item.Author.Uid,
                    Title = item.Title,
                    Type = item.Type,
                    Published = item.Published,
                    PathRedirects = item.PathRedirects,
                    OfferId = item.Offer?.Tid,
                    BuildingNo = item.BuildingNo,
                    CategoryParentId = item.CategoryParent.Tid,
                    Category = item.Category.Count > 0 ? [.. item.Category.Select(x => x.Tid)] : new(),
                    LinkedCategories = item.LinkedCategories.Count > 0 ? [.. item.LinkedCategories.Select(x => x.Tid)] : new(),
                    Price = item.Price,
                    Whatsapp = item.Whatsapp,
                    ClassifiedType = item.ClassifiedType,
                    Email = item.Email,
                    ShopUrl = item.ShopUrl,
                    Phone = item.Phone,
                    IsDeleted = item.IsDeleted,
                    BrandNew = item.BrandNew,
                    Desc = item.Desc,
                    LocationId = item.Location?.Tid,
                    Make = item.Make?.Id,
                    Model = item.Model?.Id,
                    GeoLocationLat = item.GeoLocation?.Lat,
                    GeoLocationLng = item.GeoLocation?.Lng,
                    DRUPAL7NID = item.DRUPAL7NID,
                    Sold = item.Sold,
                    StreetNo = item.StreetNo,
                    ZoneId = item.Zone?.Tid,
                    ZoneOldTid = item.ZoneOldTid,
                    Promote = item.Promote,
                    Feature = item.Feature,
                    Slug = item.Slug,
                    CreatedDate = item.CreatedDate,
                    RefreshedDate = item.RefreshedDate,
                    Images = item.Images
                };
                items.Add(newItem);
            }

            return new MigrationItems
            {
                Items = items
            };
        }
    }
}
