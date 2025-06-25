using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using QLN.Web.Shared.Helpers;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Services;
using BreadcrumbItem = QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using QLN.Web.Shared.Services.Interface;

namespace QLN.Web.Shared.Pages.Subscription
{
    public partial class Subscription : ComponentBase
    {
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        [Inject] private NavigationManager Navigation { get; set; } = default!;
        [Inject] private CookieAuthStateProvider CookieAuthenticationStateProvider { get; set; } = default!;
        [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; }

        [Inject] protected IJSRuntime _jsRuntime { get; set; }
        [Inject] protected ISubscriptionService SubscriptionService { get; set; }

        private MudForm _form;
        private string _authToken;
        private bool _isLoading = false;

        protected bool IsLoading { get; set; } = true;
        protected bool HasError { get; set; } = false;
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
            await LoadSubscriptionPlansFromApi(3, 1);
           
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                var cookie = HttpContextAccessor.HttpContext?.Request.Cookies["qat"];
                _authToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6Ijk3NTQ1NGI1LTAxMmItNGQ1NC1iMTUyLWUzMGYzNmYzNjNlMiIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJNVUpBWSIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL2VtYWlsYWRkcmVzcyI6Im11amF5LmFAa3J5cHRvc2luZm9zeXMuY29tIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbW9iaWxlcGhvbmUiOiIrOTE3NzA4MjA0MDcxIiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjpbIkNvbXBhbnkiLCJTdWJzY3JpYmVyIl0sIlVzZXJJZCI6Ijk3NTQ1NGI1LTAxMmItNGQ1NC1iMTUyLWUzMGYzNmYzNjNlMiIsIlVzZXJOYW1lIjoiTVVKQVkiLCJFbWFpbCI6Im11amF5LmFAa3J5cHRvc2luZm9zeXMuY29tIiwiUGhvbmVOdW1iZXIiOiIrOTE3NzA4MjA0MDcxIiwiZXhwIjoxNzUwODUwOTg5LCJpc3MiOiJodHRwczovL3Rlc3QucWF0YXJsaXZpbmcuY29tIiwiYXVkIjoiaHR0cHM6Ly90ZXN0LnFhdGFybGl2aW5nLmNvbSJ9.QfCXZnvd6FFVYPi4vX7j9777WJ_lgkKhJSFPijKoCCg";

                Console.WriteLine("Access Token from cookie after render: " + _authToken);

                StateHasChanged(); 
            }
        }

        private List<BreadcrumbItem> breadcrumbItems = new();
        private List<SubscriptionPlan> _plans = new();

        public string Name { get; set; }
        public string Email { get; set; }

       

        private void InitializeBreadcrumbs()
        {
            breadcrumbItems = new()
        {
            new() { Label = "Classifieds", Url = "classifieds" },
            new() { Label = "Subscriptions", Url = "/Subscriptions", IsLast = true },
        };
        }

        private List<VerticalTab> _verticalTabs = new()
{
    new VerticalTab
    {
        Index = 0,
        VerticalId = 1,
        Label = "Properties",
        Icon = "/qln-images/subscription/Properties.svg",
        Categories = new()
        {
            new CategoryTab { Index = 1, Label = "Real Estate", Icon = "/qln-images/subscription/Stores.svg", CategoryId = 1 },
            new CategoryTab { Index = 2, Label = "International Real Estate", Icon = "/qln-images/subscription/Stores.svg", CategoryId = 2 },
           new CategoryTab { Index = 3, Label = "Hotel Estate", Icon = "/qln-images/subscription/Stores.svg", CategoryId = 3 },

        }
    },
    new VerticalTab
    {
        Index = 1,
        VerticalId = 2,
        Label = "Vehicles",
        Icon = "/qln-images/subscription/Vehicles.svg",
        Categories = new()
        {
            new CategoryTab { Index = 1, Label = "Vehicles", Icon = "/qln-images/subscription/Stores.svg", CategoryId = 1 }
,
        }
    },
    new VerticalTab
    {
        Index = 2,
        VerticalId = 3,
        Label = "Classified",
        Icon = "/qln-images/subscription/Classifieds.svg",
        Categories = new()
        {
            new CategoryTab { Index = 0, Label = "Deals", Icon = "/qln-images/subscription/Deals.svg", CategoryId = 1 },
            new CategoryTab { Index = 1, Label = "Stores", Icon = "/qln-images/subscription/Stores.svg", CategoryId = 2 },
            new CategoryTab { Index = 2, Label = "Preloved", Icon = "/qln-images/subscription/Preloved.svg", CategoryId = 3 },
        }
    },
 new VerticalTab
    {
        Index = 3,
        VerticalId = 4,
        Label = "Services",
        Icon = "/qln-images/subscription/Services.svg",
        Categories = new()
        {
            new CategoryTab { Index = 0, Label = "Services", Icon = "/qln-images/subscription/Deals.svg", CategoryId = 1 },
               }
    },
         new VerticalTab
    {
        Index = 4,
        VerticalId = 5,
        Label = "Jobs",
        Icon = "/qln-images/subscription/Jobs.svg",
        Categories = new()
        {
            new CategoryTab { Index = 0, Label = "Jobss", Icon = "/qln-images/subscription/Deals.svg", CategoryId = 301 },
        }
    },
         new VerticalTab
    {
        Index = 5,
        VerticalId = 6,
        Label = "Rewards",
        Icon = "/qln-images/subscription/Rewards.svg",
        Categories = new()
        {
  new CategoryTab { Index = 0, Label = "Yearly Subscription", Icon = "/qln-images/subscription/Deals.svg", CategoryId = 301 },
            new CategoryTab { Index = 1, Label = "À La Carte", Icon = "/qln-images/subscription/Stores.svg", CategoryId = 302 },
        }
    },};

        private async void SetVeritcalTab(int index)
        {
            _activeVerticalTabIndex = index;
            _activeTabIndex = 0;
            var selectedTab = _verticalTabs.FirstOrDefault(t => t.Index == index);
            if (selectedTab != null && selectedTab.Categories.Any())
            {
                await LoadSubscriptionPlansFromApi(selectedTab.VerticalId, selectedTab.Categories[0].CategoryId);
            }
        }
        private async void SetTab(int index)
        {
            _activeTabIndex = index;
            var selectedTab = _verticalTabs[_activeVerticalTabIndex];
            var selectedCategory = selectedTab.Categories.ElementAtOrDefault(index);
            if (selectedCategory != null)
            {
                await LoadSubscriptionPlansFromApi(selectedTab.VerticalId, selectedCategory.CategoryId);
            }
        }




        //private void LoadSubscriptionPlans()
        //{
        //    _plans = new List<SubscriptionPlan>
        //{
        //    new() { Id="1",SubscriptionName = "1 flyer", Price = 50, Duration = "1 day" },
        //    new() {Id="2", SubscriptionName = "2 flyer", Price = 150, Duration = "1 Week" },
        //    new() { Id="3",SubscriptionName = "3 flyer", Price =250, Duration = "2 Week" },
        //    new() { Id="4",SubscriptionName = "4 flyer", Price = 1500, Duration = "1 Month" },
        //    new() { Id="5",SubscriptionName = "12 flyer", Price = 3000, Duration = "3 Months" },
        //    new() {Id="6", SubscriptionName = "24 flyer", Price = 6000, Duration = "6 Months" },
        //    new() {Id="7", SubscriptionName = "48 flyer", Price = 10000, Duration = "12 Months" },
        //};
        //}


        private async Task LoadSubscriptionPlansFromApi(int verticalId, int categoryId)
        {

            try
            {
                IsLoading = true;
                HasError = false;


                var response = await SubscriptionService.GetSubscriptionAsync(verticalId, categoryId);

                if (response?.Subscriptions != null && response.Subscriptions.Any())
                {
                    //_plans = response.Subscriptions;
                    _plans = response.Subscriptions.Select(plan => new SubscriptionPlan
                    {
                        Id = plan.Id,
                        SubscriptionName = plan.SubscriptionName,
                        Price = plan.Price,
                        Currency = plan.Currency,
                        Duration = plan.Duration,
                        Description = plan.Description,
                        VerticalId = response.VerticalTypeId,
                        VerticalName = response.VerticalName,
                        CategoryId = response.CategoryId,
                        CategoryName = response.CategoryName
                    }).ToList();
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
                IsLoading = false;


            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }



        private void OpenPaymentDialog()
        {
            _isPaymentDialogOpen = true;
            StateHasChanged();
        }

        private void CloseSuccessPopup()
        {
            _actionSucess = false;
            Navigation.NavigateTo("/qln/dashboard/company/create");
        }

        private void SelectPlan(SubscriptionPlan plan)
        {
            _selectedPlan = _plans.FirstOrDefault(p =>
                p.Duration == plan.Duration &&
                p.Price == plan.Price &&
                p.SubscriptionName == plan.SubscriptionName &&
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

        private async Task PaymentSubmit()
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
                    var response = await SubscriptionService.PurchaseSubscription(payload, _authToken);
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

        private string CardStyle =>
            "padding: 16px; border-radius: 12px; background-color: white;";
    }
}
