using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using QLN.Web.Shared.Components.BreadCrumb;
using QLN.Web.Shared.Helpers;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services;
using static System.Net.WebRequestMethods;
using Microsoft.JSInterop;
using BreadcrumbItem = QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem;
using static QLN.Web.Shared.Pages.Subscription.SubscriptionDetails;

namespace QLN.Web.Shared.Pages.Subscription
{
    public partial class Subscription : ComponentBase
    {
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private ApiService Api { get; set; } = default!;
        [Inject] protected IJSRuntime _jsRuntime { get; set; }

        private MudForm _form;
        private string _authToken;
        private bool _isLoading = false;

        private bool _isError = false;
        private bool _isLoadingPlans = false;
        private int _activeTabIndex;
        private int _activeVerticalTabIndex = 2;

        private SubscriptionPlan? _selectedPlan;
        private bool _isPaymentDialogOpen = false;
        private bool _actionSucess = false;

        private PaymentRequestModel _model = new();


        protected override async void OnInitialized()
        {
            InitializeBreadcrumbs();
            //LoadSubscriptionPlans();
            await LoadSubscriptionPlansFromApi("3", "1");
        }
        private List<BreadcrumbItem> breadcrumbItems = new();
        private List<SubscriptionPlan> _plans = new();

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
        private void InitializeBreadcrumbs()
        {
            breadcrumbItems = new()
        {
            new() { Label = "Classifieds", Url = "classifieds" },
            new() { Label = "Subscriptions", Url = "/Subscriptions", IsLast = true },
        };
        }

        private void LoadSubscriptionPlans()
        {
            _plans = new List<SubscriptionPlan>
        {
            new() { Id="1",SubscriptionName = "1 flyer", Price = 50, Duration = "1 day" },
            new() {Id="2", SubscriptionName = "2 flyer", Price = 150, Duration = "1 Week" },
            new() { Id="3",SubscriptionName = "3 flyer", Price =250, Duration = "2 Week" },
            new() { Id="4",SubscriptionName = "4 flyer", Price = 1500, Duration = "1 Month" },
            new() { Id="5",SubscriptionName = "12 flyer", Price = 3000, Duration = "3 Months" },
            new() {Id="6", SubscriptionName = "24 flyer", Price = 6000, Duration = "6 Months" },
            new() {Id="7", SubscriptionName = "48 flyer", Price = 10000, Duration = "12 Months" },
        };
        }
        

        private async Task LoadSubscriptionPlansFromApi(string verticalId, string categoryId)
        {
            _isLoadingPlans = true;
            _isError = false;
            try
            {
                var url = $"api/getSubscription?verticalId={Uri.EscapeDataString(verticalId)}&categoryId={Uri.EscapeDataString(categoryId)}";

                var response = await Api.GetAsyncWithToken<SubscriptionResponse>(url,_authToken);

                if (response is not null && response.Subscriptions?.Any() == true)
                {
                    _plans = response.Subscriptions;
                }
                if (_plans == null || !_plans.Any())
                {
                    _isError = true;
                }

            }
            catch (HttpRequestException ex)
            {
                Snackbar.Add($"Error fetching plans: {ex.Message}", Severity.Error);
                _isError = true;
                _isLoadingPlans = false;


            }
            finally
            {
                _isLoadingPlans = false;
                StateHasChanged();
            }
        }


        private void SetVeritcalTab(int index)
        {
            _activeVerticalTabIndex = index;
        }

        private void SetTab(int index)
        {
            _activeTabIndex = index;
            _selectedPlan = null;
        }

        private IEnumerable<SubscriptionPlan> GetFilteredPlans()
        {
            return _activeTabIndex switch
            {
                0 => _plans.Take(3),
                1 => _plans.Skip(3).Take(1),
                2 => _plans.Skip(4),
                _ => _plans
            };
        }

        private void OpenPaymentDialog()
        {
            _isPaymentDialogOpen = true;
            StateHasChanged();
        }

        private void CloseSuccessPopup()
        {
            _actionSucess = false;
            Navigation.NavigateTo("/add-company");
        }

        private void SelectPlan(SubscriptionPlan plan)
        {
            _selectedPlan = _plans.FirstOrDefault(p =>
                p.Duration == plan.Duration &&
                p.Price == plan.Price &&
                p.SubscriptionName == plan.SubscriptionName &&
                p.Id ==plan.Id);
        }

        private async Task SubmitMockCardPayment()
        {
           
            _isLoading = true;
            await _form.Validate();
            if (_form.IsValid)
            {

                Console.WriteLine(JsonSerializer.Serialize(_model));
                Console.WriteLine("Auth Token: " + _authToken);

                try
                {
                    if (_selectedPlan == null)
                        return;

                    var payload = new
                    {
                        subscriptionId = _selectedPlan?.Id,
                        verticalId = 3,
                        subcategoryId = 1,
                        cardDetails = new
                        {
                            cardNumber = _model.CardNumber,
                            expiryMonth = _model.ExpiryMonth,
                            expiryYear = _model.ExpiryYear,
                            cvv = _model.CVV,
                            cardHolderName = _model.CardHolderName
                        }

                    };

                    Console.WriteLine(JsonSerializer.Serialize(payload));
                    var response = await Api.PostAsync<object, object>("api/payment/subscribe", payload, _authToken);
                    Snackbar.Add("Subscription added!", Severity.Success);
                    _isPaymentDialogOpen = false;

                    _actionSucess = true;

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

        private string CardStyle =>
            "padding: 16px; border-radius: 12px; background-color: white;";
    }
}
