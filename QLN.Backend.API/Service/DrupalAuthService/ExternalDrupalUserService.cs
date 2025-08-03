using QLN.Common.DTO_s.ClassifiedsBo;
using QLN.Common.Infrastructure.Constants;
using QLN.Common.Infrastructure.IService.IAuth;
using System.Net.Http;

namespace QLN.Backend.API.Service.DrupalAuthService
{
    public class ExternalDrupalUserService(HttpClient httpClient): IDrupalUserService
    {
        public async Task<List<DrupalUserAutocompleteResponse>?> GetUserAutocompleteFromDrupalAsync(string searchQuery, CancellationToken cancellationToken)
        {

            var requestUrl = $"{ConstantValues.AutocompleteUserPath}/{searchQuery}";

            var httpRequest = await httpClient.PostAsync(requestUrl, content: null, cancellationToken);

            if (httpRequest.IsSuccessStatusCode)
            {
                var response = await httpRequest.Content.ReadFromJsonAsync<List<DrupalUserAutocompleteResponse>>(cancellationToken);
                return response;
            }

            return null;
        }
    }
}
