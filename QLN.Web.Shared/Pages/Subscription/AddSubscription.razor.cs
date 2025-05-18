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
                var response = await Api.PostAsync<object, object>("api/subscription/add", payload, _authToken);
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
