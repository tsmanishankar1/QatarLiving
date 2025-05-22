// QLN.SearchService.Service/SearchService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QLN.SearchService.IRepository;
using QLN.SearchService.IService;
using QLN.SearchService.IndexModels;
using QLN.SearchService.Models;

namespace QLN.SearchService.Service
{
    public class SearchService : ISearchService
    {
        private readonly ISearchRepository _repo;
        private readonly ILogger<SearchService> _logger;

        public SearchService(
            ISearchRepository repo,
            ILogger<SearchService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public Task<IEnumerable<ClassifiedIndex>> Search(
            string vertical,
            SearchRequest req)
            => _repo.Search<ClassifiedIndex>(vertical, req);

        public async Task<string> Upload(CommonIndexRequest request)
        {
            var vertical = request.VerticalName?.ToLowerInvariant()
                           ?? throw new ArgumentException("VerticalName is required");

            if (vertical == Constants.Constants.classifieds)
            {
                var item = request.ClassifiedsItem
                    ?? throw new ArgumentException("ClassifiedsItem is required for classifieds upload.");

                _logger.LogInformation(
                    "Uploading ClassifiedIndex Id={Id} into '{Vertical}'",
                    item.Id, vertical);
                return await _repo.Upload<ClassifiedIndex>(vertical, item);
            }

            // future: handle other vertical values similarly
            throw new ArgumentException($"Unsupported vertical: '{request.VerticalName}'");
        }

        public Task<ClassifiedIndex?> GetById(
            string vertical,
            string key)
            => _repo.GetById<ClassifiedIndex>(vertical, key);
    }
}
