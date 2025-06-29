using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace QLN.Web.Shared.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            var response = await _httpClient.GetAsync($"{endpoint}");
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

        public async Task<string?> GetAsync(string endpoint)
        {
            var response = await _httpClient.GetAsync($"{endpoint}");
            

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            await HandleError(response);
            return "Unauthorized";
        }

        public async Task<string?> PostAsync(string endpoint)
        {
            var response = await _httpClient.PostAsync($"{endpoint}", null);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            await HandleError(response);
            return "Unauthorized";
        }


        public async Task<T?> GetAsyncWithToken<T>(string endpoint, string authToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{endpoint}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

            var response = await _httpClient.SendAsync(request);
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

        public async Task<T?> PostAsync<TRequest, T>(string endpoint, TRequest data)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{endpoint}")
            {
                Content = JsonContent.Create(data)
            };

            var response = await _httpClient.SendAsync(request);
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
            var request = new HttpRequestMessage(HttpMethod.Patch, $"{endpoint}")
            {
                Content = JsonContent.Create(data)
            };
            var response = await _httpClient.SendAsync(request);
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
            var response = await _httpClient.DeleteAsync($"{endpoint}");
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
            var request = new HttpRequestMessage(HttpMethod.Put, $"{endpoint}")
            {
                Content = JsonContent.Create(data)
            };
            var response = await _httpClient.SendAsync(request);
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
