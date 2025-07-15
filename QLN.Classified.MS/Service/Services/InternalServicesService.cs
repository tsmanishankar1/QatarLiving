using Dapr.Client;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IService;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace QLN.Classified.MS.Service.Services
{
    public class InternalServicesService : IServices
    {
        public readonly DaprClient _dapr;
        public InternalServicesService(DaprClient dapr)
        {
            _dapr = dapr;
        }
        public async Task<string> CreateCategory(ServicesCategory dto, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dto.MainCategory))
                throw new InvalidDataException("MainCategory is required.");

            if (dto.L1Categories == null || dto.L1Categories.Count == 0)
                throw new InvalidDataException("At least one L1 Category is required.");

            dto.Id = Guid.NewGuid(); 

            foreach (var l1 in dto.L1Categories)
            {
                l1.Id = Guid.NewGuid();

                if (string.IsNullOrWhiteSpace(l1.Name))
                    throw new InvalidDataException("Each L1 Category must have a Name.");

                if (l1.L2Categories == null || l1.L2Categories.Count == 0)
                    throw new InvalidDataException($"L1 Category '{l1.Name}' must have at least one L2 Category.");

                foreach (var l2 in l1.L2Categories)
                {
                    l2.Id = Guid.NewGuid();

                    if (string.IsNullOrWhiteSpace(l2.Name))
                        throw new InvalidDataException("Each L2 Category must have a Name.");
                }
            }

            var key = dto.Id.ToString();

            await _dapr.SaveStateAsync(ConstantValues.Services.StoreName, key, dto, cancellationToken: cancellationToken);

            var keys = await _dapr.GetStateAsync<List<string>>(ConstantValues.Services.StoreName, ConstantValues.Services.IndexKey, cancellationToken : cancellationToken) ?? new();

            if (!keys.Contains(key))
            {
                keys.Add(key);
                await _dapr.SaveStateAsync(ConstantValues.Services.StoreName, ConstantValues.Services.IndexKey, keys, cancellationToken : cancellationToken);
            }

            return "Category Created Successfully";
        }
        public async Task<string> UpdateCategory(ServicesCategory dto, CancellationToken cancellationToken = default)
        {
            var key = dto.Id?.ToString();
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidDataException("Invalid category ID.");

            var existing = await _dapr.GetStateAsync<ServicesCategory>(
                ConstantValues.Services.StoreName,
                key,
                cancellationToken: cancellationToken
            );

            if (existing == null)
                throw new InvalidDataException("Category not found for update.");

            foreach (var l1 in dto.L1Categories)
            {
                if (l1.Id == Guid.Empty)
                    l1.Id = Guid.NewGuid();

                foreach (var l2 in l1.L2Categories)
                {
                    if (l2.Id == Guid.Empty)
                        l2.Id = Guid.NewGuid();
                }
            }

            await _dapr.SaveStateAsync(ConstantValues.Services.StoreName, key, dto, cancellationToken : cancellationToken);

            return "Category updated successfully.";
        }
        public async Task<List<ServicesCategory>> GetAllCategories(CancellationToken cancellationToken = default)
        {
            var keys = await _dapr.GetStateAsync<List<string>>(
                ConstantValues.Services.StoreName,
                ConstantValues.Services.IndexKey,
                cancellationToken: cancellationToken
            ) ?? new();

            if (keys == null || keys.Count == 0)
                return new List<ServicesCategory>();

            var bulkItems = await _dapr.GetBulkStateAsync(
                ConstantValues.Services.StoreName,
                keys,
                parallelism: null,
                cancellationToken: cancellationToken
            );

            var result = bulkItems
                .Where(item => !string.IsNullOrWhiteSpace(item.Value))
                .Select(item =>
                {
                    try
                    {
                        return JsonSerializer.Deserialize<ServicesCategory>(
                            item.Value,
                            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                        );
                    }
                    catch
                    {
                        return null;
                    }
                })
                .Where(x => x != null)
                .ToList();

            return result!;
        }
        public async Task<ServicesCategory?> GetCategoryById(Guid id, CancellationToken cancellationToken = default)
        {
            var key = id.ToString();

            var category = await _dapr.GetStateAsync<ServicesCategory>(
                ConstantValues.Services.StoreName,
                key,
                cancellationToken: cancellationToken
            );

            return category;
        }

        public async Task<string> CreateServiceAd(ServicesDto dto, CancellationToken cancellationToken = default)
        {
            dto.Id = Guid.NewGuid();
            dto.CreatedAt = DateTime.UtcNow;

            var key = dto.Id.ToString();

            await _dapr.SaveStateAsync(ConstantValues.Services.StoreName, key, dto, cancellationToken : cancellationToken);

            var keys = await _dapr.GetStateAsync<List<string>>(ConstantValues.Services.StoreName, ConstantValues.Services.ServicesIndexKey, cancellationToken : cancellationToken) ?? new();
            if (!keys.Contains(key))
            {
                keys.Add(key);
                await _dapr.SaveStateAsync(ConstantValues.Services.StoreName, ConstantValues.Services.ServicesIndexKey, keys, cancellationToken : cancellationToken);
            }

            return "Service Ad Created Successfully";
        }

        public async Task<string> UpdateServiceAd(ServicesDto dto, CancellationToken cancellationToken = default)
        {
            var key = dto.Id.ToString();

            var existing = await _dapr.GetStateAsync<ServicesDto>(ConstantValues.Services.StoreName, key, cancellationToken : cancellationToken);
            if (existing == null)
                throw new InvalidDataException("Ad not found.");

            dto.UpdatedAt = DateTime.UtcNow;

            await _dapr.SaveStateAsync(ConstantValues.Services.StoreName, key, dto, cancellationToken : cancellationToken);

            return "Service Ad Updated Successfully";
        }
        public async Task<List<ServicesDto>> GetAllServiceAds(CancellationToken cancellationToken = default)
        {
            var keys = await _dapr.GetStateAsync<List<string>>(
                ConstantValues.Services.StoreName,
                ConstantValues.Services.ServicesIndexKey,
                cancellationToken: cancellationToken
            ) ?? new();

            if (keys.Count == 0)
                return new List<ServicesDto>();

            var items = await _dapr.GetBulkStateAsync(
                ConstantValues.Services.StoreName,
                keys,
                parallelism: null,
                cancellationToken: cancellationToken
            );

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            return items
                .Where(i => !string.IsNullOrWhiteSpace(i.Value))
                .Select(i =>
                {
                    try
                    {
                        return JsonSerializer.Deserialize<ServicesDto>(i.Value, options);
                    }
                    catch
                    {
                        return null;
                    }
                })
                .Where(x => x != null && x.Id != Guid.Empty && !string.IsNullOrWhiteSpace(x.Title))!
                .ToList();
        }

        public async Task<ServicesDto?> GetServiceAdById(Guid id, CancellationToken cancellationToken = default)
        {
            var key = id.ToString();
            return await _dapr.GetStateAsync<ServicesDto>(ConstantValues.Services.StoreName, key, cancellationToken : cancellationToken);
        }

        public async Task<string> DeleteServiceAdById(Guid id, CancellationToken cancellationToken = default)
        {
            var key = id.ToString();

            var existing = await _dapr.GetStateAsync<ServicesDto>(ConstantValues.Services.StoreName, key, cancellationToken : cancellationToken);
            if (existing == null)
                throw new InvalidDataException("Service Ad not found.");

            await _dapr.DeleteStateAsync(ConstantValues.Services.StoreName, key, cancellationToken : cancellationToken);

            var keys = await _dapr.GetStateAsync<List<string>>(ConstantValues.Services.StoreName, ConstantValues.Services.ServicesIndexKey, cancellationToken : cancellationToken) ?? new();
            if (keys.Contains(key))
            {
                keys.Remove(key);
                await _dapr.SaveStateAsync(ConstantValues.Services.StoreName, ConstantValues.Services.ServicesIndexKey, keys, cancellationToken : cancellationToken);
            }

            return "Service Ad Deleted Successfully";
        }
    }
}
