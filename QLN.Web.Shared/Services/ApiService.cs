using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using System.Text.Json;
using QLN.Web.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using Microsoft.JSInterop;

namespace QLN.Web.Shared.Services
{
    public class ApiService
    {
        private readonly HttpClient _http;
        private readonly IJSRuntime _jsRuntime;

        private readonly string _baseUrl;
        private readonly string _authToken;

        public ApiService(HttpClient http, IOptions<ApiSettings> options, IJSRuntime jsRuntime)
        {
            _http = http;
            _baseUrl = options.Value.BaseUrl.TrimEnd('/');
            _jsRuntime = jsRuntime;

        }
        private async Task<string?> GetTokenAsync()
        {
            return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
        }

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            var response = await _http.GetAsync($"{_baseUrl}/{endpoint}");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return JsonSerializer.Deserialize<T>(responseContent, jsonOptions);
            }
            await HandleError(response);
            return default!;
        }

        public async Task<T?> PostAsync<TRequest, T>(string endpoint, TRequest data, string? accessToken = null)
        {
            var token = await GetTokenAsync();

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/{endpoint}")
            {
                Content = JsonContent.Create(data)
            };

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }

            var response = await _http.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return await response.Content.ReadFromJsonAsync<T>(jsonOptions);
            }

            await HandleError(response);
            return default!;
        }


        public async Task<TResponse?> PatchAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            var request = new HttpRequestMessage(HttpMethod.Patch, $"{_baseUrl}/{endpoint}")
            {
                Content = JsonContent.Create(data)
            };
            var response = await _http.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return await response.Content.ReadFromJsonAsync<TResponse>(jsonOptions);
            }
                await HandleError(response);
                return default!;
        }

        public async Task<bool> DeleteAsync(string endpoint)
        {
            var response = await _http.DeleteAsync($"{_baseUrl}/{endpoint}");
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            await HandleError(response);
            return false;
        }

        public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl}/{endpoint}")
            {
                Content = JsonContent.Create(data)
            };
            var response = await _http.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return await response.Content.ReadFromJsonAsync<TResponse>(jsonOptions);
            }
                await HandleError(response);
                return default!;
        }

        private async Task HandleError(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            try
            {
                var problem = JsonSerializer.Deserialize<ProblemDetails>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var message = problem?.Detail ?? problem?.Title ?? "API Error";
                throw new HttpRequestException($"{(int)response.StatusCode} - {message}");
            }
            catch
            {
                throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
            }
        }
    }
}

public class ApiResponse<T>
{
    public T Data { get; set; }
    public int StatusCode { get; set; }
}
