using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.IContentService;
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

        public async Task<CreatePostResponse?> CreateDiscussionPostOnDrupalAsync(CreateDiscussionPostRequest request, CancellationToken cancellationToken)
        {

            //httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtWoken);

            var httpRequest = await httpClient.PostAsJsonAsync<CreateDiscussionPostRequest>(DrupalContentConstants.PostsSavePath, request, cancellationToken);

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
        // qlnapi/events?category_id=126205&location_id=102701&date=2025-01-01'
        public async Task<ContentEventsResponse?> GetEventsFromDrupalAsync(
            CancellationToken cancellationToken, 
            string? category_id = null,
            string? location_id = null,
            string? date = null,
            string? order = "asc",
            int? page = 1, 
            int? page_size = 20
            )
        {
            string requestUri = $"{DrupalContentConstants.EventsPath}?page={page}&page_size={page_size}&order={order}";
            if (!string.IsNullOrEmpty(category_id))
            {
                requestUri += $"&category_id={category_id}";
            }

            if (!string.IsNullOrEmpty(location_id))
            {
                requestUri += $"&location_id={location_id}";
            }

            if (!string.IsNullOrEmpty(date))
            {
                requestUri += $"&date={date}";
            }


            return await httpClient.GetFromJsonAsync<ContentEventsResponse>(requestUri, cancellationToken);
        }

        public async Task<CategoriesResponse?> GetCategoriesFromDrupalAsync(CancellationToken cancellationToken)
        {
            return await httpClient.GetFromJsonAsync<CategoriesResponse>(DrupalContentConstants.CategoriesPath, cancellationToken);
        }

        public async Task<CommunitiesResponse?> GetCommunitiesFromDrupalAsync(CancellationToken cancellationToken, string? forum_id = null, string? order = "asc", int? page = 1, int? page_size = 10)
        {
            string requestUri = $"{DrupalContentConstants.CommunityPath}?page={page}&page_size={page_size}&order={order}";
            if (!string.IsNullOrEmpty(forum_id))
            {
                requestUri += $"&forum_id={forum_id}";
            }

            return await httpClient.GetFromJsonAsync<CommunitiesResponse>(requestUri, cancellationToken);
        }

        public async Task<GetCommentsResponse?> GetCommentsFromDrupalAsync(string forum_id, CancellationToken cancellationToken, int? page = 1, int? page_size = 10)
        {
            return await httpClient.GetFromJsonAsync<GetCommentsResponse>($"{DrupalContentConstants.CommentsGetPath}/{forum_id}?page={page}&page_size={page_size}", cancellationToken);
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
