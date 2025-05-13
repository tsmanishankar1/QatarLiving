using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;
using QLN.SearchService.IndexModels;
using QLN.SearchService.IRepository;
using QLN.SearchService.Models;

namespace QLN.SearchService.Repository
{
    public class SearchRepository : ISearchRepository
    {
        private readonly SearchClient _searchClient;

        public SearchRepository(IConfiguration config)
        {
            var endpoint = new Uri(config["AzureSearch:Endpoint"]);
            var credential = new AzureKeyCredential(config["AzureSearch:ApiKey"]);
            var indexName = config["AzureSearch:IndexName"];
            _searchClient = new SearchClient(endpoint, indexName, credential);
        }

        public async Task<IEnumerable<ClassifiedIndex>> SearchAsync(SearchRequest req)
        {
            var options = new SearchOptions
            {
                Size = req.Top > 0 ? req.Top : 50,
                IncludeTotalCount = true
            };

            // add your facets:
            options.Facets.Add("Category");
            options.Facets.Add("L1Category");
            options.Facets.Add("L2Category");
            // …etc…

            var filter = BuildFilter(req);
            if (!string.IsNullOrWhiteSpace(filter))
                options.Filter = filter;

            var response = await _searchClient.SearchAsync<ClassifiedIndex>(
                req.Text ?? "*", options);

            return response.Value.GetResults().Select(r => r.Document);
        }

        public async Task<string> UploadAsync(ClassifiedIndex document)
        {
            if (string.IsNullOrEmpty(document.Id))
                document.Id = Guid.NewGuid().ToString();
            if (document.CreatedDate == default)
                document.CreatedDate = DateTime.UtcNow;

            var batch = IndexDocumentsBatch.Upload(new[] { document });
            await _searchClient.IndexDocumentsAsync(batch);
            return "Document uploaded successfully.";
        }

        private string BuildFilter(SearchRequest request)
        {
            var filters = new List<string>();
            void AddIf(string expr)
            {
                if (!string.IsNullOrWhiteSpace(expr)) filters.Add(expr);
            }

            AddIf(!string.IsNullOrEmpty(request.Category)
                ? $"Category eq '{request.Category}'" : null);
            AddIf(!string.IsNullOrEmpty(request.L1Category)
                ? $"L1Category eq '{request.L1Category}'" : null);
            AddIf(!string.IsNullOrEmpty(request.L2Category)
                ? $"L2Category eq '{request.L2Category}'" : null);
            // …repeat for all your string‐based filters…

            if (request.MinPrice.HasValue)
                AddIf($"Price ge {request.MinPrice.Value}");
            if (request.MaxPrice.HasValue)
                AddIf($"Price le {request.MaxPrice.Value}");

            if (request.IsFeaturedItem.HasValue)
                AddIf($"IsFeaturedItem eq {request.IsFeaturedItem.Value.ToString().ToLower()}");
            // …and so on…

            return filters.Count > 0
                ? string.Join(" and ", filters)
                : null;
        }

        public async Task<IEnumerable<ClassifiedIndex>> GetFeaturedItemsAsync()
        {
            var options = new SearchOptions
            {
                Filter = "IsFeaturedItem eq true",
                Size = 10,
                OrderBy = { "CreatedDate desc" }
            };
            var resp = await _searchClient.SearchAsync<ClassifiedIndex>("*", options);
            return resp.Value.GetResults().Select(r => r.Document);
        }

        public async Task<IEnumerable<CategoryAdCount>> GetCategoryAdCountsAsync()
        {
            var options = new SearchOptions
            {
                Size = 0,
                Facets = { "Category,count:1000" }
            };
            var resp = await _searchClient.SearchAsync<ClassifiedIndex>("*", options);
            return resp.Value.Facets["Category"]
                       .Select(f => new CategoryAdCount
                       {
                           Category = f.Value.ToString(),
                           Count = (int)(f.Count ?? 0)
                       });
        }

        public async Task<IEnumerable<LandingCategoryInfo>> GetFeaturedCategoriesAsync()
        {
            var options = new SearchOptions
            {
                Filter = "IsFeaturedCategory eq true",
                Size = 20
            };
            var resp = await _searchClient.SearchAsync<ClassifiedIndex>("*", options);
            return resp.Value.GetResults()
                       .GroupBy(r => r.Document.Category)
                       .Select(g => new LandingCategoryInfo
                       {
                           Category = g.Key,
                           ImageUrl = $"/images/categories/{g.Key.Replace(" ", "_").ToLower()}.svg"
                       });
        }

        public async Task<IEnumerable<LandingStoreInfo>> GetStoresWithCountsAsync()
        {
            var options = new SearchOptions
            {
                Size = 0,
                Facets = { "StoreName,count:10" }
            };
            var resp = await _searchClient.SearchAsync<ClassifiedIndex>("*", options);
            return resp.Value.Facets["StoreName"]
                       .Select(f => new LandingStoreInfo
                       {
                           StoreName = f.Value.ToString(),
                           LogoUrl = $"/logos/{f.Value.ToString().Replace(" ", "_").ToLower()}.png",
                           ItemCount = (int)f.Count.GetValueOrDefault()
                       });
        }
    }
}
