using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using QLN.ContentBO.WebUI.Models;
using MudBlazor;

namespace QLN.ContentBO.WebUI.Pages.Classified.Stores.ViewSubscriptionListing
{
    public partial class ViewSubscriptionListingBase : ComponentBase
    {
      
        protected List<ViewSubscriptionListingDto> Listings { get; set; } = new();
        [Inject] public IDialogService DialogService { get; set; }
        protected int currentPage = 1;
        protected int pageSize = 12;
        protected int TotalCount => Listings.Count;
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
        protected override void OnInitialized()
        {
            Listings = GetSampleData();
        }
        private List<ViewSubscriptionListingDto> GetSampleData()
        {
            DateTime commonDate = DateTime.Parse("2025-04-12");
            return new List<ViewSubscriptionListingDto>
            {
                new ViewSubscriptionListingDto
                {
                    OrderId = 21435,
                    SubscriptionType = "12 Months Plus",
                    UserName = "Rashid",
                    Email = "Rashid.r@gmail.com",
                    Mobile = "+974 5030537",
            Whatsapp = "+974 5030537",
            WebUrl = "Rashidr.com",
            Amount = 250,
            Status = "Active",
            StartDate = commonDate,
            EndDate = commonDate,
            WebLeads = 1,
            EmailLeads = 1,
            WhatsappLeads = 1,
            PhoneLeads = 2
        },
        new ViewSubscriptionListingDto
        {
            OrderId = 21435,
            SubscriptionType = "12 Months Super",
            UserName = "Rashid",
            Email = "Rashid.r@gmail.com",
            Mobile = "+974 5030537",
            Whatsapp = "+974 5030537",
            WebUrl = "Rashidr.com",
            Amount = 250,
            Status = "On Hold",
            StartDate = commonDate,
            EndDate = commonDate,
            WebLeads = 2,
            EmailLeads = 2,
            WhatsappLeads = 12,
            PhoneLeads = 2
        },
        new ViewSubscriptionListingDto
        {
            OrderId = 21342,
            SubscriptionType = "12 Months Super",
            UserName = "Rashid",
            Email = "Rashid.r@gmail.com",
            Mobile = "+974 5030537",
            Whatsapp = "+974 5030537",
            WebUrl = "Rashidr.com",
            Amount = 250,
            Status = "Active",
            StartDate = commonDate,
            EndDate = commonDate,
            WebLeads = 3,
            EmailLeads = 3,
            WhatsappLeads = 12,
            PhoneLeads = 1
        },
        new ViewSubscriptionListingDto
        {
            OrderId = 23415,
            SubscriptionType = "12 Months Super",
            UserName = "Rashid",
            Email = "Rashid.r@gmail.com",
            Mobile = "+974 5030537",
            Whatsapp = "+974 5030537",
            WebUrl = "Rashidr.com",
            Amount = 250,
            Status = "Cancelled",
            StartDate = commonDate,
            EndDate = commonDate,
            WebLeads = 4,
            EmailLeads = 4,
            WhatsappLeads = 2,
            PhoneLeads = 3
        }
    };
        }
        protected void EditOrder(ViewSubscriptionListingDto order)
        {
    
            // You can open a dialog or set a flag to show a UI panel
            Console.WriteLine($"Editing Order ID: {order.OrderId}");
        }
        protected void CancelOrder(ViewSubscriptionListingDto order)
        {
            order.Status = "Cancelled";
            Console.WriteLine($"Cancelled Order ID: {order.OrderId}");
        }
    }
}
