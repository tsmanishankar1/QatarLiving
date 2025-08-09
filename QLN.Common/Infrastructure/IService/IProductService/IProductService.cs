using QLN.Common.DTO_s.Payments;
using QLN.Common.DTO_s.Subscription;
using QLN.Common.DTOs;
using QLN.Common.Infrastructure.Model;
using QLN.Common.Infrastructure.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IProductService
{
    public interface IProductService
    {
        Task<List<ProductResponseDto>> GetAllProductsAsync(CancellationToken cancellationToken = default);
        Task<List<ProductResponseDto>> GetProductsByVerticalAsync(Vertical vertical, CancellationToken cancellationToken = default);
        Task<List<ProductResponseDto>> GetProductsByTypeAsync(ProductType productType, CancellationToken cancellationToken = default);
        Task<ProductResponseDto?> GetProductByCodeAsync(string productCode, CancellationToken cancellationToken = default);

        Task<ProductResponseDto> CreateProductAsync(CreateProductDto dto, CancellationToken cancellationToken = default);
        Task<ProductResponseDto> UpdateProductAsync(string productCode, UpdateProductDto dto, CancellationToken cancellationToken = default);
        Task<bool> DeleteProductAsync(string productCode, CancellationToken cancellationToken = default);
    }
}
