using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using MudBlazor;
using QLN.Web.Shared.Helpers;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services.Interface;
using System.Text.Json;

namespace QLN.Web.Shared.Pages.Subscription
{
    public class PurchaseRefreshBase : ComponentBase
    {
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] protected ISubscriptionService SubscriptionService { get; set; }

        protected MudForm _form;
        protected bool _isLoading = false;

        protected bool HasError { get; set; } = false;


        protected List<PayToPublishPlan> _plans = new();
        protected bool IsPayPlansLoading = false;

        protected bool _isPaymentDialogOpen = false;
        protected bool _actionSucess = false;
        protected PayToPublishPlan? _selectedPlan;
        protected PaymentRequestModel _model = new();

        protected int _selectedQuantity = 1000;
        protected decimal _selectedPrice => _priceMap.ContainsKey(_selectedQuantity) ? _priceMap[_selectedQuantity] : 0;

        protected List<int> _quantities = new() { 500, 1000, 2000, 5000 };
        protected Dictionary<int, decimal> _priceMap = new()
    {
        { 500, 400 },
        { 1000, 800 },
        { 2000, 1500 },
        { 5000, 3500 }
    };

        protected override async void OnInitialized()
        {
            InitializeBreadcrumbs();
            await LoadSubscriptionPlansFromApi(3, 1);

        }


        protected List<QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem> breadcrumbItems = new();

        public string Name { get; set; }
        public string Email { get; set; }



        protected void InitializeBreadcrumbs()
        {
            breadcrumbItems = new()
        {
            new() { Label = "Back to Dashboard", Url = "/qln/classified/dashboard/items" },
        };
        }


        protected async Task LoadSubscriptionPlansFromApi(int verticalId, int categoryId)
        {

            try
            {
                IsPayPlansLoading = true;
                HasError = false;


                var response = await SubscriptionService.GetPayToPublishPlansAsync(verticalId, categoryId);

                if (response != null && response.Any())
                {
                    _plans = response;
                }
                else
                {
                    HasError = true;
                }

            }
            catch (HttpRequestException ex)
            {
                Snackbar.Add($"Error fetching plans: {ex.Message}", Severity.Error);
                HasError = true;
                IsPayPlansLoading = false;


            }
            finally
            {
                IsPayPlansLoading = false;
                StateHasChanged();
            }
        }



        protected void OpenPaymentDialog()
        {
            _isPaymentDialogOpen = true;
            StateHasChanged();
        }

        protected void CloseSuccessPopup()
        {
            _actionSucess = false;
            Navigation.NavigateTo("/qln/classified/dashboard/items");
        }

        protected void SelectPlan(PayToPublishPlan plan)
        {
            _selectedPlan = _plans.FirstOrDefault(p =>
                p.DurationName == plan.DurationName &&
                p.Price == plan.Price &&
                p.PlanName == plan.PlanName &&
                p.Id == plan.Id
                );
            if (_selectedPlan != null)
            {
                _selectedPlan.VerticalId = plan.VerticalId;
                _selectedPlan.VerticalName = plan.VerticalName;
                _selectedPlan.CategoryId = plan.CategoryId;
                _selectedPlan.CategoryName = plan.CategoryName;
            }
        }

        protected async Task PaymentSubmit()
        {

            _isLoading = true;
            await _form.Validate();
            if (_form.IsValid)
            {

                Console.WriteLine(JsonSerializer.Serialize(_model));

                try
                {
                    if (_selectedPlan == null)
                        return;

                    var payload = new
                    {
                        subscriptionId = _selectedPlan?.Id,
                        verticalId = _selectedPlan.VerticalId,
                        categoryId = _selectedPlan.CategoryId,
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
                    var response = await SubscriptionService.PurchaseSubscription(payload);
                    if (response)
                    {
                        Snackbar.Add("Subscription added!", Severity.Success);
                        _isPaymentDialogOpen = false;
                        _actionSucess = true;
                    }
                    else
                    {
                        Snackbar.Add("Failed to subscribe. Please try again.", Severity.Error);
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

        protected string CardStyle =>
            "padding: 16px; border-radius: 12px; background-color: white;";
    }
}
