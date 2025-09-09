using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Payments;
using QLN.Common.DTO_s.Subscription;
using QLN.Common.DTOs;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.IService.IProductService;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.QLDbContext;
using QLN.Common.Infrastructure.Subscriptions;
using System.Text.Json;
using L1Category = QLN.Common.DTO_s.Subscription.L1Category;

namespace QLN.Subscriptions.Actor.Service
{
    public class InternalProductService : IProductService
    {
        private readonly QLSubscriptionContext _context;
        private readonly ILogger<InternalProductService> _logger;

        public InternalProductService(QLSubscriptionContext context, ILogger<InternalProductService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<ProductResponseDto>> GetAllProductsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var products = await _context.Products
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Vertical)
                    .ThenBy(p => p.ProductType)
                    .ThenBy(p => p.ProductName)
                    .ToListAsync(cancellationToken);

                return products.Select(MapToResponseDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all products");
                throw;
            }
        }

        public async Task<List<ProductResponseDto>> GetProductsByVerticalAsync(Vertical? vertical, SubVertical? subvertical, ProductType? productType, CancellationToken cancellationToken = default)
        {
            try
            {
                var products = await _context.Products
                .Where(p => p.IsActive)
                .Where(p => vertical == null || p.Vertical == vertical)
                .Where(p => subvertical == null || p.SubVertical == subvertical)
                .Where(p => productType == null || p.ProductType == productType)
                .OrderBy(p => p.ProductType)
                .ThenBy(p => p.Price)
                .ToListAsync(cancellationToken);

                return products.Select(MapToResponseDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products for vertical {Vertical}", vertical);
                throw;
            }
        }

        public async Task<List<ProductResponseDto>> GetProductsByTypeAsync(ProductType productType, CancellationToken cancellationToken = default)
        {
            try
            {
                var products = await _context.Products
                    .Where(p => p.IsActive && p.ProductType == productType)
                    .OrderBy(p => p.Vertical)
                    .ThenBy(p => p.Price)
                    .ToListAsync(cancellationToken);

                return products.Select(MapToResponseDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products for type {ProductType}", productType);
                throw;
            }
        }

        public async Task<ProductResponseDto?> GetProductByCodeAsync(string productCode, CancellationToken cancellationToken = default)
        {
            try
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductCode == productCode && p.IsActive, cancellationToken);

                return product != null ? MapToResponseDto(product) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product with code {ProductCode}", productCode);
                throw;
            }
        }

        public async Task<ProductResponseDto> CreateProductAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if product code already exists
                var existingProduct = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductCode == dto.ProductCode, cancellationToken);

                if (existingProduct != null)
                    throw new ConflictException($"Product with code '{dto.ProductCode}' already exists.");

                var product = new Product
                {
                    ProductCode = dto.ProductCode,
                    ProductName = dto.ProductName,
                    ProductType = dto.ProductType,
                    Vertical = dto.Vertical,
                    SubVertical = dto.SubVertical,
                    Price = dto.Price,
                    Currency = dto.Currency,
                    Constraints = dto.Constraints ?? new ProductConstraints(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Created product with code: {ProductCode}", product.ProductCode);
                return MapToResponseDto(product);
            }
            catch (ConflictException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product with code {ProductCode}", dto.ProductCode);
                throw;
            }
        }

        public async Task<ProductResponseDto> UpdateProductAsync(string productCode, UpdateProductDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductCode == productCode && p.IsActive, cancellationToken);

                if (product == null)
                    throw new KeyNotFoundException($"Product with code '{productCode}' not found.");

                // Apply updates
                if (!string.IsNullOrEmpty(dto.ProductName))
                    product.ProductName = dto.ProductName;

                if (dto.Price.HasValue)
                    product.Price = dto.Price.Value;

                if (!string.IsNullOrEmpty(dto.Currency))
                    product.Currency = dto.Currency;

                if (dto.Constraints != null)
                    product.Constraints = dto.Constraints;

                if (dto.IsActive.HasValue)
                    product.IsActive = dto.IsActive.Value;

                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Updated product with code: {ProductCode}", productCode);
                return MapToResponseDto(product);
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product with code {ProductCode}", productCode);
                throw;
            }
        }

        public async Task<bool> DeleteProductAsync(string productCode, CancellationToken cancellationToken = default)
        {
            try
            {
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductCode == productCode && p.IsActive, cancellationToken);

                if (product == null)
                    return false;

                // Soft delete
                product.IsActive = false;
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Soft deleted product with code: {ProductCode}", productCode);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product with code {ProductCode}", productCode);
                throw;
            }
        }

        // Helper method to map Product to ProductResponseDto
        private static ProductResponseDto MapToResponseDto(Product product)
        {
            return new ProductResponseDto
            {
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                ProductType = product.ProductType,
                Vertical = product.Vertical,
                SubVertical = product.SubVertical,
                Price = product.Price,
                Currency = product.Currency,
                Constraints = product.Constraints,
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            };
        }
        public async Task<ProductResponseDto> CreateFreeAdsProductAsync(CreateFreeAdsProductDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if product code already exists
                var existingProduct = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductCode == dto.ProductCode, cancellationToken);

                if (existingProduct != null)
                    throw new ConflictException($"Product with code '{dto.ProductCode}' already exists.");

                // Parse category hierarchy from JSON
                var freeAdsQuotas = BuildFreeAdsQuotasFromJson(dto.CategoryHierarchyJson);

                // Create constraints for FREE product type
                var constraints = new ProductConstraints
                {
                    AdsBudget = freeAdsQuotas.Sum(q => q.FreeAdsAllowed), // Total free ads across all categories
                    FeaturedBudget = 0, // No features for FREE
                    PromotedBudget = 0, // No promotions for FREE
                    RefreshBudgetPerDay = 0, // No refreshes for FREE
                    Duration = dto.Duration,
                    Scope = "Category-Based-Free",
                    IsAddOn = false,
                    PayToPublish = false, // Free to publish
                    PayToPromote = true, // Must pay to promote
                    PayToFeature = true, // Must pay to feature
                    Remarks = dto.Remarks,
                    CategoryQuotas = freeAdsQuotas.Select(q => new CategoryQuota
                    {
                        Category = q.Category,
                        L1Category = q.L1Category,
                        L2Category = q.L2Category,
                        AdsBudget = q.FreeAdsAllowed,
                        FeaturedBudget = 0,
                        PromotedBudget = 0,
                        RefreshBudget = 0
                    }).ToList()
                };

                var product = new Product
                {
                    ProductCode = dto.ProductCode,
                    ProductName = dto.ProductName,
                    ProductType = ProductType.FREE,
                    Vertical = dto.Vertical,
                    SubVertical = dto.SubVertical,
                    Price = 0, // Always free
                    Currency = dto.Currency,
                    Constraints = constraints,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Created FREE ads product with code: {ProductCode} with {CategoryCount} category quotas",
                    product.ProductCode, freeAdsQuotas.Count);

                return MapToResponseDto(product);
            }
            catch (ConflictException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating FREE ads product with code {ProductCode}", dto.ProductCode);
                throw;
            }
        }

        // Build FREE ads subscription quota from product
        public FreeAdsSubscriptionQuota BuildFreeAdsSubscriptionQuotaFromProduct(Product product)
        {
            if (product.ProductType != ProductType.FREE)
                throw new InvalidOperationException("Product must be of type FREE to build free ads quota");

            var constraints = product.Constraints ?? new ProductConstraints();
            var quota = new FreeAdsSubscriptionQuota
            {
                Vertical = product.Vertical.ToString(),
                Scope = constraints.Scope ?? "Category-Based-Free"
            };

            // Add category-specific free ads quotas
            foreach (var categoryQuota in constraints.CategoryQuotas ?? new List<CategoryQuota>())
            {
                quota.CategoryQuotas.Add(new FreeAdsCategoryUsage
                {
                    Category = categoryQuota.Category,
                    L1Category = categoryQuota.L1Category,
                    L2Category = categoryQuota.L2Category,
                    FreeAdsAllowed = categoryQuota.AdsBudget // For FREE products, AdsBudget represents free ads allowed
                });
            }

            return quota;
        }

        private List<FreeAdsCategoryQuota> BuildFreeAdsQuotasFromJson(string? jsonHierarchy)
        {
            var freeAdsQuotas = new List<FreeAdsCategoryQuota>();

            if (string.IsNullOrEmpty(jsonHierarchy))
                return freeAdsQuotas;

            try
            {
                _logger.LogInformation("Parsing JSON hierarchy: {JsonLength} characters", jsonHierarchy.Length);

                // FIX: Use JsonSerializerOptions for proper deserialization
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var hierarchyData = JsonSerializer.Deserialize<List<CategoryHierarchy>>(jsonHierarchy, options);

                if (hierarchyData == null || !hierarchyData.Any())
                {
                    _logger.LogWarning("Deserialized hierarchy data is null or empty");
                    return freeAdsQuotas;
                }

                _logger.LogInformation("Successfully parsed {CategoryCount} main categories", hierarchyData.Count);

                foreach (var category in hierarchyData)
                {
                    _logger.LogInformation("Processing category: {CategoryName} with {L1Count} L1 categories",
                        category.Category, category.L1?.Count ?? 0);

                    foreach (var l1 in category.L1 ?? new List<L1Category>())
                    {
                        if (l1.L2?.Any() == true)
                        {
                            // Has L2 categories - create free ads quotas for each L2
                            foreach (var l2 in l1.L2)
                            {
                                var quota = new FreeAdsCategoryQuota
                                {
                                    Category = category.Category,
                                    L1Category = l1.L1CategoryName,
                                    L2Category = l2.L2CategoryName,
                                    FreeAdsAllowed = l2.AdsBudget
                                };

                                freeAdsQuotas.Add(quota);

                                _logger.LogInformation("Added L2 quota: {Category} > {L1} > {L2} = {Budget} ads",
                                    quota.Category, quota.L1Category, quota.L2Category, quota.FreeAdsAllowed);
                            }
                        }
                        else
                        {
                            // Only L1 category - create free ads quota for L1
                            var quota = new FreeAdsCategoryQuota
                            {
                                Category = category.Category,
                                L1Category = l1.L1CategoryName,
                                L2Category = null,
                                FreeAdsAllowed = l1.L1Cap
                            };

                            freeAdsQuotas.Add(quota);

                            _logger.LogInformation("Added L1 quota: {Category} > {L1} = {Budget} ads",
                                quota.Category, quota.L1Category, quota.FreeAdsAllowed);
                        }
                    }
                }

                _logger.LogInformation("Built {Count} free ads category quotas from JSON hierarchy. Total ads: {TotalAds}",
                    freeAdsQuotas.Count, freeAdsQuotas.Sum(q => q.FreeAdsAllowed));
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing category hierarchy JSON. JSON content: {JsonContent}",
                    jsonHierarchy?.Substring(0, Math.Min(500, jsonHierarchy.Length)));
                throw new InvalidOperationException($"Invalid category hierarchy JSON format: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error parsing category hierarchy JSON");
                throw new InvalidOperationException($"Failed to parse category hierarchy: {ex.Message}", ex);
            }

            return freeAdsQuotas;
        }
    }
}
