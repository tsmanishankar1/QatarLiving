using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.IRepository.ISearchServiceRepository;
using QLN.Common.Infrastructure.IService.ISearchService;
using QLN.SearchService.Repository;

namespace QLN.SearchService.Service
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly IAnalyticsRepository _repo;
        private readonly ILogger<AnalyticsService> _logger;

        public AnalyticsService(
            IAnalyticsRepository repo,
            ILogger<AnalyticsService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public Task<ApiResponse<object>> GetAnalyticsAsync(AnalyticsRequestDto request, string userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<AnalyticsIndex?> GetAsync(string section, string entityId)
        {
            if (string.IsNullOrWhiteSpace(section) || string.IsNullOrWhiteSpace(entityId))
                throw new ArgumentException("Section and EntityId are required.");

            var key = $"{section}_{entityId}";
            return await _repo.GetByKeyAsync(key);
        }

        public async Task UpsertAsync(AnalyticsEventRequest req)
        {
            if (req == null
                || string.IsNullOrWhiteSpace(req.Section)
                || string.IsNullOrWhiteSpace(req.EntityId))
            {
                throw new ArgumentException("Section and EntityId are required.");
            }

            var key = $"{req.Section}_{req.EntityId}";
            var existing = await _repo.GetByKeyAsync(key)
                           ?? new AnalyticsIndex
                           {
                               Id = key,
                               Section = req.Section,
                               EntityId = req.EntityId,
                               Impressions = 0,
                               Views = 0,
                               WhatsApp = 0,
                               Calls = 0,
                               Shares = 0,
                               Saves = 0
                           };

            existing.Impressions += req.Impressions;
            existing.Views += req.Views;
            existing.WhatsApp += req.WhatsApp;
            existing.Calls += req.Calls;
            existing.Shares += req.Shares;
            existing.Saves += req.Saves;
            existing.LastUpdated = DateTimeOffset.UtcNow;

            await _repo.UpsertAsync(existing);
        }
    }
}
