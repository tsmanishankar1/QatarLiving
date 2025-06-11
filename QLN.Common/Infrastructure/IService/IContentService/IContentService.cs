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
        Task<ContentEventsResponse?> GetEventsFromDrupalAsync(
            CancellationToken cancellationToken,
            string? category_id = null,
            string? location_id = null,
            string? from = null,
            string? to = null,
            string? order = null,
            int? page = null,
            int? page_size = null
            );
        Task<CategoriesResponse?> GetCategoriesFromDrupalAsync(CancellationToken cancellationToken);
        Task<CommunitiesResponse?> GetCommunitiesFromDrupalAsync(
            CancellationToken cancellationToken, 
            string? forum_id, 
            string? order = null, 
            int? page_size = null, 
            int? page = null
            );
        Task<ContentPost?> GetNewsBySlugAsync(string slug, CancellationToken cancellationToken);
        Task<CreateCommentResponse?> CreateCommentOnDrupalAsync(CreateCommentRequest request, CancellationToken cancellationToken);
        Task<CreatePostResponse?> CreateDiscussionPostOnDrupalAsync(CreateDiscussionPostRequest request, CancellationToken cancellationToken);
        Task<ChangeLikeStatusResponse?> ChangeLikeStatusOnDrupalAsync(ChangeLikeStatusRequest request, CancellationToken cancellationToken);
        Task<GetCommentsResponse?> GetCommentsFromDrupalAsync(
            string forum_id, 
            CancellationToken cancellationToken,
            int? page_size = null,
            int? page = null
            );
    }
}