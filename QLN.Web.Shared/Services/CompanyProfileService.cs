using Microsoft.AspNetCore.Components.Forms;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Models.QLN.Web.Shared.Models;
using QLN.Web.Shared.Services.Interface;
using System.Text.Json;

namespace QLN.Web.Shared.Services
{
    public class CompanyProfileService : ServiceBase<CompanyProfileService>, ICompanyProfileService
    {
        private readonly HttpClient _httpClient;


        public CompanyProfileService(HttpClient httpClient) : base(httpClient)
        {
            _httpClient = httpClient;
        }
     

        public async Task<CompanyProfileModel?> GetCompanyProfileAsync(string authToken)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "api/companyprofile/getByTokenUser");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var list = JsonSerializer.Deserialize<List<CompanyProfileModel>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return list?.FirstOrDefault();
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetCompanyProfileAsync Exception: " + ex.Message);
                return null;
            }
        }

        public async Task<CompanyProfileModel?> GetCompanyProfileByIdAsync(string id, string authToken)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"api/companyprofile/getById?id={id}");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

                var response = await _httpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var model = JsonSerializer.Deserialize<CompanyProfileModel>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    return model;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetCompanyProfileByIdAsync Exception: " + ex.Message);
                return null;
            }
        }

        public async Task<bool> CreateCompanyProfileAsync(
     CompanyModel model,
     IBrowserFile logoFile,
     IBrowserFile documentFile,
     string authToken)
        {
            throw new NotImplementedException();
        }

    }
}