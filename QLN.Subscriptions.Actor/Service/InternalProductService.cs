using Microsoft.EntityFrameworkCore;
using QLN.Common.DTO_s.Payments;
using QLN.Common.DTO_s.Subscription;
using QLN.Common.DTOs;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.IService.IProductService;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.QLDbContext;
using QLN.Common.Infrastructure.Subscriptions;
using static QLN.Common.DTO_s.Enums.Enum;

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

        public async Task<List<ProductResponseDto>> GetProductsByVerticalAsync(Vertical vertical, CancellationToken cancellationToken = default)
        {
            try
            {
                var products = await _context.Products
                    .Where(p => p.IsActive && p.Vertical == vertical)
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
    }
}
