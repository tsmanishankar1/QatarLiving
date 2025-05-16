using Microsoft.AspNetCore.Http;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Model
{
    public class AdInformation
    {
        public Guid Id { get; set; }
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
        public string BatteryPercentage { get; set; }
        public string? SizeType { get; set; }
        public string? Size { get; set; }
        public string Gender { get; set; }
        public IFormFile WarrantyCertificate { get; set; }
        public string PhoneNumber { get; set; }
        public string WhatsappNumber { get; set; }
        public string zone { get; set; }
        public string streetNumber { get; set; }
        public string buildingNumber { get; set; }
        public IFormFile UploadPhotos { get; set; }
        public bool Ispublished { get; set; }
    }

    public class AdResponse
    {
        public Guid Id { get; set; }
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
        public string BatteryPercentage { get; set; }
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
        public DateTime CreatedAt { get; set; }
    }

    public class AdCategory
    {
        public Guid Id { get; set; }
        public string CatogoryName { get; set; }
    }

    public class AdSubCategory
    {
        public Guid Id { get; set; }
        public string SubCategoryName { get; set; }
        public Guid CategoryId { get; set; }
    }

    public class AdBrand
    {
        public Guid Id { get; set; }
        public Guid SubCategoryId { get; set; } 
        public string BrandName { get; set; } = string.Empty;
    }

    public class AdModel
    {
        public Guid Id { get; set; }
        public Guid BrandId { get; set; } 
        public string ModelName { get; set; } = string.Empty;
    }

    public class AdCondition
    {
        public Guid Id { get; set; }
        public string ConditionName { get; set; } = string.Empty;
    }
    public class AdColor
    {
        public Guid Id { get; set; }
        public string ColorName { get; set; } = string.Empty;
    }

    public class AdCapacity
    {
        public Guid Id { get; set; }
        public string CapacityValue { get; set; } = string.Empty;
    }

    public class AdProcessor
    {
        public Guid Id { get; set; }
        public Guid ModelId { get; set; } 
        public string ProcessorName { get; set; } = string.Empty;
    }

    public class AdCoverage
    {
        public Guid Id { get; set; }
        public string CoverageName { get; set; } = string.Empty;
    }

    public class AdRam
    {
        public Guid Id { get; set; }
        public Guid ModelId { get; set; }
        public string RamSize { get; set; } = string.Empty; 
    }

    public class AdResolution
    {
        public Guid Id { get; set; }
        public Guid ModelId { get; set; }
        public string ResolutionValue { get; set; } = string.Empty; 
    }

    public class AdSizeType
    {
        public Guid Id { get; set; }
        public string SizeName { get; set; } = string.Empty;
    }
    public class AdGender
    {
        public Guid Id { get; set; }
        public string GenderName { get; set; } = string.Empty;
    }
    public class AdZone
    {
        public Guid Id { get; set; }
        public string ZoneNumber { get; set; } = string.Empty;
    }
}
