using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IContentService
{
    public interface IContentService
    {
        Task<ContentPost?> GetPostBySlugAsync(string slug, CancellationToken cancellationToken);
        Task<ContentEvent?> GetEventBySlugAsync(string slug, CancellationToken cancellationToken);
        Task<T?> GetPostsFromDrupalAsync<T>(string queue_name, CancellationToken cancellationToken);
        Task<List<ContentEvent>?> GetEventsFromDrupalAsync(CancellationToken cancellationToken);
        Task<CategoriesResponse?> GetCategoriesFromDrupalAsync(CancellationToken cancellationToken);
        Task<List<CommunityPost>?> GetCommunitiesFromDrupalAsync(string forum_id, CancellationToken cancellationToken, string? order = "asc", int? page_size = 10, int? page = 1);
        Task<ContentPost?> GetNewsBySlugAsync(string slug, CancellationToken cancellationToken);
        Task<CreateCommentResponse?> CreateCommentOnDrupalAsync(CreateCommentRequest request, CancellationToken cancellationToken);
        Task<CreatePostResponse?> CreatePostOnDrupalAsync(CreatePostRequest request, CancellationToken cancellationToken);
        Task<ChangeLikeStatusResponse?> ChangeLikeStatusOnDrupalAsync(ChangeLikeStatusRequest request, CancellationToken cancellationToken);
    }
}