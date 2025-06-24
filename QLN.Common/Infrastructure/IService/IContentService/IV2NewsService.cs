using QLN.Common.DTO_s;
using System.Threading;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IContentService
{
    public interface IV2NewsService
    {
        Task<string> CreateNews(V2ContentNewsDto dto, CancellationToken cancellationToken = default);
        //
        Task<List<V2ContentNewsDto>> GetAllNews( CancellationToken cancellationToken = default);
        Task<V2ContentNewsDto?> GetNewsById(Guid id, CancellationToken cancellationToken = default);
        Task<string> UpdateNews(V2ContentNewsDto dto, CancellationToken cancellationToken = default);
        Task<bool> DeleteNews(Guid id, CancellationToken cancellationToken = default);


        // news category 
        Task<string> CreateNewsCategoryAsync(NewsCategoryDto dto, CancellationToken cancellationToken = default);
        Task<List<NewsCategoryDto>> GetAllNewsCategoriesAsync(CancellationToken cancellationToken = default);



        ///////// New News Json 
        Task<Dictionary<string, string>> GetWriterTagsAsync(CancellationToken cancellationToken = default);
        Task<string> CreateNewsArticleAsync(Guid userId, V2NewsArticleDTO dto, CancellationToken cancellationToken = default);
    }
}
