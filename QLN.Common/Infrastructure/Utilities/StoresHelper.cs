using QLN.Common.DTO_s.ClassifiedsBo;
using QLN.Common.Migrations.QLClassified;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Utilities
{
    
    public static class StoresMapper
    {
        public static StoreProducts MapShopifyToStore(ShopifyProduct sp, Guid flyerId)
        {
            var productId = Guid.NewGuid();
            string SlugId = productId.ToString().Substring(0, 8);
            return new StoreProducts
            {
                StoreProductId = productId,
                
                ProductName = sp.Title ?? "Unnamed Product",
                ProductLogo = sp.ImageSrc ?? sp.VariantImage ?? string.Empty,
                ProductPrice = sp.VariantPrice ?? 0,
                Currency = "QAR",
                ProductSummary = sp.Tags ?? sp.Title ?? string.Empty,
                ProductDescription = sp.BodyHtml ?? sp.SEODescription ?? string.Empty,
                PageNumber = 1,
                PageCoordinates = null, 
                Slug = "Stores-" + sp.Title ?? "Unnamed Product" + "-" + SlugId, 
                Category = sp.ProductCategory ?? "Others",
                Qty = sp.VariantInventoryQty ?? 0,
                ProductBarcode = sp.VariantBarcode,
                FlyerId = flyerId,
                Features = new List<ProductFeatures>(), 
                Images = new List<ProductImages> 
                {
                    new ProductImages
                    {
                         Images = sp.ImageSrc ?? sp.VariantImage ?? "",
                         StoreProductId = productId
                    }
                }
            };
        }
        public static string GetFileNameFromUrl(string url)
        {
            var uri = new Uri(url);
            return Path.GetFileName(uri.LocalPath);
        }

    }




}
