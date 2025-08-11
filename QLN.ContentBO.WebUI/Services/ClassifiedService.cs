using Nextended.Core.Extensions;
using QLN.ContentBO.WebUI.Interfaces;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Services.Base;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using static QLN.Common.Infrastructure.Constants.ConstantValues;

namespace QLN.ContentBO.WebUI.Services
{
    public class ClassifiedService : ServiceBase<ClassifiedService>, IClassifiedService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        public ClassifiedService(HttpClient httpClient, ILogger<ClassifiedService> Logger)
            : base(httpClient, Logger)
        {
            _httpClient = httpClient;
            _logger = Logger;
        }
        public async Task<HttpResponseMessage?> GetAllCategoryTreesAsync(string vertical)
        {
            try
            {
                return await _httpClient.GetAsync($"/api/classified/category/{vertical}/all-trees");
            }
            catch (Exception ex)
            {
                _logger.LogError("GetAllCategoryTreesAsync Error: " + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> GetFeaturedSeasonalPicks(Vertical vertical)
        {
            try
            {
                return await _httpClient.GetAsync($"/api/v2/classifiedbo/seasonal-picks/slotted?vertical={vertical}");
            }
            catch (Exception ex)
            {
                _logger.LogError("GetFeaturedSeasonalPicks" + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> GetAllSeasonalPicks(Vertical vertical)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/v2/classifiedbo/getseasonalpicks?vertical={vertical}");
                return response;

            }
            catch (Exception ex)
            {
                _logger.LogError("GetAllSeasonalPicks" + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> CreateSeasonalPicksAsync(object payload)
        {
            try
            {
                var json = JsonSerializer.Serialize(payload);
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/v2/classifiedbo/seasonal-picks")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("CreateSeasonalPicksAsync: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }


        public async Task<HttpResponseMessage?> ReplaceSeasonalPickAsync(string pickId, int slot, Vertical vertical)
        {
            try
            {
                var payload = new
                {
                    pickId = pickId,
                    targetSlotId = slot,
                    vertical = vertical
                };

                var request = new HttpRequestMessage(HttpMethod.Put, "/api/v2/classifiedbo/seasonal-picks/replace-slot")
                {
                    Content = JsonContent.Create(payload)
                };

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("ReplaceSeasonalPickAsync: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> DeleteSeasonalPicks(string pickId, Vertical vertical)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Delete,
                    $"/api/v2/classifiedbo/seasonal-picks/soft-delete?pickId={pickId}&Vertical={vertical}");
                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteSeasonalPicks: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> ReorderSeasonalPicksAsync(IEnumerable<object> slotAssignments, Vertical vertical)
        {
            try
            {
                var payload = new
                {
                    slotAssignments = slotAssignments,
                    vertical = vertical
                };
                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
                var request = new HttpRequestMessage(HttpMethod.Put, "/api/v2/classifiedbo/seasonal-picks/reorder-slots")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("ReorderSeasonalPicksAsync: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> GetFeaturedCategory(Vertical vertical)
        {
            try
            {
                return await _httpClient.GetAsync($"/api/v2/classifiedbo/getslottedfeaturedcategory?vertical={vertical}");
            }
            catch (Exception ex)
            {
                _logger.LogError("GetFeaturedCategory" + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> GetAllFeatureCategory(Vertical vertical)
        {
            try
            {
                return await _httpClient.GetAsync($"/api/v2/classifiedbo/getfeaturedcategoriesbyvertical?vertical={vertical}");
            }
            catch (Exception ex)
            {
                _logger.LogError("GetAllFeatureCategory" + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> CreateFeaturedCategoryAsync(object payload)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(payload, options);
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/v2/classifiedbo/createfeaturedcategory")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("CreateFeaturedCategoryAsync: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> UpdateFeaturedCategoryAsync(object payload)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(payload, options);
                var request = new HttpRequestMessage(HttpMethod.Put, "/api/v2/classifiedbo/editfeaturedcategory")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("UpdateFeaturedCategoryAsync " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> UpdateSeasonalPicksAsync(object payload)
        { 
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(payload, options);
                var request = new HttpRequestMessage(HttpMethod.Put, "/api/v2/classifiedbo/editseasonalpick")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("UpdateSeasonalPicksAsync" + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
            
        }




        public async Task<HttpResponseMessage?> ReplaceFeaturedCategoryAsync(string pickId, int slot, Vertical vertical)
        {
            try
            {
                var body = new
                {
                    categoryId = pickId,
                    targetSlotId = slot,
                    vertical = vertical
                };

                var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                var request = new HttpRequestMessage(HttpMethod.Put, "/api/v2/classifiedbo/replacefeaturedcategoryslots")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("ReplaceFeaturedCategoryAsync: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> DeleteFeaturedCategory(string pickId, Vertical vertical)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Delete,
                    $"/api/v2/classifiedbo/featured-category-delete?categoryId={pickId}&vertical={vertical}");

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteFeaturedCategory: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> ReorderFeaturedCategoryAsync(IEnumerable<object> slotAssignments, Vertical vertical)
        {
            try
            {
                var payload = new
                {
                    slotAssignments = slotAssignments,
                    vertical = vertical
                };
                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
                var request = new HttpRequestMessage(HttpMethod.Put, "/api/v2/classifiedbo/featured-category/reorder-slots")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("ReorderFeaturedCategoryAsync: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        /// <summary>
        /// Searches classifieds by vertical using a common API pattern.
        /// </summary>
        /// <param name="vertical">The vertical segment of the API URL (e.g., "getall-items")</param>
        /// <param name="searchPayload">The request payload</param>
        /// <returns>A list of HttpResponseMessage</returns>
        public async Task<List<HttpResponseMessage>> SearchClassifiedsViewListingAsync(string vertical, object searchPayload)
        {
            var responses = new List<HttpResponseMessage>();

            try
            {
                var endpoint = $"/api/v2/classifiedbo/{vertical}";
                var response = await _httpClient.PostAsJsonAsync(endpoint, searchPayload);
                responses.Add(response);
                return responses;
            }
            catch (Exception ex)
            {
                responses.Add(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
                return responses;
            }
        }
        public async Task<List<HttpResponseMessage>> SearchClassifiedsViewTransactionAsync(object searchPayload)
        {
            var responses = new List<HttpResponseMessage>();

            try
            {
                var endpoint = $"/api/v2/classifiedbo/items/transactions";
                var response = await _httpClient.PostAsJsonAsync(endpoint, searchPayload);
                responses.Add(response);
                return responses;
            }
            catch (Exception ex)
            {
                responses.Add(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
                return responses;
            }
        }
        public async Task<HttpResponseMessage?> PerformBulkActionAsync(string vertical, object payload)
        {
            try
            {
                var json = JsonSerializer.Serialize(payload);
                var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v2/classifiedbo/{vertical}")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("PerformBulkActionAsync Error: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> GetPrelovedListingsAsync(FilterRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v2/classifiedbo/getall-preloved")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                return await _httpClient.SendAsync(httpRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError("GetPrelovedListingsAsync: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> GetPrelovedSubscription(FilterRequest request)
        {
            try
            {
                var query = $"?pageNumber={request.PageNumber}&pageSize={request.PageSize}";
                var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v2/classifiedbo/preloved-ads/payment-summary{query}");
                return await _httpClient.SendAsync(httpRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError("GetPrelovedSubscription: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> GetPrelovedP2pListing(FilterRequest request)
        {
            try
            {
                var url = "/api/v2/classifiedbo/getallprelovedads";

                var queryParams = new List<string>
        {
            $"pageNumber={request.PageNumber}",
            $"pageSize={request.PageSize}"
        };

                if (request.Status.HasValue)
                {
                    queryParams.Add($"status={request.Status.Value}");
                }

                if (!string.IsNullOrEmpty(request.SearchText))
                {
                    queryParams.Add($"search={Uri.EscapeDataString(request.SearchText)}");
                }

                if (request.CreationDate.HasValue)
                {
                    queryParams.Add($"creationDate={request.CreationDate.Value:yyyy-MM-dd}");
                }

                if (request.PublishedDate.HasValue)
                {
                    queryParams.Add($"datePublished={request.PublishedDate.Value:yyyy-MM-dd}");
                }
                if (request.IsPromoted == true)
                {
                    queryParams.Add("isPromoted=true");
                }

                if (request.IsFeatured == true)
                {
                    queryParams.Add("isFeatured=true");
                }


                //if (!string.IsNullOrEmpty(request.SortField))
                //{
                //    queryParams.Add($"sortField={request.SortField}");
                //}

                //if (!string.IsNullOrEmpty(request.SortDirection))
                //{
                //    queryParams.Add($"sortDirection={request.SortDirection}");
                //}

                if (queryParams.Count > 0)
                {
                    url += "?" + string.Join("&", queryParams);
                }

                var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
                return await _httpClient.SendAsync(httpRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError("GetPrelovedP2pListing: {Message}", ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> GetPrelovedUserListing(FilterRequest request)
        {
            try
            {
                var url = $"/api/companyprofile/getallcompanies?vertical={request.Vertical}&subVertical={request.SubVertical}";

                var queryParams = new List<string>
        {
            $"pageNumber={request.PageNumber}",
            $"pageSize={request.PageSize}"
        };

                if (request.Status.HasValue)
                {
                    queryParams.Add($"status={request.Status.Value}");
                }

                var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
                return await _httpClient.SendAsync(httpRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError("GetPrelovedP2pListing: {Message}", ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> PerformPrelovedBulkActionAsync(object payload)
        {
            try
            {
                var json = JsonSerializer.Serialize(payload);
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/v2/classifiedbo/bulk-preloved-action")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("PerformBulkActionAsync Error: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> GetAdByIdAsync(string vertical, string adId)
        {
            try
            {
                return await _httpClient.GetAsync($"/api/classified/{vertical}/{adId}");
            }
            catch (Exception ex)
            {
                _logger.LogError("GetAdByIdAsync Error: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> GetAllZonesAsync()
        {
            try
            {
                return await _httpClient.GetAsync("/api/v2/location/getAllZones");
            }
            catch (Exception ex)
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> GetAddressByDetailsAsync(int zone, int street, int building, string location)
        {
            try
            {
                var url = $"/api/v2/location/findAddress?zone={zone}&street={street}&building={building}&location={Uri.EscapeDataString(location)}";
                return await _httpClient.GetAsync(url);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetAddressByDetailsAsync Error: {ex}");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> PostAdAsync(string vertical, object payload)
        {
            try
            {
                var endpoint = $"/api/classified/{vertical}";

                // Create request manually with correct headers
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = JsonContent.Create(payload, options: new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = false
                    })
                };

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error body: {errorBody}");
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HttpRequestException: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.BadGateway);
            }
            catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("HTTP request timed out.");
                return new HttpResponseMessage(HttpStatusCode.RequestTimeout);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unhandled error in PostClassifiedItemAsync: {ex}");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> UpdateAdAsync(string vertical, object payload)
        {
            try
            {
                var endpoint = $"/api/classified/{vertical}/update";

                using var request = new HttpRequestMessage(HttpMethod.Put, endpoint)
                {
                    Content = JsonContent.Create(payload, options: new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = false
                    })
                };

                var response = await _httpClient.SendAsync(request);


                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error body: {errorBody}");
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HttpRequestException: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.BadGateway);
            }
            catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("HTTP request timed out.");
                return new HttpResponseMessage(HttpStatusCode.RequestTimeout);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unhandled error in UpdateAdAsync: {ex}");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> GetPrelovedP2pTransaction(FilterRequest request)
        {
            try
            {


                var query = $"?pageNumber={request.PageNumber}&pageSize={request.PageSize}";

                var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v2/classifiedbo/preloved/transactions{query}");

                return await _httpClient.SendAsync(httpRequest);

            }
            catch (Exception ex)
            {
                _logger.LogError("GetPrelovedP2pTransaction: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }


        public async Task<HttpResponseMessage?> UplodAsync(object payload)
        {
            try
            {
                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

                var request = new HttpRequestMessage(HttpMethod.Post, $"/files/upload")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                Console.WriteLine("PostAdAsync Request Payload:");
                Console.WriteLine(json);

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("PostAdAsync: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> RefreshAdAsync(string adId, int subVertical)
        {
            try
            {
                var url = $"/api/classified/items/refresh/{adId}?subVertical={subVertical}";
                //  _logger.LogInformation("Calling RefreshAd API at URL: {Url}", url);
                var response = await _httpClient.PostAsync(url, null); // No body required
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to refresh ad {adId}");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> PerformBulkActionAsync(object payload)
        {
            try
            {
                var json = JsonSerializer.Serialize(payload);
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/v2/classifiedbo/bulk-action")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("PerformBulkActionAsync Error: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> GetDealsSubscription(FilterRequest request)
        {
            try
            {

                var query = $"?pageNumber={request.PageNumber}&pageSize={request.PageSize}";

                var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v2/classifiedbo/getdealsSummary{query}");


                return await _httpClient.SendAsync(httpRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError("GetDealsSubscription: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> GetDealsListing(FilterRequest request)
        {
            try
            {
                var url = "/api/v2/classifiedbo/DealsViewSummary";

                var queryParams = new List<string>
        {
            $"pageNumber={request.PageNumber}",
            $"pageSize={request.PageSize}"
        };

                if (request.Status.HasValue)
                {
                    queryParams.Add($"status={request.Status.Value}");
                }

                if (!string.IsNullOrEmpty(request.SearchText))
                {
                    queryParams.Add($"search={Uri.EscapeDataString(request.SearchText)}");
                }

                if (request.CreationDate.HasValue)
                {
                    queryParams.Add($"creationDate={request.CreationDate.Value:yyyy-MM-dd}");
                }

                if (request.PublishedDate.HasValue)
                {
                    queryParams.Add($"datePublished={request.PublishedDate.Value:yyyy-MM-dd}");
                }
                if (request.IsPromoted == true)
                {
                    queryParams.Add("isPromoted=true");
                }

                if (request.IsFeatured == true)
                {
                    queryParams.Add("isFeatured=true");
                }


                //if (!string.IsNullOrEmpty(request.SortField))
                //{
                //    queryParams.Add($"sortField={request.SortField}");
                //}

                //if (!string.IsNullOrEmpty(request.SortDirection))
                //{
                //    queryParams.Add($"sortDirection={request.SortDirection}");
                //}

                if (queryParams.Count > 0)
                {
                    url += "?" + string.Join("&", queryParams);
                }

                var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
                return await _httpClient.SendAsync(httpRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError("GetPrelovedP2pListing: {Message}", ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<HttpResponseMessage?> PerformDealsBulkActionAsync(object payload)
        {
            try
            {
                var json = JsonSerializer.Serialize(payload);
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/v2/classifiedbo/getdealsSummary")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("PerformDealsBulkActionAsync Error: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> GetDealsByIdAsync(string vertical, string adId)
        {
            try
            {
                return await _httpClient.GetAsync($"/api/classified/deals/{adId}");
            }
            catch (Exception ex)
            {
                _logger.LogError("GetDealsByIdAsync Error: " + ex.Message);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> UpdateDealsAsync(object payload)
        {
            try
            {
                var endpoint = $"/api/classified/deals/update";

                using var request = new HttpRequestMessage(HttpMethod.Put, endpoint)
                {
                    Content = JsonContent.Create(payload, options: new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = false
                    })
                };

                var response = await _httpClient.SendAsync(request);

                Console.WriteLine($"Update response status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error body: {errorBody}");
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HttpRequestException: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.BadGateway);
            }
            catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("HTTP request timed out.");
                return new HttpResponseMessage(HttpStatusCode.RequestTimeout);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unhandled error in UpdateAdAsync: {ex}");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage> GetFeaturedCategoryById(string id)
        {
            try
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"/api/v2/classifiedbo/getfeaturedcategory?id={id}"
                );

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetFeaturedCategoryById");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage> GetSeasonalPicksById(string id)
        {
            try
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"/api/v2/classifiedbo/getseasonalpick?id={id}"
                );

                return await _httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GetSeasonalPicksById");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
    }
}
