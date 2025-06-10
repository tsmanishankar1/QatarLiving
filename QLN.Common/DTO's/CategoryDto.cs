using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class CategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public List<CategoriesDto> SubCategories { get; set; } = new();
    }

    public class CategoriesDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
    }

    public class BrandDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
    }

    public class SubCategoryWithBrandsDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public List<BrandDto> Brands { get; set; } = new();
    }

    public class ModelDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
    }

    public class BrandWithModelsDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public List<ModelDto> Models { get; set; } = new();
    }

    public class ProcessorDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
    }

    public class ModelWithProcessorsDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public List<ProcessorDto> Processors { get; set; } = new();
    }

    public class ModelWithRamDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public List<CategoriesDto> RamOptions { get; set; } = new();
    }

    public class ModelWithResolutionsDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public List<CategoriesDto> Resolutions { get; set; } = new();
    }

    public class AddSubCategoryRequest
    {
        public Guid CategoryId { get; set; }
        public string Name { get; set; }
    }

    public class AddBrandRequest
    {
        public Guid SubCategoryId { get; set; }
        public string Name { get; set; }

    }
    public class AddModelRequest
    {
        public Guid BrandId { get; set; }
        public string Name { get; set; }
    }
    public class AddConditionRequest
    {
        public string Name { get; set; }
    }

    public class AdColorRequest
    {
        public string Name { get; set; }
    }
    public class AdCapacityRequest
    {
        public string Name { get; set; }
    }

    public class AdProcessorRequest
    {
        public Guid ModelId { get; set; }
        public string Name { get; set; }
    }
    public class AdCoverageRequest
    {
        public string Name { get; set; }
    }
    public class AdRamRequest
    {
        public Guid ModelId { get; set; }
        public string Name { get; set; }
    }
    public class AdResolutionRequest
    {
        public Guid ModelId { get; set; }
        public string Name { get; set; }
    }
    
    public class AdSizeRequest
    {
        public string Name { get; set; }
    }

    public class AdZoneRequest
    {
        public string Name { get; set; }
    }

    public class NestedCategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<NestedSubCategoryDto> SubCategories { get; set; } = new();
    }

    public class NestedSubCategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<NestedBrandDto> Brands { get; set; } = new();
    }
    public class NestedBrandDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<NestedModelDto> Models { get; set; } = new();
    }

    public class NestedModelDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<CategoriesDto> Rams { get; set; } = new();
        public List<CategoriesDto> Processors { get; set; } = new();
        public List<CategoriesDto> Resolutions { get; set; } = new();
    }
}
