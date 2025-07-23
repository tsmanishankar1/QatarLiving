using Microsoft.AspNetCore.Components;

namespace QLN.ContentBO.WebUI.Pages.Classified.Collectibles.ViewTransactions
{
    public partial class ViewTransactionsBase : ComponentBase
    {
        protected string SearchTerm { get; set; } = string.Empty;
        protected bool Ascending = true;

        protected async Task HandleSearch(string searchTerm)
        {
            SearchTerm = searchTerm;
            Console.WriteLine($"Search triggered: {SearchTerm}");
            // Add logic to filter your listing data based on SearchTerm
        }

          protected async Task HandleSort(bool sortOption)
        {
            Ascending = sortOption;
            Console.WriteLine($"Sort triggered: {sortOption}");
            // Add logic to sort your listing data based on SortOption
        }
    }
}
