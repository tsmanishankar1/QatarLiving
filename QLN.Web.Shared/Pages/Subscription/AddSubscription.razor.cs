using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using QLN.Web.Shared.Helpers;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services;
using System.ComponentModel;
using System.Text.Json;

namespace QLN.Web.Shared.Pages.Subscription;

public partial class AddSubscription : ComponentBase
{
    [Inject] protected IJSRuntime _jsRuntime { get; set; }
    [Inject] private HttpClient Http { get; set; } = default!;
    [Inject] private ApiService Api { get; set; } = default!;


    private MudForm _form;
    private SubscriptionModel _model = new();
    private bool _showSubCategory = false;
    private string _authToken;
    private bool _isLoading = false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _model.PropertyChanged += OnModelChanged;
            _authToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
            if (string.IsNullOrWhiteSpace(_authToken))
            {
                _authToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjU0NTZhZTY0LTNjMGMtNDJjYS04MGIxLTBjOWQ2YjBkYmY5MiIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJqYXNyMjciLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9lbWFpbGFkZHJlc3MiOiJqYXN3YW50aC5yQGtyeXB0b3NpbmZvc3lzLmNvbSIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL21vYmlsZXBob25lIjoiKzkxOTAwMzczODEzOCIsIlVzZXJJZCI6IjU0NTZhZTY0LTNjMGMtNDJjYS04MGIxLTBjOWQ2YjBkYmY5MiIsIlVzZXJOYW1lIjoiamFzcjI3IiwiRW1haWwiOiJqYXN3YW50aC5yQGtyeXB0b3NpbmZvc3lzLmNvbSIsIlBob25lTnVtYmVyIjoiKzkxOTAwMzczODEzOCIsImV4cCI6MTc0NjY5NTE0NywiaXNzIjoiUWF0YXIgTGl2aW5nIiwiYXVkIjoiUWF0YXIgTGl2aW5nIn0.KYxgzCBr5io7jm9SDzh2GE7GADKZ38k3kivgx6gC3PQ";
            }

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

        _isLoading = true;
        await _form.Validate();
        if (_form.IsValid)
        {
            Console.WriteLine(JsonSerializer.Serialize(_model));
            Console.WriteLine("Auth Token: " + _authToken);

            try
            {
                var payload = new
                {
                    SubscriptionName = _model.SubscriptionName,
                    Price = _model.Price,
                    Currency = _model.Currency,
                    Duration = _model.Duration,
                    VerticalType = _model.VerticalType,
                    SubCategory = _model.SubCategory,
                    Description = _model.Description
                };
                Console.WriteLine(JsonSerializer.Serialize(payload));
                var response = await Api.PostAsync<object, object>("api/subscription/edit", payload, _authToken);
                Snackbar.Add("Subscription added!", Severity.Success);
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
