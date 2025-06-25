using QLN.Web.Shared.Services.Interface;
using System.Net;
using System.Net.Http.Json;

namespace QLN.Web.Shared.Services
{
    public class ClassifiedsServices : IClassifiedsServices
    {
        private readonly HttpClient _httpClient;

        public ClassifiedsServices(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<HttpResponseMessage?> GetClassifiedsLPAsync()
        {
            try
            {
                return await _httpClient.GetAsync("/api/landing/classifieds");
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetClassifiedsLPAsync Error: " + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

        public async Task<List<HttpResponseMessage>> SearchClassifiedsAsync(object searchPayload)
        {
            var responses = new List<HttpResponseMessage>();

            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/classified/search", searchPayload);
                responses.Add(response);

                return responses;
            }
            catch (Exception ex)
            {
                Console.WriteLine("SearchClassifiedsAsync Error: " + ex);
                responses.Add(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
                return responses;
            }
        }
        /// <inheritdoc />
        public async Task<HttpResponseMessage?> GetClassifiedsByIdAsync(string ClassifiedId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/classified/{ClassifiedId}");
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetClassifiedsByIdAsync" + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
        public async Task<HttpResponseMessage?> GetAllCategoryTreesAsync(string vertical)
        {
            try
            {
                return await _httpClient.GetAsync($"/api/classified/category/{vertical}/all-trees");
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetAllCategoryTreesAsync Error: " + ex);
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }
            
            public async Task<HttpResponseMessage?> PostClassifiedItemAsync(string vertical, object payload)
            {
                try
                {
                    var endpoint = $"/api/classified/{vertical}/post";
                    var response = await _httpClient.PostAsJsonAsync(endpoint, payload);
                    return response;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"PostClassifiedItemAsync Error for {vertical}: " + ex);
                    return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                }
            }
       public async Task<HttpResponseMessage?> GetClassifiedWithSimilarAsync(string classifiedId, int similarPageSize)
        {
            try
            {
                var url = $"/api/classified/details/{classifiedId}?similarPageSize={similarPageSize}";
                var response = await _httpClient.GetAsync(url);
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetClassifiedWithSimilarAsync Error: {ex}");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }
        }

    }
}
