using Microsoft.Extensions.Logging;
using MudBlazor;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services.Interface;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QLN.Web.Shared.Services
{
    public class CompanyProfileService : ServiceBase<CompanyProfileService>, ICompanyProfileService
    {
        private readonly HttpClient _httpClient;
        private readonly ISnackbar _snackbar;
        private readonly ILogger<CompanyProfileService> _logger;


        public CompanyProfileService(HttpClient httpClient, ISnackbar snackbar, ILogger<CompanyProfileService> logger) : base(httpClient)
        {
            _httpClient = httpClient;
            _snackbar = snackbar;
            _logger = logger;
        }


        public async Task<CompanyProfileModel?> GetCompanyProfileAsync()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "api/companyprofile/getByTokenUser");

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
                _logger.LogError("GetCompanyProfileAsync Exception: " + ex.Message);
                return null;
            }
        }

        public async Task<CompanyProfileModel?> GetCompanyProfileByIdAsync(string id)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"api/companyprofile/getById?id={id}");

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
                _logger.LogError("GetCompanyProfileByIdAsync Exception: " + ex.Message);
                return null;
            }
        }

        public async Task<bool> UpdateCompanyProfileAsync(CompanyProfileModel model)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            try
            {
                var json = JsonSerializer.Serialize(model, options);
                var request = new HttpRequestMessage(HttpMethod.Put, "api/companyprofile/update")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError("UpdateCompanyProfileAsync Exception: " + ex.Message);
                return false;
            }
        }

        public async Task<bool> CreateCompanyProfileAsync(CompanyProfileModelDto model)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull

            };


            try
            {
                var json = JsonSerializer.Serialize(model, options);
                var request = new HttpRequestMessage(HttpMethod.Post, "api/companyprofile/create")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                var response = await _httpClient.SendAsync(request);
                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    _snackbar.Add("Company profile could not be created as a company profile already exists for your account under this vertical.", Severity.Error);
                    return false;
                }
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    _snackbar.Add("Invalid company profile data. Please check the form and try again.", Severity.Error);
                    return false;
                }
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError("CreateCompanyProfileAsync Exception: " + ex.Message);
                return false;
            }
        }


    }
}