using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using System.Text.Json;
using QLN.Web.Shared.Models;

namespace QLN.Web.Shared.Services
{
    public class ApiService
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        public ApiService(HttpClient http, IOptions<ApiSettings> options)
        {
            _http = http ?? throw new ArgumentNullException(nameof(HttpClient));
            _baseUrl = options.Value.BaseUrl.TrimEnd('/') ?? "https://localhost:7761";
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

        

public async Task<ResponseModel<TData>?> PostAsync<TRequest, TData>(string endpoint, TRequest data)
{
    try
    {
        var response = await _http.PostAsJsonAsync($"{_baseUrl}/{endpoint}", data);
        var statusCode = (int)response.StatusCode;

        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Response Content: {responseContent}");

        if (!string.IsNullOrWhiteSpace(responseContent))
        {
            var result = JsonSerializer.Deserialize<ResponseModel<TData>>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result != null)
            {
                result.StatusCode = statusCode;
                return result;
            }
        }
        return default;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"POST Exception: {ex.Message}");
        return new ResponseModel<TData>
        {
            Status = false,
            Message = ex.Message,
            StatusCode = 0
        };
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

        public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl}/{endpoint}")
                {
                    Content = JsonContent.Create(data)
                };

                var response = await _http.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<TResponse>();
                }

                Console.WriteLine($"PUT Error: {response.StatusCode}");
                return default;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PUT Exception: {ex.Message}");
                return default;
            }
        }

    }
}

public class ApiResponse<T>
{
    public T Data { get; set; }
    public int StatusCode { get; set; }
}
