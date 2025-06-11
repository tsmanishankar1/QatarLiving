using Azure.Search.Documents.Models;

namespace QLN.AIPOV.Frontend.ChatBot.Services.Interfaces
{
    public interface IAzureSearchService
    {
        Task<IEnumerable<SearchDocument>> SearchDocumentsAsync(string query, int top = 10);
        Task<IEnumerable<SearchDocument>> SearchDocumentsHybridAsync(string query, int top = 10);
        Task<string> GetIndexDefinitionAsync();
    }
}
