using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.BannerService;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QLN.Backend.API.Service.ContentService
{
    public class ExternalContentService(HttpClient httpClient) : IContentService
    {
        public async Task<CreateCommentResponse?> CreateCommentOnDrupalAsync(CreateCommentRequest request, CancellationToken cancellationToken)
        {

            //httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtWoken);

            var httpRequest = await httpClient.PostAsJsonAsync<CreateCommentRequest>(DrupalContentConstants.CommentsSavePath, request, cancellationToken);

            if(httpRequest.IsSuccessStatusCode)
            {
                var response = await httpRequest.Content.ReadFromJsonAsync<CreateCommentResponse>(cancellationToken);
                return response;
            }

            return null;
        }

        public async Task<CreatePostResponse?> CreatePostOnDrupalAsync(CreatePostRequest request, CancellationToken cancellationToken)
        {

            //httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtWoken);

            var httpRequest = await httpClient.PostAsJsonAsync<CreatePostRequest>(DrupalContentConstants.PostsSavePath, request, cancellationToken);

            if (httpRequest.IsSuccessStatusCode)
            {
                var response = await httpRequest.Content.ReadFromJsonAsync<CreatePostResponse>(cancellationToken);
                return response;
            }

            return null;
        }

        public async Task<ChangeLikeStatusResponse?> ChangeLikeStatusOnDrupalAsync(ChangeLikeStatusRequest request, CancellationToken cancellationToken)
        {

            //httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtWoken);

            var httpRequest = await httpClient.PostAsJsonAsync<ChangeLikeStatusRequest>(DrupalContentConstants.ChangeLikeStatusPath, request, cancellationToken);

            if (httpRequest.IsSuccessStatusCode)
            {
                var response = await httpRequest.Content.ReadFromJsonAsync<ChangeLikeStatusResponse>(cancellationToken);
                return response;
            }

            return null;
        }

        public async Task<T?> GetPostsFromDrupalAsync<T>(string queue_name, CancellationToken cancellationToken)
        {
            return await httpClient.GetFromJsonAsync<T>($"{DrupalContentConstants.LandingPath}/{queue_name}", cancellationToken);
        }

        public async Task<List<ContentEvent>?> GetEventsFromDrupalAsync(CancellationToken cancellationToken)
        {
            return await httpClient.GetFromJsonAsync<List<ContentEvent>>(DrupalContentConstants.EventsPath, cancellationToken);
        }

        public async Task<CategoriesResponse?> GetCategoriesFromDrupalAsync(CancellationToken cancellationToken)
        {
            return await httpClient.GetFromJsonAsync<CategoriesResponse>(DrupalContentConstants.CategoriesPath, cancellationToken);
        }

        public async Task<List<CommunityPost>?> GetCommunitiesFromDrupalAsync(string forum_id, CancellationToken cancellationToken, string? order = "asc", int? page = 1, int? page_size = 10)
        {
            return await httpClient.GetFromJsonAsync<List<CommunityPost>>($"{DrupalContentConstants.CommunityPath}?page={page}&page_size={page_size}&order={order}&forum_id={forum_id}", cancellationToken);
        }

        public async Task<ContentPost?> GetPostBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            var results = await httpClient.GetFromJsonAsync<ContentPost>($"{DrupalContentConstants.GetPostBySlugPath}?slug={slug}", cancellationToken);

            if (results?.NodeType == "post")
            {
                return results;
            }

            return null;
        }

        public async Task<ContentPost?> GetNewsBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            var results = await httpClient.GetFromJsonAsync<ContentPost>($"{DrupalContentConstants.GetNewsBySlugPath}?slug={slug}", cancellationToken);

            if (results?.NodeType == "post")
            {
                return results;
            }

            return null;
        }

        public async Task<ContentEvent?> GetEventBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            var results = await httpClient.GetFromJsonAsync<ContentEvent>($"{DrupalContentConstants.GetEventBySlugPath}?slug={slug}", cancellationToken);

            if (results?.NodeType == "event")
            {
                return results;
            }

            return null;
        }
    }
}
