using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.DTO_s.Payments;
using QLN.Common.DTO_s.Subscription;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.IService.IProductService;
using QLN.Common.Infrastructure.Subscriptions;
using System.Net;
using System.Text;

namespace QLN.Backend.API.Service.ProductService
{
    public class ExternalProductService : IProductService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<ExternalProductService> _logger;
        private const string ServiceAppId = ConstantValues.ServiceAppIds.SubscriptionApp;

        public ExternalProductService(DaprClient dapr, ILogger<ExternalProductService> logger)
        {
            _dapr = dapr;
            _logger = logger;
        }

        public async Task<List<ProductResponseDto>> GetAllProductsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dapr.InvokeMethodAsync<List<ProductResponseDto>>(
                    HttpMethod.Get,
                    ServiceAppId,
                    "api/products/getall",
                    cancellationToken) ?? new();
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
                var queryParams = new List<string>();
                if (vertical.HasValue) queryParams.Add($"vertical={(int)vertical.Value}");
                if (subvertical.HasValue) queryParams.Add($"subvertical={(int)subvertical.Value}");
                if (productType.HasValue) queryParams.Add($"productType={(int)productType.Value}");

                var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";

                return await _dapr.InvokeMethodAsync<List<ProductResponseDto>>(
                    HttpMethod.Get,
                    ServiceAppId,
                    $"api/products/getallproducts{queryString}",
                    cancellationToken) ?? new();
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
                return await _dapr.InvokeMethodAsync<List<ProductResponseDto>>(
                    HttpMethod.Get,
                    ServiceAppId,
                    $"api/products/type/{(int)productType}",
                    cancellationToken) ?? new();
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
                return await _dapr.InvokeMethodAsync<ProductResponseDto>(
                    HttpMethod.Get,
                    ServiceAppId,
                    $"api/products/{productCode}",
                    cancellationToken);
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Product with code {ProductCode} not found", productCode);
                return null;
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
                return await _dapr.InvokeMethodAsync<CreateProductDto, ProductResponseDto>(
                    HttpMethod.Post,
                    ServiceAppId,
                    "api/products/create",
                    dto,
                    cancellationToken) ?? throw new Exception("Invalid response from product service");
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == HttpStatusCode.Conflict)
            {
                var errorMessage = await ex.Response.Content.ReadAsStringAsync(cancellationToken);
                throw new ConflictException(errorMessage);
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorMessage = await ex.Response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidDataException(errorMessage);
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
                return await _dapr.InvokeMethodAsync<UpdateProductDto, ProductResponseDto>(
                    HttpMethod.Put,
                    ServiceAppId,
                    $"api/products/{productCode}",
                    dto,
                    cancellationToken) ?? throw new Exception("Invalid response from product service");
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == HttpStatusCode.NotFound)
            {
                var errorMessage = await ex.Response.Content.ReadAsStringAsync(cancellationToken);
                throw new KeyNotFoundException(errorMessage);
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorMessage = await ex.Response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidDataException(errorMessage);
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
                var result = await _dapr.InvokeMethodAsync<string>(
                    HttpMethod.Delete,
                    ServiceAppId,
                    $"api/products/{productCode}",
                    cancellationToken);

                return result == "Product deleted successfully";
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Product with code {ProductCode} not found for deletion", productCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product with code {ProductCode}", productCode);
                throw;
            }
        }
    }
}
