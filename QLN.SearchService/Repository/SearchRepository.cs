using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using QLN.SearchService.IRepository;
using QLN.SearchService.IndexModels;

namespace QLN.SearchService.Repository
{
    public class SearchRepository : ISearchRepository
    {
        private readonly SearchClient _searchClient;

        public SearchRepository(IConfiguration config)
        {
            var endpoint = new Uri(config["AzureSearch:Endpoint"]);
            var key = new AzureKeyCredential(config["AzureSearch:ApiKey"]);
            var indexName = config["AzureSearch:IndexName"];
            _searchClient = new SearchClient(endpoint, indexName, key);
        }

        public async Task<IEnumerable<ClassifiedIndex>> SearchAsync(SearchRequest request)
        {
            var options = new SearchOptions
            {
                Filter = BuildFilter(request),
                Size = request.Top > 0 ? request.Top : 50
            };

            var response = await _searchClient.SearchAsync<ClassifiedIndex>(request.Text ?? "*", options);
            return response.Value.GetResults().Select(r => r.Document);
        }

        public async Task<string> UploadAsync(ClassifiedIndex document)
        {
            var batch = IndexDocumentsBatch.Create(IndexDocumentsAction.Upload(document));
            await _searchClient.IndexDocumentsAsync(batch);
            return "Document uploaded successfully.";
        }

        private string BuildFilter(SearchRequest request)
        {
            var filters = new List<string>();

            if (!string.IsNullOrEmpty(request.Category))
                filters.Add($"Category eq '{request.Category}'");

            if (!string.IsNullOrEmpty(request.L1Category))
                filters.Add($"L1Category eq '{request.L1Category}'");

            if (!string.IsNullOrEmpty(request.L2Category))
                filters.Add($"L2Category eq '{request.L2Category}'");

            if (!string.IsNullOrEmpty(request.Location))
                filters.Add($"Location eq '{request.Location}'");

            if (!string.IsNullOrEmpty(request.Make))
                filters.Add($"Make eq '{request.Make}'");

            if (!string.IsNullOrEmpty(request.Model))
                filters.Add($"Model eq '{request.Model}'");

            if (!string.IsNullOrEmpty(request.Condition))
                filters.Add($"Condition eq '{request.Condition}'");

            if (!string.IsNullOrEmpty(request.Storage))
                filters.Add($"Storage eq '{request.Storage}'");

            if (!string.IsNullOrEmpty(request.Colour))
                filters.Add($"Colour eq '{request.Colour}'");

            if (!string.IsNullOrEmpty(request.Coverage))
                filters.Add($"Coverage eq '{request.Coverage}'");

            if (!string.IsNullOrEmpty(request.Size))
                filters.Add($"Size eq '{request.Size}'");

            if (!string.IsNullOrEmpty(request.Gender))
                filters.Add($"Gender eq '{request.Gender}'");

            if (request.MinPrice.HasValue)
                filters.Add($"Price ge {request.MinPrice.Value}");

            if (request.MaxPrice.HasValue)
                filters.Add($"Price le {request.MaxPrice.Value}");

            return filters.Count > 0 ? string.Join(" and ", filters) : null;
        }

    }
}
