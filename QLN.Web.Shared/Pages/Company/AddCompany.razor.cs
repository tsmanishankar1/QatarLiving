using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using MudBlazor;
using QLN.Web.Shared.Helpers;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Models.QLN.Web.Shared.Models;
using QLN.Web.Shared.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace QLN.Web.Shared.Pages.Company
{
    public  partial class AddCompany : ComponentBase
    {

        [Inject] protected IJSRuntime _jsRuntime { get; set; }

        [Inject] private HttpClient Http { get; set; } = default!;
        [Inject] private ApiService Api { get; set; } = default!;
        [Inject] private IOptions<ApiSettings> Options { get; set; }

        private IBrowserFile uploadedLogoFile;
        private IBrowserFile uploadedDocumentFile;
        private MudForm _form;
        private List<QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem> breadcrumbItems = new();
        private string logoPreviewUrl;
        private bool _isLoading = false;
        private string _authToken;
        private CompanyModel _model = new();
        private  string _baseUrl;

    
       
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _authToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
                if (string.IsNullOrWhiteSpace(_authToken))
                {
                    _authToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjU0NTZhZTY0LTNjMGMtNDJjYS04MGIxLTBjOWQ2YjBkYmY5MiIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJqYXNyMjciLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9lbWFpbGFkZHJlc3MiOiJqYXN3YW50aC5yQGtyeXB0b3NpbmZvc3lzLmNvbSIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL21vYmlsZXBob25lIjoiKzkxOTAwMzczODEzOCIsIlVzZXJJZCI6IjU0NTZhZTY0LTNjMGMtNDJjYS04MGIxLTBjOWQ2YjBkYmY5MiIsIlVzZXJOYW1lIjoiamFzcjI3IiwiRW1haWwiOiJqYXN3YW50aC5yQGtyeXB0b3NpbmZvc3lzLmNvbSIsIlBob25lTnVtYmVyIjoiKzkxOTAwMzczODEzOCIsImV4cCI6MTc0NjY5NTE0NywiaXNzIjoiUWF0YXIgTGl2aW5nIiwiYXVkIjoiUWF0YXIgTGl2aW5nIn0.KYxgzCBr5io7jm9SDzh2GE7GADKZ38k3kivgx6gC3PQ";
                }

            }

            await base.OnAfterRenderAsync(firstRender); 
        }

        private async Task TriggerFileUpload()
        {
            await _jsRuntime.InvokeVoidAsync("document.getElementById('doc-upload').click");
        }

        private void HandleDocumentUpload(InputFileChangeEventArgs e)
        {
            uploadedDocumentFile = e.File;
        }


        protected override void OnInitialized()
        {
            breadcrumbItems = new()
        {
            new() {   Label = "Classifieds",Url ="classifieds" },
             new() { Label = "Company", Url = "/company"},

            new() { Label = "Add Company", Url = "/add-company", IsLast = true },

        };
            _baseUrl = Options.Value.BaseUrl.TrimEnd('/');
            Console.WriteLine($"[ApiService] Base URL: {_baseUrl}");
        }
        private async Task HandleLogoUpload(InputFileChangeEventArgs e)
        {
            uploadedLogoFile = e.File;

            // Generate preview for display only (optional)
            var buffer = new byte[uploadedLogoFile.Size];
            await uploadedLogoFile.OpenReadStream().ReadAsync(buffer);
            var imageType = uploadedLogoFile.ContentType;
            logoPreviewUrl = $"data:{imageType};base64,{Convert.ToBase64String(buffer)}";
        }

        private async Task SubmitForm()
        {
            _isLoading = true;
            await _form.Validate();

            if (_form.IsValid)
            {
                Console.WriteLine("Auth Token: " + _authToken);

                using var formContent = new MultipartFormDataContent();

                // Add form fields (ensure all keys are strings)
                formContent.Add(new StringContent("3"), "VerticalId");
                formContent.Add(new StringContent(_model.BusinessName ?? ""), "BusinessName");
                formContent.Add(new StringContent(_model.Country ?? ""), "Country");
                formContent.Add(new StringContent(_model.City ?? ""), "City");
                formContent.Add(new StringContent(_model.BranchLocations ?? ""), "BranchLocations");
                formContent.Add(new StringContent(_model.PhoneNumber ?? ""), "PhoneNumber");
                formContent.Add(new StringContent(_model.WhatsAppNumber ?? ""), "WhatsAppNumber");
                formContent.Add(new StringContent(_model.Email ?? ""), "Email");
                formContent.Add(new StringContent(_model.WebsiteUrl ?? ""), "WebsiteUrl");
                formContent.Add(new StringContent(_model.FacebookUrl ?? ""), "FacebookUrl");
                formContent.Add(new StringContent(_model.InstagramUrl ?? ""), "InstagramUrl");
                formContent.Add(new StringContent(_model.StartDay ?? ""), "StartDay");
                formContent.Add(new StringContent(_model.EndDay ?? ""), "EndDay");
                formContent.Add(new StringContent(_model.StartHour ?? ""), "StartHour");
                formContent.Add(new StringContent(_model.EndHour ?? ""), "EndHour");
                formContent.Add(new StringContent(_model.NatureOfBusiness ?? ""), "NatureOfBusiness");
                formContent.Add(new StringContent(_model.CompanySize ?? ""), "CompanySize");
                formContent.Add(new StringContent(_model.CompanyType ?? ""), "CompanyType");
                formContent.Add(new StringContent(_model.UserDesignation ?? ""), "UserDesignation");
                formContent.Add(new StringContent(_model.BusinessDescription ?? ""), "BusinessDescription");
                formContent.Add(new StringContent(_model.CRNumber ?? ""), "CRNumber");
                formContent.Add(new StringContent(_model.CRDocumentPath ?? ""), "CRDocumentPath");

                // Add file if it exists
                if (uploadedLogoFile != null)
                {
                    var stream = uploadedLogoFile.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
                    var streamContent = new StreamContent(stream);
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(uploadedLogoFile.ContentType);
                    formContent.Add(streamContent, "Logo", uploadedLogoFile.Name);
                }
                if (uploadedDocumentFile != null)
                {
                    var docStream = uploadedDocumentFile.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
                    var docContent = new StreamContent(docStream);
                    docContent.Headers.ContentType = new MediaTypeHeaderValue(uploadedDocumentFile.ContentType);
                    formContent.Add(docContent, "Document", uploadedDocumentFile.Name);
                }


                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/companyprofile/create")
                    {
                        Content = formContent
                    };

                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);

                    var response = await Http.SendAsync(request); // Use the injected _http client

                    if (response.IsSuccessStatusCode)
                    {
                        Snackbar.Add("Business Profile created!", Severity.Success);
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine("Upload failed: " + errorContent);
                        Snackbar.Add("Failed to create business profile.", Severity.Error);
                    }
                }
                catch (HttpRequestException ex)
                {
                    HttpErrorHelper.HandleHttpException(ex, Snackbar);
                }
                finally
                {
                    _isLoading = false;
                }
            }
        }



    }

}

