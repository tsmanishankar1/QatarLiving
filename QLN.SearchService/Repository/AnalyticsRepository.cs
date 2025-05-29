using System;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QLN.SearchService.IndexModels;
using QLN.SearchService.IRepository;
using QLN.SearchService.Models;

namespace QLN.SearchService.Repository
{
    public class AnalyticsRepository : IAnalyticsRepository
    {
        private readonly SearchClient _searchClient;
        private readonly ILogger<AnalyticsRepository> _logger;

        public AnalyticsRepository(
            IOptions<AzureSearchSettings> opts,
            ILogger<AnalyticsRepository> logger)
        {
            var settings = opts.Value;
            _searchClient = new SearchClient(
                new Uri(settings.Endpoint),
                settings.Indexes[Constants.Constants.analytics],
                new AzureKeyCredential(settings.ApiKey)
            );
            _logger = logger;
        }

        public async Task<AnalyticsIndex?> GetByKeyAsync(string key)
        {
            try
            {
                var resp = await _searchClient.GetDocumentAsync<AnalyticsIndex>(key);
                return resp.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogInformation("Analytics key '{Key}' not found", key);
                return null;
            }
        }

        public async Task UpsertAsync(AnalyticsIndex item)
        {
            try
            {
                var batch = IndexDocumentsBatch.MergeOrUpload(new[] { item });
                await _searchClient.IndexDocumentsAsync(batch);
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Failed to upsert analytics key '{Key}'", item.Id);
                throw;
            }
        }
    }
}
