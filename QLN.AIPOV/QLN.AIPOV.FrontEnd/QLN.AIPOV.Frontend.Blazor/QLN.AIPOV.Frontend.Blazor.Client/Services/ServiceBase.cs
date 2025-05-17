using System.Net.Http.Json;

namespace QLN.AIPOV.Frontend.Blazor.Client.Services
{
    public class ServiceBase
    {
        protected readonly HttpClient HttpClient;

        protected ServiceBase(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        protected async Task<T?> GetAsync<T>(string uri)
        {
            return await HttpClient.GetFromJsonAsync<T>(uri);
        }

        protected async Task<TResult?> PostAsync<TRequest, TResult>(string uri, TRequest data)
        {
            var response = await HttpClient.PostAsJsonAsync(uri, data);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TResult>();
        }

        protected async Task<TResult?> PutAsync<TRequest, TResult>(string uri, TRequest data)
        {
            var response = await HttpClient.PutAsJsonAsync(uri, data);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TResult>();
        }

        protected async Task DeleteAsync(string uri)
        {
            var response = await HttpClient.DeleteAsync(uri);
            response.EnsureSuccessStatusCode();
        }
    }
}
