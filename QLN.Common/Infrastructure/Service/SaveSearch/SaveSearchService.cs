using Dapr;
using Dapr.Client;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Service.SaveSearch
{
    public class SaveSearchService : ISaveSearchService
    {
        private readonly DaprClient _daprClient;
        private const string StoreName = "searchStateStore";

        public SaveSearchService(DaprClient daprClient)
        {
            _daprClient = daprClient;
        }

        public async Task<bool> SaveSearchAsync(SaveSearchRequestDto dto)
        {
            try
            {
                if (dto == null)
                    throw new ArgumentNullException(nameof(dto));

                var key = $"search:{dto.UserId}";

                var existing = await _daprClient.GetStateAsync<List<SavedSearchResponseDto>>(StoreName, key)
                               ?? new List<SavedSearchResponseDto>();

                var newSearch = new SavedSearchResponseDto
                {
                    Id = Guid.NewGuid(),
                    UserId = dto.UserId,
                    Name = dto.Name,
                    CreatedAt = DateTime.UtcNow,
                    SearchQuery = dto.SearchQuery
                };




                existing.Insert(0, newSearch);

                if (existing.Count > 30)
                    existing = existing.Take(30).ToList();

                await _daprClient.SaveStateAsync(StoreName, key, existing);
                return true;
            }
            catch (DaprException dex)
            {
                // Optionally log
                Console.WriteLine($"Dapr error: {dex.Message}");
                throw new InvalidOperationException("Failed to save search due to Dapr error.", dex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                throw new InvalidOperationException("An unexpected error occurred while saving search.", ex);
            }
        }

        public async Task<List<SavedSearchResponseDto>> GetSearchesAsync(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("UserId is required.");

                var key = $"search:{userId}";
                var result = await _daprClient.GetStateAsync<List<SavedSearchResponseDto>>(StoreName, key);

                return result ?? new List<SavedSearchResponseDto>();
            }
            catch (DaprException dex)
            {
                Console.WriteLine($"Dapr error: {dex.Message}");
                throw new InvalidOperationException("Failed to retrieve saved searches due to Dapr error.", dex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                throw new InvalidOperationException("An unexpected error occurred while retrieving saved searches.", ex);
            }
        }
    }
}
