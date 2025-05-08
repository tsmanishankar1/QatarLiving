using QLN.SearchService.IndexModels;
using QLN.SearchService.IRepository;
using QLN.SearchService.IService;

namespace QLN.SearchService.Service
{
    public class SearchService : ISearchService
    {
        private readonly ISearchRepository _repository;

        public SearchService(ISearchRepository repository)
        {
            _repository = repository;
        }

        public Task<IEnumerable<ClassifiedIndex>> SearchAsync(SearchRequest request)
            => _repository.SearchAsync(request);

        public Task<string> UploadAsync(ClassifiedIndex document)
        => _repository.UploadAsync(document);
    }
}
