using Microsoft.AspNetCore.Components;
using MudBlazor;
using QLN.Web.Shared.Models;


namespace QLN.Web.Shared.Pages.Subscription
{
    public class SubscriptionListBase : ComponentBase
    {
        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;

        protected string SearchText { get; set; } = string.Empty;
        protected List<SubscriptionModel> AllData { get; set; } = new();
        protected List<SubscriptionModel> pagedData = new();

        protected override void OnInitialized()
        {
            // Mock 10 records
            AllData = Enumerable.Range(1, 10).Select(i => new SubscriptionModel
            {
                SubscriptionName = $"Plan {i}",
                Price = i * 10,
                Currency = i % 2 == 0 ? "USD" : "QAR",
                Duration = i % 3 == 0 ? "6 Months" : "1 Month"
            }).ToList();
        }

        protected Task<TableData<SubscriptionModel>> LoadServerData(TableState state, CancellationToken cancellationToken)
        {
            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? AllData
                : AllData.Where(x => x.SubscriptionName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();

            var data = filtered
                .Skip(state.Page * state.PageSize)
                .Take(state.PageSize)
                .ToList();

            return Task.FromResult(new TableData<SubscriptionModel>
            {
                TotalItems = filtered.Count,
                Items = data
            });
        }


        protected void NavigateToAdd()
        {
            NavigationManager.NavigateTo("/subscription/add");
        }
    }
}