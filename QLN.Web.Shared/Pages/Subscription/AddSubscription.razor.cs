using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using QLN.Web.Shared.Models;
using System.ComponentModel;
using System.Text.Json;

namespace QLN.Web.Shared.Pages.Subscription;

public partial class AddSubscription : ComponentBase
{
    [Inject] protected IJSRuntime _jsRuntime { get; set; } = default!;


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

            Snackbar.Add("Subscription added!", Severity.Success);
        }
    }
}
