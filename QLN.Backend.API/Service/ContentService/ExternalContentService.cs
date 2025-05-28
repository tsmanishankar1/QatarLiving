using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.BannerService;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QLN.Backend.API.Service.ContentService
{
    public class ExternalContentService(HttpClient httpClient) : IContentService
    {

        public async Task<T?> GetPostsFromDrupalAsync<T>(string queue_name, CancellationToken cancellationToken)
        {
            return await httpClient.GetFromJsonAsync<T>($"{ContentConstants.LandingPath}/{queue_name}", cancellationToken);
        }

        public async Task<List<ContentEvent>?> GetEventsFromDrupalAsync(CancellationToken cancellationToken)
        {
            return await httpClient.GetFromJsonAsync<List<ContentEvent>>(ContentConstants.EventsPath, cancellationToken);
        }

        public async Task<CategoriesResponse?> GetCategoriesFromDrupalAsync(CancellationToken cancellationToken)
        {
            return await httpClient.GetFromJsonAsync<CategoriesResponse>(ContentConstants.CategoriesPath, cancellationToken);
        }

        public async Task<List<CommunityPost>?> GetCommunitiesFromDrupalAsync(string forum_id, CancellationToken cancellationToken, string? order = "asc", int? page = 1, int? page_size = 10)
        {
            return await httpClient.GetFromJsonAsync<List<CommunityPost>>($"{ContentConstants.CommunityPath}?page={page}&page_size={page_size}&order={order}&forum_id={forum_id}", cancellationToken);
        }

        public async Task<ContentPost?> GetPostBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            var results = await httpClient.GetFromJsonAsync<ContentPost>($"{ContentConstants.GetPostBySlugPath}?slug={slug}", cancellationToken);

            if (results?.NodeType == "post")
            {
                return results;
            }

            return null;
        }

        public async Task<ContentPost?> GetNewsBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            var results = await httpClient.GetFromJsonAsync<ContentPost>($"{ContentConstants.GetNewsBySlugPath}?slug={slug}", cancellationToken);

            if (results?.NodeType == "post")
            {
                return results;
            }

            return null;
        }

        public async Task<ContentEvent?> GetEventBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            var results = await httpClient.GetFromJsonAsync<ContentEvent>($"{ContentConstants.GetEventBySlugPath}?slug={slug}", cancellationToken);

            if (results?.NodeType == "event")
            {
                return results;
            }

            return null;
        }
    }
}
