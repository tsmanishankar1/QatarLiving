using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper.Configuration.Attributes;
namespace QLN.Common.DTO_s.ClassifiedsBo
{
    public class StoresCSVDto
    {
    }

    public class ShopifyProduct
    {
        public string? Handle { get; set; }
        public string? Title { get; set; }

        [Name("Body (HTML)")]
        public string? BodyHtml { get; set; }

        public string? Vendor { get; set; }

        [Name("Product Category")]
        public string? ProductCategory { get; set; }

        public string? Type { get; set; }
        public string? Tags { get; set; }
        public bool? Published { get; set; } = false;

        [Name("Option1 Name")]
        public string? Option1Name { get; set; }

        [Name("Option1 Value")]
        public string? Option1Value { get; set; }

        [Name("Option2 Name")]
        public string? Option2Name { get; set; }

        [Name("Option2 Value")]
        public string? Option2Value { get; set; }

        [Name("Option3 Name")]
        public string? Option3Name { get; set; }

        [Name("Option3 Value")]
        public string? Option3Value { get; set; }

        [Name("Variant SKU")]
        public string? VariantSKU { get; set; }

        [Name("Variant Grams")]
        public int? VariantGrams { get; set; }

        [Name("Variant Inventory Tracker")]
        public string? VariantInventoryTracker { get; set; }

        [Name("Variant Inventory Qty")]
        public int? VariantInventoryQty { get; set; }

        [Name("Variant Inventory Policy")]
        public string? VariantInventoryPolicy { get; set; }

        [Name("Variant Fulfillment Service")]
        public string? VariantFulfillmentService { get; set; }

        [Name("Variant Price")]
        public decimal? VariantPrice { get; set; }

        [Name("Variant Compare At Price")]
        public decimal? VariantCompareAtPrice { get; set; }

        [Name("Variant Requires Shipping")]
        public bool? VariantRequiresShipping { get; set; }

        [Name("Variant Taxable")]
        public bool? VariantTaxable { get; set; }

        [Name("Variant Barcode")]
        public string? VariantBarcode { get; set; }

        [Name("Image Src")]
        public string? ImageSrc { get; set; }

        [Name("Image Position")]
        public int? ImagePosition { get; set; }

        [Name("Image Alt Text")]
        public string? ImageAltText { get; set; }

        [Name("Gift Card")]
        public bool? GiftCard { get; set; }

        [Name("SEO Title")]
        public string? SEOTitle { get; set; }

        [Name("SEO Description")]
        public string? SEODescription { get; set; }

        [Name("Google Shopping / Google Product Category")]
        public string? GoogleProductCategory { get; set; }

        [Name("Google Shopping / Gender")]
        public string? GoogleGender { get; set; }

        [Name("Google Shopping / Age Group")]
        public string? GoogleAgeGroup { get; set; }

        [Name("Google Shopping / MPN")]
        public string? GoogleMPN { get; set; }

        [Name("Google Shopping / Condition")]
        public string? GoogleCondition { get; set; }

        [Name("Google Shopping / Custom Product")]
        public bool? GoogleCustomProduct { get; set; }

        [Name("Variant Image")]
        public string? VariantImage { get; set; }

        [Name("Variant Weight Unit")]
        public string? VariantWeightUnit { get; set; }

        [Name("Variant Tax Code")]
        public string? VariantTaxCode { get; set; }

        [Name("Cost per item")]
        public decimal? CostPerItem { get; set; }

        [Name("Included / United States")]
        public bool? IncludedUnitedStates { get; set; }

        [Name("Price / United States")]
        public decimal? PriceUnitedStates { get; set; }

        [Name("Compare At Price / United States")]
        public decimal? CompareAtPriceUnitedStates { get; set; }

        [Name("Included / International")]
        public bool? IncludedInternational { get; set; }

        [Name("Price / International")]
        public decimal? PriceInternational { get; set; }

        [Name("Compare At Price / International")]
        public decimal? CompareAtPriceInternational { get; set; }

        public string? Status { get; set; }
    }
}
