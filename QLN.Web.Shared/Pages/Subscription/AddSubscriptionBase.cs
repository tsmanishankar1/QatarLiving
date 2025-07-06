using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using QLN.Web.Shared.Helpers;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services;
using QLN.Web.Shared.Services.Interface;
using System.ComponentModel;
using System.Text.Json;

namespace QLN.Web.Shared.Pages.Subscription
{
    public partial class AddSubscriptionBase : ComponentBase
    {
        [Inject] protected IJSRuntime _jsRuntime { get; set; }
        [Inject] protected ISnackbar Snackbar { get; set; }

        [Inject] protected HttpClient Http { get; set; } = default!;
        [Inject] protected ApiService Api { get; set; } = default!;
        [Inject] protected ISubscriptionService SubscriptionService { get; set; }


        protected MudForm _form;
        protected SubscriptionModel _subscriptionModel = new();


        protected bool _showSubCategory = false;
        protected bool _isLoading = false;



        protected void OnModelChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SubscriptionModel.VerticalType))
            {
                _showSubCategory = !string.IsNullOrEmpty(_subscriptionModel.VerticalType);
                _subscriptionModel.SubCategory = null;
                InvokeAsync(StateHasChanged);
            }
        }

        protected List<string> GetSubCategories() => _subscriptionModel.VerticalType switch
        {
            "Classified" => new() { "Deals", "Store", "Preloved" },
            "Properties" => new() { "Residential", "Commercial", "Vacation" },
            "Vehicles" => new() { "Cars", "Motorcycles", "Boats" },
            _ => new()
        };

        protected async Task SaveSubscription()
        {

            _isLoading = true;
            await _form.Validate();
            if (_form.IsValid)
            {
                Console.WriteLine(JsonSerializer.Serialize(_subscriptionModel));

                try
                {
                    var payload = new
                    {
                        _subscriptionModel.SubscriptionName,
                        _subscriptionModel.Price,
                        _subscriptionModel.Currency,
                        _subscriptionModel.Duration,
                        _subscriptionModel.VerticalType,
                        _subscriptionModel.SubCategory,
                        _subscriptionModel.Description
                    };
                    Console.WriteLine(JsonSerializer.Serialize(payload));
                    var success = await SubscriptionService.AddSubscriptionAsync(_subscriptionModel);
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
            else
            {
                _isLoading = false;
            }
        }
    }
}
