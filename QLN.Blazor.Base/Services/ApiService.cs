using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace QLN.Blazor.Base.Services
{
    public class ApiService
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        public ApiService(HttpClient http, IOptions<ApiSettings> options)
        {
            _http = http;
            _baseUrl = options.Value.BaseUrl.TrimEnd('/');
        }

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            try
            {
                return await _http.GetFromJsonAsync<T>($"{_baseUrl}/{endpoint}");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"GET Error: {ex.Message}");
                return default;
            }
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
{
    try
    {
        var response = await _http.PostAsJsonAsync($"{_baseUrl}/{endpoint}", data);

        var responseContent = await response.Content.ReadAsStringAsync();

        if (!string.IsNullOrWhiteSpace(responseContent))
        {
            var result = JsonSerializer.Deserialize<TResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return result;
        }

        Console.WriteLine($"POST Error: {response.StatusCode}");
        return default;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"POST Exception: {ex.Message}");
        return default;
    }
}
        


        public async Task<TResponse?> PatchAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Patch, $"{_baseUrl}/{endpoint}")
                {
                    Content = JsonContent.Create(data)
                };

                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<TResponse>();
                }

                Console.WriteLine($"PATCH Error: {response.StatusCode}");
                return default;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PATCH Exception: {ex.Message}");
                return default;
            }
        }

        public async Task<bool> DeleteAsync(string endpoint)
        {
            try
            {
                var response = await _http.DeleteAsync($"{_baseUrl}/{endpoint}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DELETE Exception: {ex.Message}");
                return false;
            }
        }
    }
}
