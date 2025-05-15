using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using QLN.Web.Shared.Models;
using System.ComponentModel;
using System.Text.Json;

namespace QLN.Web.Shared.Pages.Subscription;

public partial class AddSubscription : ComponentBase
{
    [Inject] protected IJSRuntime _jsRuntime { get; set; }
    [Inject] private HttpClient Http { get; set; } = default!;


    private MudForm _form;
    private SubscriptionModel _model = new();
    private bool _showSubCategory = false;
    private string _authToken;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _model.PropertyChanged += OnModelChanged;
            _authToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");

        }

        await  base.OnAfterRenderAsync(firstRender);
    }

    private void OnModelChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SubscriptionModel.VerticalType))
        {
            _showSubCategory = !string.IsNullOrEmpty(_model.VerticalType);
            _model.SubCategory = null;
            InvokeAsync(StateHasChanged);
        }
    }

    private List<string> GetSubCategories() => _model.VerticalType switch
    {
        "Classified" => new() { "Deals", "Store", "Preloved" },
        "Properties" => new() { "Residential", "Commercial", "Vacation" },
        "Vehicles" => new() { "Cars", "Motorcycles", "Boats" },
        _ => new()
    };

    private async Task SaveSubscription()
    {
        await _form.Validate();
        if (_form.IsValid)
        {
            Console.WriteLine(JsonSerializer.Serialize(_model));
            Console.WriteLine("Auth Token: " + _authToken);

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "api/subscription/add")
                {
                    Content = new StringContent(JsonSerializer.Serialize(_model), System.Text.Encoding.UTF8, "application/json")
                };

                if (!string.IsNullOrEmpty(_authToken))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);
                }

                var response = await Http.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    Snackbar.Add("Subscription added successfully!", Severity.Success);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Snackbar.Add($"Error: {response.StatusCode} - {errorContent}", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add("Error occurred while saving subscription: " + ex.Message, Severity.Error);
            }
        }
    }
}
