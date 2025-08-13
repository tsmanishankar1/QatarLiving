using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using Dapr;
using Dapr.Client;
using Dapr.Client.Autogen.Grpc.v1;
using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.Mvc;
using QLN.Common.DTO_s;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.CustomException;
using QLN.Common.Infrastructure.DTO_s;
using QLN.Common.Infrastructure.IService.IContentService;
using QLN.Common.Infrastructure.IService.IFileStorage;
using QLN.Common.Infrastructure.IService.ISearchService;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using static QLN.Common.Infrastructure.Constants.ConstantValues;

namespace QLN.Backend.API.Service.V2ContentService
{
    public class V2ExternalNewsService : IV2NewsService
    {
        private readonly DaprClient _dapr;
        private readonly ILogger<V2ExternalNewsService> _logger;
        private readonly IFileStorageBlobService _blobStorage;
        private readonly ISearchService _search;

        public V2ExternalNewsService(
            DaprClient dapr,
            ILogger<V2ExternalNewsService> logger,
            IFileStorageBlobService blobStorage,
            ISearchService search)
        {
            _dapr = dapr;
            _logger = logger;
            _blobStorage = blobStorage;
            _search = search;
        }

        private static V2NewsArticleDTO MapIndexToDto(ContentNewsIndex i)
        {
            _ = i ?? throw new ArgumentNullException(nameof(i));
            Guid.TryParse(i.Id, out var id);

            return new V2NewsArticleDTO
            {
                Id = id,
                Title = i.Title,
                Content = i.Content,
                authorName = i.authorName,
                CoverImageUrl = i.CoverImageUrl,
                Slug = i.Slug,
                UserId = i.UserId,
                WriterTag = i.WriterTag,
                PublishedDate = i.PublishedDate,
                IsActive = i.IsActive,
                CreatedBy = i.CreatedBy,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt,
                UpdatedBy = i.UpdatedBy,
                Categories = i.Categories?.Select(c => new V2ArticleCategory
                {
                    CategoryId = c.CategoryId,
                    SubcategoryId = c.SubcategoryId,
                    SlotId = c.SlotId
                }).ToList() ?? new()
            };
        }

        public async Task<string> CreateWritertagAsync(Writertag dto, CancellationToken cancellationToken)
        {
            try
            {
                var appId = ConstantValues.V2Content.ContentServiceAppId;
                var path = "/api/v2/news/createtagnamebyuserid";

                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, appId, path);
                request.Content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadAsStringAsync(cancellationToken);

                return result; 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating writer tag via internal service");
                throw;
            }
        }
        public async Task<List<Writertag>> GetAllWritertagsAsync(CancellationToken cancellationToken)
        {
            try
            {
                var appId = ConstantValues.V2Content.ContentServiceAppId;
                var path = "/api/v2/news/writertags";

                return await _dapr.InvokeMethodAsync<List<Writertag>>(
               HttpMethod.Get,
               appId,
               path,
               cancellationToken
           ) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving writer tags from internal service");
                throw;
            }
        }
        public async Task<List<V2NewsSlot>> GetAllSlotsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var appId = ConstantValues.V2Content.ContentServiceAppId;
                var path = "/api/v2/news/slots";

                return await _dapr.InvokeMethodAsync<List<V2NewsSlot>>(
               HttpMethod.Get,
               appId,
               path,
               cancellationToken
           ) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving writer tags from internal service");
                throw;
            }
        }
        public async Task<string> CreateNewsArticleAsync(string userId, V2NewsArticleDTO dto, CancellationToken cancellationToken = default)
        {
            try
            {
                dto.Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id;
                dto.CreatedBy = userId;
                dto.UpdatedBy = userId;
                dto.CreatedAt = DateTime.UtcNow;
                dto.UpdatedAt = DateTime.UtcNow;

                // Upload image to blob storage if present
                if (!string.IsNullOrWhiteSpace(dto.CoverImageUrl))
                {
                    var imageName = $"{dto.Title}_{dto.Id}.png";
                    dto.CoverImageUrl = await _blobStorage.SaveBase64File(dto.CoverImageUrl, imageName, "imageurl", cancellationToken);
                }

                var url = "/api/v2/news/createNewsArticleById";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.V2Content.ContentServiceAppId, url);
                request.Content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<string>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                       ?? throw new Exception("Empty or invalid response from content service.");
            }
            catch(InvalidDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating news article via external service");
                throw;
            }
        }

        public async Task<PagedResponse<V2NewsArticleDTO>> GetAllNewsArticlesAsync(
                    int? page, int? perPage, string? search, CancellationToken cancellationToken = default)
        {
            var currentPage = Math.Max(1, page ?? 1);
            var pageSize = Math.Clamp(perPage ?? 12, 1, 100);

            var results = await _search.SearchRawAsync<ContentNewsIndex>(
                ConstantValues.IndexNames.ContentNewsIndex,
                new RawSearchRequest
                {
                    Filter = "IsActive eq true",
                    OrderBy = "PublishedDate desc, CreatedAt desc",
                    Top = pageSize,
                    Skip = (currentPage - 1) * pageSize,
                    Text = string.IsNullOrWhiteSpace(search) ? "*" : search!,
                    IncludeTotalCount = true
                },
                cancellationToken
            );

            var items = results.Items.Select(MapIndexToDto).ToList();

            return new PagedResponse<V2NewsArticleDTO>
            {
                Page = currentPage,
                PerPage = pageSize,
                TotalCount = (int)results.TotalCount,   // <-- use total from Azure Search
                Items = items
            };
        }
        public async Task<List<V2NewsArticleDTO>> GetArticlesByCategoryIdAsync(int categoryId, CancellationToken cancellationToken)
        {
            var results = await _search.SearchRawAsync<ContentNewsIndex>(
                ConstantValues.IndexNames.ContentNewsIndex,
                new RawSearchRequest
                {
                    Filter = $"IsActive eq true and Categories/any(c: c/CategoryId eq {categoryId})",
                    OrderBy = "PublishedDate desc, CreatedAt desc",
                    Top = 100,
                    Skip = 0,
                    Text = "*",
                    IncludeTotalCount = false
                },
                cancellationToken
            );

            return results.Items.Select(MapIndexToDto).ToList();
        }

        public async Task<List<V2NewsArticleDTO>> GetArticlesBySubCategoryIdAsync(
            int categoryId,
            int subCategoryId,
            ArticleStatus status,
            string? searchText,
            int? page,
            int? pageSize,
            CancellationToken cancellationToken)
        {
            string slotPredicate = status switch
            {
                ArticleStatus.Published => "c/SlotId eq 14",
                ArticleStatus.Unpublished => "c/SlotId eq 15",
                ArticleStatus.Live => "c/SlotId ge 1 and c/SlotId le 13",
                _ => "c/SlotId ge 1"
            };

            var p = Math.Max(1, page ?? 1);
            var ps = Math.Clamp(pageSize ?? 50, 1, 200);

            var results = await _search.SearchRawAsync<ContentNewsIndex>(
                ConstantValues.IndexNames.ContentNewsIndex,
                new RawSearchRequest
                {
                    Filter = $"IsActive eq true and Categories/any(c: c/CategoryId eq {categoryId} and c/SubcategoryId eq {subCategoryId} and {slotPredicate})",
                    // CASE WHEN isn’t supported in Azure Search order by; use your computed fields if needed.
                    OrderBy = "PublishedDate desc, CreatedAt desc",
                    Top = ps,
                    Skip = (p - 1) * ps,
                    Text = string.IsNullOrWhiteSpace(searchText) ? "*" : searchText!,
                    IncludeTotalCount = false
                },
                cancellationToken
            );

            var dtos = results.Items.Select(MapIndexToDto).ToList();
            foreach (var dto in dtos)
            {
                dto.Categories = dto.Categories
                    .Where(c => c.CategoryId == categoryId && c.SubcategoryId == subCategoryId)
                    .ToList();
            }
            return dtos;
        }

        public async Task<V2NewsArticleDTO?> GetArticleByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            try
            {
                var doc = await _search.GetByIdAsync<ContentNewsIndex>(ConstantValues.IndexNames.ContentNewsIndex, id.ToString());
                return doc is null ? null : MapIndexToDto(doc);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading article by id {Id} from Azure Search", id);
                throw;
            }
        }

        public async Task<V2NewsArticleDTO?> GetArticleBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            try
            {
                var req = new CommonSearchRequest
                {
                    Text = "*",
                    PageNumber = 1,
                    PageSize = 1,
                    Filters = new Dictionary<string, object>
                    {
                        ["IsActive"] = true,
                        ["Slug"] = slug
                    }
                };

                var res = await _search.SearchAsync(ConstantValues.IndexNames.ContentNewsIndex, req);
                var doc = res.ContentNewsItems?.FirstOrDefault();
                return doc is null ? null : MapIndexToDto(doc);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading article by slug {Slug} from Azure Search", slug);
                throw;
            }
        }

        public async Task<List<V2NewsArticleDTO>> GetAllNewsFilterArticles(bool? isActive, CancellationToken cancellationToken = default)
        {
            try
            {
                var filters = new Dictionary<string, object>();
                if (isActive.HasValue) filters["IsActive"] = isActive.Value;

                var req = new CommonSearchRequest
                {
                    Text = "*",
                    PageNumber = 1,
                    PageSize = 1000,
                    OrderBy = "PublishedDate desc",
                    Filters = filters
                };

                var res = await _search.GetAllAsync(ConstantValues.IndexNames.ContentNewsIndex, req);
                return (res.ContentNewsItems ?? new List<ContentNewsIndex>()).Select(MapIndexToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading filtered news list from Azure Search");
                throw;
            }
        }
        public async Task<string> UpdateNewsArticleAsync(V2NewsArticleDTO dto, CancellationToken cancellationToken)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(dto.CoverImageUrl) &&
                    !dto.CoverImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    var imageName = $"{dto.Title}_{dto.Id}.png";
                    var blobUrl = await _blobStorage.SaveBase64File(dto.CoverImageUrl, imageName, "imageurl", cancellationToken);
                    dto.CoverImageUrl = blobUrl;
                }

                var url = "/api/v2/news/updateNewsarticleByUserId";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, ConstantValues.V2Content.ContentServiceAppId, url);
                request.Content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    string detail = content;
                    try
                    {
                        var problem = JsonSerializer.Deserialize<ProblemDetails>(content, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        detail = problem?.Detail ?? content;
                    }
                    catch
                    {
                        // fallback to raw content
                    }

                    if (response.StatusCode == HttpStatusCode.Conflict)
                        throw new InvalidOperationException(detail);

                    if (response.StatusCode == HttpStatusCode.NotFound)
                        return null;

                    throw new DaprServiceException((int)response.StatusCode, detail);
                }

                return content;
            }
            catch (InvocationException ex)
            {
                var status = ex.Response?.StatusCode ?? HttpStatusCode.BadGateway;
                string body = ex.Response?.Content is { }
                    ? await ex.Response.Content.ReadAsStringAsync()
                    : ex.Message;

                string detail;
                try
                {
                    var pd = JsonSerializer.Deserialize<ProblemDetails>(body,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    detail = pd?.Detail ?? body;
                }
                catch
                {
                    detail = body;
                }

                if (status == HttpStatusCode.Conflict)
                {
                    throw new InvalidOperationException(detail);
                }
                else if (status == HttpStatusCode.NotFound)
                {
                    return null;
                }

                throw new DaprServiceException((int)status, detail);
            }
        }

        public async Task<string> DeleteTagName(Guid id, CancellationToken cancellationToken = default)
        {
            var url = $"/api/v2/news/tag/{id}";
            try
            {
                return await _dapr.InvokeMethodAsync<string>(
                    HttpMethod.Delete,
                    V2Content.ContentServiceAppId,
                    url,
                    cancellationToken: cancellationToken
                );
            }
            catch (InvocationException ex)
            {
                var status = ex.Response?.StatusCode ?? HttpStatusCode.BadGateway;
                string body = ex.Response?.Content is { }
                    ? await ex.Response.Content.ReadAsStringAsync()
                    : ex.Message;

                string detail;
                try
                {
                    var pd = JsonSerializer.Deserialize<ProblemDetails>(body,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    detail = pd?.Detail ?? body;
                }
                catch
                {
                    detail = body;
                }

                if (status == HttpStatusCode.NotFound)
                {
                    return null;
                }
                else if (status == HttpStatusCode.Conflict)
                {
                    throw new InvalidOperationException(detail);
                }
                else
                {
                    throw new DaprServiceException((int)status, detail);
                }
            }
        }

        public async Task<string> DeleteNews(Guid id, CancellationToken cancellationToken = default)
        {
            var url = $"/api/v2/news/news/{id}";
            try
            {
                return await _dapr.InvokeMethodAsync<string>(
                    HttpMethod.Delete,
                    V2Content.ContentServiceAppId,
                    url,
                    cancellationToken: cancellationToken
                );
            }
            catch (InvocationException ex)
            {
                var status = ex.Response?.StatusCode ?? HttpStatusCode.BadGateway;
                string body = ex.Response?.Content is { }
                    ? await ex.Response.Content.ReadAsStringAsync()
                    : ex.Message;

                string detail;
                try
                {
                    var pd = JsonSerializer.Deserialize<ProblemDetails>(body,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    detail = pd?.Detail ?? body;
                }
                catch
                {
                    detail = body;
                }

                if (status == HttpStatusCode.NotFound)
                {
                    return null;
                }
                else if (status == HttpStatusCode.Conflict)
                {
                    throw new InvalidOperationException(detail);
                }
                else
                {
                    throw new DaprServiceException((int)status, detail);
                }
            }
        }
            public async Task<string> ReorderSlotsAsync(NewsSlotReorderRequest dto, CancellationToken cancellationToken)
            {
            try
            {
                var url = "/api/v2/news/reorderLiveSlotsByUserId";
                var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.V2Content.ContentServiceAppId, url);
                request.Content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");

                var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply slot rearrangement via external service");
                throw;
            }
        }

        public async Task AddCategoryAsync(V2NewsCategory category, CancellationToken cancellationToken = default)
        {
            var url = "/api/v2/news/category/createById";
            var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Post, ConstantValues.V2Content.ContentServiceAppId, url);
            request.Content = new StringContent(JsonSerializer.Serialize(category), Encoding.UTF8, "application/json");

            var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        public async Task<List<V2NewsCategory>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
        {
            var url = "/api/v2/news/allcategories";
            var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Get, ConstantValues.V2Content.ContentServiceAppId, url);

            var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<V2NewsCategory>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        }

        public async Task<V2NewsCategory?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var url = $"/api/v2/news/categorygetbyid/{id}";
            var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Get, ConstantValues.V2Content.ContentServiceAppId, url);

            var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<V2NewsCategory>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<bool> UpdateSubCategoryAsync(int categoryId, V2NewsSubCategory updatedSubCategory, CancellationToken cancellationToken = default)
        {
            var url = $"/api/v2/news/category/subcategorybyid?categoryId={categoryId}";
            var request = _dapr.CreateInvokeMethodRequest(HttpMethod.Put, ConstantValues.V2Content.ContentServiceAppId, url);
            request.Content = new StringContent(JsonSerializer.Serialize(updatedSubCategory), Encoding.UTF8, "application/json");

            var response = await _dapr.InvokeMethodWithResponseAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return false;

            response.EnsureSuccessStatusCode();
            return true;
        }
        //comments


        public async Task<NewsCommentApiResponse> SaveNewsCommentAsync(V2NewsCommentDto dto, CancellationToken ct = default)
        {
            try
            {
                dto.CommentId = dto.CommentId == Guid.Empty ? Guid.NewGuid() : dto.CommentId;
                dto.CommentedAt = dto.CommentedAt == default ? DateTime.UtcNow : dto.CommentedAt;

                var url = "/api/v2/news/commentsavebyid"; // This should match the internal endpoint route
                var request = _dapr.CreateInvokeMethodRequest(
                    HttpMethod.Post,
                    V2Content.ContentServiceAppId,
                    url
                );

                request.Content = new StringContent(
                    JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _dapr.InvokeMethodWithResponseAsync(request, ct);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<NewsCommentApiResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new NewsCommentApiResponse
                {
                    Status = "failed",
                    Message = "Empty response from internal service."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving news comment via external service");

                return new NewsCommentApiResponse
                {
                    Status = "failed",
                    Message = "Failed to save news comment"
                };
            }
        }

        public async Task<NewsCommentListResponse> GetCommentsByArticleIdAsync(string nid, int? page = null, int? perPage = null, CancellationToken ct = default)
        {
            try
            {
                var queryParams = new List<string>();
                if (page.HasValue) queryParams.Add($"page={page.Value}");
                if (perPage.HasValue) queryParams.Add($"perPage={perPage.Value}");

                var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
                var url = $"/api/v2/news/commentsbyArticleid/{nid}{queryString}";

                var response = await _dapr.InvokeMethodAsync<NewsCommentListResponse>(
                    HttpMethod.Get,
                    V2Content.ContentServiceAppId,
                    url,
                    ct);

                return response;
            }
            catch (InvocationException ex) when (ex.Response?.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("No comments or article found for Article ID: {Nid}", nid);
                throw new KeyNotFoundException($"No article or comment index found for article {nid}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch comments for Article ID: {Nid}", nid);
                throw new InvalidOperationException("Error retrieving comments for article.", ex);
            }
        }


        public async Task<bool> LikeNewsCommentAsync(string commentId, string userId, string userName, CancellationToken ct = default)
        {
            try
            {
                var encodedUserId = Uri.EscapeDataString(userId);
                var encodedUserName = Uri.EscapeDataString(userName);
                var url = $"/api/v2/news/commentsbyid/{commentId}?userId={encodedUserId}&userName={encodedUserName}";

                var request = _dapr.CreateInvokeMethodRequest(
                    HttpMethod.Post,
                    V2Content.ContentServiceAppId,
                    url
                );

                var response = await _dapr.InvokeMethodWithResponseAsync(request, ct);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<bool>(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to like comment {CommentId} by user {UserId}", commentId, userId);
                throw new InvalidOperationException("Like (by user ID) failed", ex);
            }
        }
            
        public async Task<NewsCommentApiResponse> SoftDeleteNewsCommentAsync(string articleId, Guid commentId, string userId, CancellationToken ct = default)
        {
            try
            {
                var encodedUserId = Uri.EscapeDataString(userId);
                var url = $"/api/v2/news/comments/delete/byid/{articleId}/{commentId}?userId={encodedUserId}";

                var request = _dapr.CreateInvokeMethodRequest(
                    HttpMethod.Post,
                    V2Content.ContentServiceAppId,
                    url
                );

                var response = await _dapr.InvokeMethodWithResponseAsync(request, ct);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<NewsCommentApiResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new NewsCommentApiResponse
                {
                    Status = "failed",
                    Message = "Empty response received from delete call."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to soft delete comment {CommentId} for article {ArticleId} by user {UserId}", commentId, articleId, userId);

                return new NewsCommentApiResponse
                {
                    Status = "failed",
                    Message = "Soft delete request failed"
                };
            }
        }
        private async Task<List<ContentNewsIndex>> FetchSinglePerSlotRange(
                    int categoryId, int subCategoryId, int start, int end, CancellationToken cancellationToken)
        {
            string CatSlotFilter(int catId, int subId, int slot) =>
                $"IsActive eq true and Categories/any(c: c/CategoryId eq {catId} and c/SubcategoryId eq {subId} and c/SlotId eq {slot})";

            var acc = new List<ContentNewsIndex>();
            for (int s = start; s <= end; s++)
            {
                var res = await _search.SearchRawAsync<ContentNewsIndex>(
                    ConstantValues.IndexNames.ContentNewsIndex,
                    new RawSearchRequest
                    {
                        Filter = CatSlotFilter(categoryId, subCategoryId, s),
                        OrderBy = "PublishedDate desc, CreatedAt desc",
                        Top = 3,
                        Skip = 0,
                        Text = "*",
                        IncludeTotalCount = false
                    },
                    cancellationToken
                );
                if (res.Items.Count > 0) acc.Add(res.Items[0]);
            }
            return acc;
        }

        public async Task<GenericNewsPageResponse> GetNewsLandingPageAsync(
            int categoryId,
            int subCategoryId,
            CancellationToken cancellationToken = default)
        {
            var categoryDto = await GetCategoryByIdAsync(categoryId, cancellationToken)
                ?? throw new KeyNotFoundException($"Category {categoryId} not found");
            var subDto = categoryDto.SubCategories?.FirstOrDefault(s => s.Id == subCategoryId)
                ?? throw new KeyNotFoundException($"SubCategory {subCategoryId} not found");

            string catKey = (categoryDto.CategoryName ?? $"cat_{categoryId}").ToLowerInvariant().Replace(" ", "_");
            string subKey = (subDto.SubCategoryName ?? $"sub_{subCategoryId}").ToLowerInvariant().Replace(" ", "_");
            string pageName = $"qln_{catKey}_{subKey}";

            var slot1to4 = await FetchSinglePerSlotRange(categoryId, subCategoryId, 1, 4, cancellationToken);
            var slot5to8 = await FetchSinglePerSlotRange(categoryId, subCategoryId, 5, 8, cancellationToken);
            var slot9to13 = await FetchSinglePerSlotRange(categoryId, subCategoryId, 9, 13, cancellationToken);

            string CatSlotFilter(int catId, int subId, int slot) =>
                $"IsActive eq true and Categories/any(c: c/CategoryId eq {catId} and c/SubcategoryId eq {subId} and c/SlotId eq {slot})";

            var slot14Filter = CatSlotFilter(categoryId, subCategoryId, 14);

            var slot14Top4 = await _search.SearchRawAsync<ContentNewsIndex>(
                ConstantValues.IndexNames.ContentNewsIndex,
                new RawSearchRequest
                {
                    Filter = slot14Filter,
                    OrderBy = "PublishedDate desc, CreatedAt desc",
                    Top = 4,
                    Skip = 0,
                    Text = "*",
                    IncludeTotalCount = false
                },
                cancellationToken
            );

            var slot14Rest = await _search.SearchRawAsync<ContentNewsIndex>(
                ConstantValues.IndexNames.ContentNewsIndex,
                new RawSearchRequest
                {
                    Filter = slot14Filter,
                    OrderBy = "PublishedDate desc, CreatedAt desc",
                    Top = 100,
                    Skip = 4,
                    Text = "*",
                    IncludeTotalCount = false
                },
                cancellationToken
            );

            List<Common.Infrastructure.DTO_s.ContentPost> MapToPosts(IEnumerable<ContentNewsIndex> list)
                => list.Select(i =>
                {
                    Guid.TryParse(i.Id, out var gid);
                    return new Common.Infrastructure.DTO_s.ContentPost
                    {
                        Id = gid,
                        Nid = i.Id,
                        DateCreated = (i.CreatedAt == default ? DateTime.UtcNow : i.CreatedAt).ToString("o"),
                        ImageUrl = i.CoverImageUrl,
                        UserName = i.authorName,
                        Title = i.Title,
                        WriterTag = i.WriterTag,
                        Description = i.Content,
                        Category = categoryDto.CategoryName,
                        NodeType = "post",
                        IsActive = i.IsActive,
                        Slug = i.Slug,
                        CreatedAt = i.CreatedAt,
                        UpdatedAt = i.UpdatedAt
                    };
                }).ToList();

            Common.Infrastructure.DTO_s.BaseQueueResponse<Common.Infrastructure.DTO_s.ContentPost> BuildQueue(
                string sectionKey, string label, IEnumerable<Common.Infrastructure.DTO_s.ContentPost> items)
            {
                var qName = $"{pageName}_{sectionKey}";
                var list = items.ToList();
                list.ForEach(p =>
                {
                    p.PageName = pageName;
                    p.QueueName = qName;
                    p.QueueLabel = label;
                });
                return new Common.Infrastructure.DTO_s.BaseQueueResponse<Common.Infrastructure.DTO_s.ContentPost>
                {
                    QueueLabel = label,
                    Items = list
                };
            }

            var page = new Common.Infrastructure.DTO_s.GenericNewsPage
            {
                TopStory = BuildQueue("top_story", "Top Story", MapToPosts(slot1to4)),
                MoreArticles = BuildQueue("more_articles", "More Articles", MapToPosts(slot5to8)),
                Articles1 = BuildQueue("articles_1", "Articles 1", MapToPosts(slot9to13)),
                Articles2 = BuildQueue("articles_2", "Articles 2", MapToPosts(slot14Rest.Items)),
                MostPopularArticles = BuildQueue("most_popular_articles", "Most Popular Articles", MapToPosts(slot14Top4.Items)),
                WatchOnQatarLiving = new Common.Infrastructure.DTO_s.BaseQueueResponse<Common.Infrastructure.DTO_s.ContentVideo>
                {
                    QueueLabel = "Watch on Qatar Living",
                    Items = Enumerable.Empty<Common.Infrastructure.DTO_s.ContentVideo>().ToList()
                }
            };

            return new GenericNewsPageResponse { News = page };
        }
        public async Task<NewsCommentApiResponse> EditNewsCommentAsync(string articleId, Guid commentId, string userId, string updatedText, CancellationToken ct = default)
        {
            try
            {
                var encodedUserId = Uri.EscapeDataString(userId);
                var url = $"/api/v2/news/comments/editbyid/{articleId}/{commentId}?userId={encodedUserId}";

                var request = _dapr.CreateInvokeMethodRequest(
                    HttpMethod.Post,
                    V2Content.ContentServiceAppId,
                    url
                );

                request.Content = new StringContent(
                    JsonSerializer.Serialize(updatedText),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _dapr.InvokeMethodWithResponseAsync(request, ct);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<NewsCommentApiResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new NewsCommentApiResponse
                {
                    Status = "failed",
                    Message = "Empty response received from edit call."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to edit comment {CommentId} for article {ArticleId} by user {UserId}", commentId, articleId, userId);

                return new NewsCommentApiResponse
                {
                    Status = "failed",
                    Message = "Edit request failed"
                };
            }
        }

        public Task<string> BulkMigrateNewsArticleAsync(List<V2NewsArticleDTO> articles, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> MigrateNewsArticleAsync(V2NewsArticleDTO article, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
