using Azure.Search.Documents.Models;
using QLN.SearchService.IndexModels;
using QLN.SearchService.IRepository;
using QLN.SearchService.IService;
using QLN.SearchService.Models;

namespace QLN.SearchService.Service
{
    public class SearchService : ISearchService
    {
        private readonly ISearchRepository _repo;
        public SearchService(ISearchRepository repo) => _repo = repo;

        public Task<IEnumerable<SearchDocument>> SearchAsync(string vertical, SearchRequest req)
            => _repo.SearchAsync<SearchDocument>(vertical, req);

        public Task<string> UploadAsync(string vertical, SearchDocument document)
            => _repo.UploadAsync(vertical, document);
    }
}
