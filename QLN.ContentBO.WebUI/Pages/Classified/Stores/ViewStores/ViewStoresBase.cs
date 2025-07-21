using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using QLN.ContentBO.WebUI.Models;
using MudBlazor;

namespace QLN.ContentBO.WebUI.Pages.Classified.Stores.ViewStores
{
    public partial class ViewStoresBase : ComponentBase
    {

        protected List<ViewStoreList> Listings { get; set; } = new();
        [Inject]
        public NavigationManager NavigationManager { get; set; } = default!;
        protected int currentPage = 1;
        protected int pageSize = 12;
        protected int TotalCount => Listings.Count;
        protected string SearchTerm { get; set; } = string.Empty;
        protected bool Ascending = true;
        protected override void OnInitialized()
        {
            Listings = GetSampleData();
        }

        private List<ViewStoreList> GetSampleData()
        {
            DateTime commonDate = DateTime.Parse("2025-04-12");

    return new List<ViewStoreList>
    {
        new ViewStoreList
        {
            OrderId = 21435,
            CompanyName = "Rashid",
            Email = "Rashid.r@gmail.com",
            Mobile = "+974 5030537",
            Whatsapp = "+974 5030537",
            WebUrl = "Rashidr.com",
            Status = "Active",
            StartDate = commonDate,
            EndDate = commonDate
        },
        new ViewStoreList
        {
            OrderId = 21435,
            CompanyName = "Rashid",
            Email = "Rashid.r@gmail.com",
            Mobile = "+974 5030537",
            Whatsapp = "+974 5030537",
            WebUrl = "Rashidr.com",
            Status = "On Hold",
            StartDate = commonDate,
            EndDate = commonDate
        },
        new ViewStoreList
        {
            OrderId = 21342,
            CompanyName = "Rashid",
            Email = "Rashid.r@gmail.com",
            Mobile = "+974 5030537",
            Whatsapp = "+974 5030537",
            WebUrl = "Rashidr.com",
            Status = "Active",
            StartDate = commonDate,
            EndDate = commonDate
        },
        new ViewStoreList
        {
            OrderId = 23415,
            CompanyName = "Rashid",
            Email = "Rashid.r@gmail.com",
            Mobile = "+974 5030537",
            Whatsapp = "+974 5030537",
            WebUrl = "Rashidr.com",
            Status = "Cancelled",
            StartDate = commonDate,
            EndDate = commonDate
        }
    };
        }


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
        protected void HandlePageChange(int newPage)
        {
            currentPage = newPage;
            StateHasChanged();
        }

        protected void HandlePageSizeChange(int newPageSize)
        {
            pageSize = newPageSize;
            currentPage = 1; // reset to first page
            StateHasChanged();
        }
      
        protected void OnViewClicked(ViewStoreList store)
        {
            var name = "Rashid";
            // NavigationManager.NavigateTo($"/manage/classified/stores/createform/{name}");
        }
      
    }
}
