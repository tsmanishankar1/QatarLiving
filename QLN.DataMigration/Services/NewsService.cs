using Dapr.Client;
using FirebaseAdmin.Messaging;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Common.Infrastructure.IService.IFileStorage;
using System.Text;
using System.Text.Json;

namespace QLN.DataMigration.Services
{
    public class NewsService : IV2NewsService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<NewsService> _logger;

        public NewsService(
            DaprClient dapr,
            ILogger<NewsService> logger
            )
        {
            _dapr = dapr;
            _logger = logger;
        }

        public async Task AddCategoryAsync(V2NewsCategory category, CancellationToken cancellationToken = default)
        {
            var url = "/api/v2/news/category/createById";
            var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.V2Content.ContentServiceAppId, url);
            request.Content = new StringContent(JsonSerializer.Serialize(category), Encoding.UTF8, "application/json");

            var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        public async Task<string> BulkMigrateNewsArticleAsync(List<V2NewsArticleDTO> articles, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/api/v2/news/bulkMigrate";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.V2Content.ContentServiceAppId, url);
                request.Content = new StringContent(JsonSerializer.Serialize(articles), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<string>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                       ?? throw new Exception("Empty or invalid response from content service.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating news article via external service");
                throw;
            }
        }

        public async Task<string> MigrateNewsArticleAsync(V2NewsArticleDTO article, CancellationToken cancellationToken = default)
        {
            try
            {
                await _dapr.PublishEventAsync(
                            pubsubName: ConstantValues.PubSubName,
                            topicName: ConstantValues.PubSubTopics.ArticlesMigration,
                            data: article,
                            cancellationToken: cancellationToken
                        );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error publishing article {article.Id} to {ConstantValues.PubSubTopics.ArticlesMigration} topic");
                throw;
            }

            return $"Published article {article.Id} to {ConstantValues.PubSubTopics.ArticlesMigration} topic";
        }

        public async Task<string> CreateNewsArticleAsync(string userId, V2NewsArticleDTO dto, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "/api/v2/news/createNewsArticleById";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.V2Content.ContentServiceAppId, url);
                request.Content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<string>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                       ?? throw new Exception("Empty or invalid response from content service.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating news article via external service");
                throw;
            }
        }

        public Task<string> CreateWritertagAsync(Writertag dto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> DeleteNews(Guid id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> DeleteTagName(Guid id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<NewsCommentApiResponse> EditNewsCommentAsync(string articleId, Guid commentId, string userId, string updatedText, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<V2NewsCategory>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResponse<V2NewsArticleDTO>> GetAllNewsArticlesAsync(int? page, int? perPage, string? search, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<V2NewsArticleDTO>> GetAllNewsFilterArticles(bool? isActive = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<V2NewsSlot>> GetAllSlotsAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<Writertag>> GetAllWritertagsAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<V2NewsArticleDTO?> GetArticleByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<V2NewsArticleDTO?> GetArticleBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<List<V2NewsArticleDTO>> GetArticlesByCategoryIdAsync(int categoryId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<List<V2NewsArticleDTO>> GetArticlesBySubCategoryIdAsync(int categoryId, int subCategoryId, ArticleStatus status, string? search, int? page, int? pageSize, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<V2NewsCategory?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<NewsCommentListResponse> GetCommentsByArticleIdAsync(string nid, int? page = null, int? perPage = null, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<GenericNewsPageResponse> GetNewsLandingPageAsync(int categoryId, int subCategoryId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> LikeNewsCommentAsync(string commentId, string userId, string userName, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> ReorderSlotsAsync(NewsSlotReorderRequest dto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<NewsCommentApiResponse> SaveNewsCommentAsync(V2NewsCommentDto dto, CancellationToken ct = default)
        {
            try
            {
                await _dapr.PublishEventAsync(
                            pubsubName: ConstantValues.PubSubName,
                            topicName: ConstantValues.PubSubTopics.NewsCommentsMigration,
                            data: dto,
                            cancellationToken: ct
                        );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error publishing comment {dto.CommentId} to {ConstantValues.PubSubTopics.NewsCommentsMigration} topic");
                throw;
            }

            return new NewsCommentApiResponse
            {
                Status = "Success",
                Message = $"Published comment {dto.CommentId} to {ConstantValues.PubSubTopics.NewsCommentsMigration} topic"
            };
        }

        public Task<NewsCommentApiResponse> SoftDeleteNewsCommentAsync(string articleId, Guid commentId, string userId, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> UpdateNewsArticleAsync(V2NewsArticleDTO updatedDto, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateSubCategoryAsync(int categoryId, V2NewsSubCategory updatedSubCategory, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
