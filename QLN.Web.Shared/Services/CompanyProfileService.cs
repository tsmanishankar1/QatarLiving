using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;
using MudBlazor;
using QLN.Web.Shared.Helpers;
using QLN.Web.Shared.Models.QLN.Web.Shared.Models;
using QLN.Web.Shared.Services.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace QLN.Web.Shared.Services
{
    public class CompanyProfileService : ICompanyProfileService
    {
        private readonly HttpClient _http;
        private readonly ISnackbar _snackbar;
        private readonly string _baseUrl;

        public CompanyProfileService(HttpClient http, ISnackbar snackbar, IOptions<ApiSettings> options)
        {
            _http = http;
            _snackbar = snackbar;
            _baseUrl = options.Value.BaseUrl.TrimEnd('/');
        }

        public async Task<bool> CreateCompanyProfileAsync(
     CompanyModel model,
     IBrowserFile logoFile,
     IBrowserFile documentFile,
     string authToken)
        {
            try
            {
                if (logoFile != null)
                {
                    var logoBuffer = new byte[logoFile.Size];
                    await logoFile.OpenReadStream(10 * 1024 * 1024).ReadAsync(logoBuffer);
                    model.LogoBase64 = $"data:{logoFile.ContentType};base64,{Convert.ToBase64String(logoBuffer)}";
                }

                if (documentFile != null)
                {
                    var docBuffer = new byte[documentFile.Size];
                    await documentFile.OpenReadStream(10 * 1024 * 1024).ReadAsync(docBuffer);
                    model.DocumentBase64 = $"data:{documentFile.ContentType};base64,{Convert.ToBase64String(docBuffer)}";
                }

                model.VerticalId = 3; // Make sure this is included in your model

                var json = System.Text.Json.JsonSerializer.Serialize(model);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/companyprofile/create")
                {
                    Content = content
                };

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

                var response = await _http.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    _snackbar.Add("Business Profile created!", Severity.Success);
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Upload failed: " + error);
                    _snackbar.Add("Failed to create business profile.", Severity.Error);
                    return false;
                }
            }
            catch (HttpRequestException ex)
            {
                HttpErrorHelper.HandleHttpException(ex, _snackbar);
                return false;
            }
        }

    }
}