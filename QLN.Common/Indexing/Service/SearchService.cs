using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Azure.Search.Documents;
using Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QLN.Common.Indexing.IService;

namespace QLN.Common.Indexing.Service
{
    public class SearchService<T> : ISearchService<T>
    {
        private readonly SearchIndexClient _indexClient;

        public SearchService(string serviceEndpoint, string adminApiKey)
        {
            Uri endpoint = new Uri(serviceEndpoint);
            _indexClient = new SearchIndexClient(endpoint, new AzureKeyCredential(adminApiKey));
        }

        // New method: create the index only if it does not exist.
        public async Task CreateIndexIfNotExistsAsync(string indexName, CancellationToken cancellationToken = default)
        {
            try
            {
                // Attempt to fetch the existing index.
                var existingIndex = await _indexClient.GetIndexAsync(indexName, cancellationToken);
                // If no exception is thrown, the index exists—do nothing.
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                // Index does not exist; build and create it.
                var fieldBuilder = new FieldBuilder();
                var searchFields = fieldBuilder.Build(typeof(T));
                var definition = new SearchIndex(indexName, searchFields);
                await _indexClient.CreateIndexAsync(definition, cancellationToken: cancellationToken);
            }
        }

        // This method is kept for backward compatibility if needed.
        public async Task CreateOrUpdateIndexAsync(string indexName, CancellationToken cancellationToken = default)
        {
            var fieldBuilder = new FieldBuilder();
            var searchFields = fieldBuilder.Build(typeof(T));
            var definition = new SearchIndex(indexName, searchFields);

            await _indexClient.CreateOrUpdateIndexAsync(definition, cancellationToken: cancellationToken);
        }

        public async Task UploadDocumentsAsync(string indexName, IEnumerable<T> documents, CancellationToken cancellationToken = default)
        {
            SearchClient searchClient = _indexClient.GetSearchClient(indexName);
            var actions = documents.Select(doc => IndexDocumentsAction.Upload<T>(doc)).ToArray();
            var batch = IndexDocumentsBatch.Create<T>(actions);
            await searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);
        }


        public async Task<SearchResults<T>> SearchAsync(string indexName, string searchText, SearchOptions? options = null, CancellationToken cancellationToken = default)
        {
            SearchClient searchClient = _indexClient.GetSearchClient(indexName);
            return await searchClient.SearchAsync<T>(searchText, options, cancellationToken);
        }
    }
}
