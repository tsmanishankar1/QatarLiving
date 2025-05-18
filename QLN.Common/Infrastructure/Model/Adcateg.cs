using Microsoft.AspNetCore.Http;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Model
{
    public class Adcateg : BaseItem
    {

    }

    public class AdInformation
    {
        public Guid Id { get; set; }
        public string UserId { get; set; }
        public string DocType { get; set; } = ConstantValues.DocTypeAd;
        public string SubVertical { get; set; } = default!;

        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string Condition { get; set; }
        public double Price { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        public string Capacity { get; set; }
        public string Processor { get; set; }
        public string Coverage { get; set; }
        public string Ram { get; set; }
        public string Resolution { get; set; }
        public int BatteryPercentage { get; set; }
        public string? SizeType { get; set; }
        public string? Size { get; set; }
        public string Gender { get; set; }
        public IFormFile WarrantyCertificate { get; set; } = default!;
        public string PhoneNumber { get; set; }
        public string WhatsappNumber { get; set; }
        public string zone { get; set; }
        public string streetNumber { get; set; }
        public string buildingNumber { get; set; }
        public IFormFile UploadPhotos { get; set; } = default!;
        public bool Ispublished { get; set; }
        public bool? IsFeaturedItem { get; set; }
        public bool? IsFeaturedCategory { get; set; }
        public bool? IsFeaturedStore { get; set; }
    }

    public class AdResponse
    {
        public Guid Id { get; set; }
        public string DocType { get; set; }
        public string SubVertical { get; set; } = default!;
        public string Title { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string Condition { get; set; }
        public double Price { get; set; }

        public string Color { get; set; }
        public string Capacity { get; set; }
        public string Processor { get; set; }
        public string Coverage { get; set; }
        public string Ram { get; set; }
        public string Resolution { get; set; }
        public int BatteryPercentage { get; set; }
        public string? SizeType { get; set; }
        public string? Size { get; set; }
        public string Gender { get; set; }

        public string WarrantyCertificateUrl { get; set; }
        public string ImageUrl { get; set; }

        public string PhoneNumber { get; set; }
        public string WhatsappNumber { get; set; }

        public string Zone { get; set; }
        public string StreetNumber { get; set; }
        public string BuildingNumber { get; set; }

        public bool IsPublished { get; set; }
        public string CreatedBy { get; set; } // The user who created the ad
        public DateTime CreatedDate { get; set; }
    }

    public class AdSubCategory : BaseItem
    {
        public Guid CategoryId { get; set; }
    }

    public class AddSubCategoryRequest
    {
        public string Name { get; set; } = default!;
        public Guid CategoryId { get; set; }
    }

    public class AdBrand : BaseItem
    {
        public string Name { get; set; } = default!;
        public Guid SubCategoryId { get; set; }
    }

    public class AddBrandRequest
    {
        public string Name { get; set; }
        public Guid SubCategoryId { get; set; }
    }

    public class AdModel : BaseItem
    {
        public string Name { get; set; }
        public Guid BrandId { get; set; }
    }

    public class AddModelRequest
    {
        public string Name { get; set; }
        public Guid BrandId { get; set; }
    }
    public class AdCondition : BaseItem
    {
        public string Name { get; set; }
    }

    public class AddConditionRequest
    {
        public string Name { get; set; }
    }

    public class AdColor : BaseItem
    {
        public string Name { get; set; }
    }

    public class AdColorRequest
    {
        public string Name { get; set; }
    }
    public class AdCapacity : BaseItem
    {
        public string Name { get; set; }
    }

    public class AdCapacityRequest
    {
        public string Name { get; set; }
    }
    public class AdProcessor : BaseItem
    {
        public string Name { get; set; }
        public Guid ModelId { get; set; }
    }
    public class AdProcessorRequest
    {
        public string Name { get; set; }
        public Guid ModelId { get; set; }
    }
    public class AdCoverage : BaseItem
    {
        public string Name { get; set; }
    }
    public class AdCoverageRequest
    {
        public string Name { get; set; }
    }
    public class AdRam : BaseItem
    {
        public string Name { get; set; }
        public Guid ModelId { get; set; }
    }
    public class AdRamRequest
    {
        public string Name { get; set; }
        public Guid ModelId { get; set; }
    }

    public class AdResolution : BaseItem
    {
        public string Name { get; set; }
        public Guid ModelId { get; set; }
    }
    public class AdResolutionRequest
    {
        public string Name { get; set; }
        public Guid ModelId { get; set; }
    }

    public class AdSizeType : BaseItem
    {
        public string Name { get; set; }
    }
    public class AdSizeRequest
    {
        public string Name { get; set; }
    }

    public class AdGender : BaseItem
    {
        public string Name { get; set; }
    }
    public class AdGenderRequest
    {
        public string Name { get; set; }
    }

    public class AdZone : BaseItem
    {
        public string Name { get; set; }
    }

    public class AdZoneRequest
    {
        public string Name { get; set; }
    }

}
