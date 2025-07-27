using Microsoft.AspNetCore.Components;
using QLN.ContentBO.WebUI.Components.ToggleTabs;
using MudBlazor;
using QLN.ContentBO.WebUI.Models;
using QLN.ContentBO.WebUI.Components.ConfirmationDialog;
using QLN.ContentBO.WebUI.Components.RejectVerificationDialog;

namespace QLN.ContentBO.WebUI.Pages.Services
{
    public partial class ViewSubscriptionListingTableBase : ComponentBase
    {
        [Inject] public IDialogService DialogService { get; set; }
        [Inject] public NavigationManager Navigation { get; set; }
        protected HashSet<SubscriptionOrder> SelectedListings { get; set; } = new();
        protected int currentPage = 1;
        protected int pageSize = 12;
        protected string selectedTab = "pendingApproval";
        protected int TotalCount => Listings.Count;
        protected List<SubscriptionOrder> Listings = new List<SubscriptionOrder>
{
    new SubscriptionOrder
    {
        AdId = 21435,
        OrderId = 123,
        SubscriptionPlan = "1 Month Plus",
        Username = "Rashid-khatar",
        Email = "Rashid.r@gmail.com",
        Mobile = "+974 5030537",
        Whatsapp = "+974 5030537",
        Amount = 250,
        Status = "Active",
        StartDate = DateTime.Parse("2025-04-12 00:00"),
        EndDate = DateTime.Parse("2025-05-12 00:00"),
        ImageUrl = "https://tse1.mm.bing.net/th/id/OIP.vKZ_FtQOoCj-6yxauiy7LgAAAA?rs=1&pid=ImgDetMain&o=7&rm=3"
    },
    new SubscriptionOrder
    {
        AdId = 21436,
        OrderId = 124,
        SubscriptionPlan = "3 Months Premium",
        Username = "Ali-jassim",
        Email = "ali.j@gmail.com",
        Mobile = "+974 5012345",
        Whatsapp = "+974 5012345",
        Amount = 600,
        Status = "Inactive",
        StartDate = DateTime.Parse("2025-01-10 00:00"),
        EndDate = DateTime.Parse("2025-04-10 00:00"),
        ImageUrl = "https://tse1.mm.bing.net/th/id/OIP.vKZ_FtQOoCj-6yxauiy7LgAAAA?rs=1&pid=ImgDetMain&o=7&rm=3"
    }
};
        protected List<ToggleTabs.TabOption> tabOptions = new()
        {
            new() { Label = "Pending Approval", Value = "pendingApproval" },
            new() { Label = "Published", Value = "published" },
            new() { Label = "Unpublished", Value = "unpublished" },
            new() { Label = "Promoted", Value = "promoted" },
            new() { Label = "Featured", Value = "featured" }
        };
        protected async Task OnTabChanged(string newTab)
        {
            selectedTab = newTab;

            int? status = newTab switch
            {
                "pendingApproval" => 1,
                "published" => 2,
                "unpublished" => 3,
                "Promoted" => 4,
                "featured" => 5,
                _ => null
            };

        }
        protected Task ApproveSelected() => Task.Run(() => Console.WriteLine("Approved Selected"));
        protected Task UnpublishSelected() => Task.Run(() => Console.WriteLine("Unpublished Selected"));
        protected Task PublishSelected() => Task.Run(() => Console.WriteLine("Published Selected"));
        protected Task RemoveSelected() => Task.Run(() => Console.WriteLine("Removed Selected"));
        protected Task UnpromoteSelected() => Task.Run(() => Console.WriteLine("Unpromoted Selected"));
        protected Task UnfeatureSelected() => Task.Run(() => Console.WriteLine("Unfeatured Selected"));

        protected Task Approve(SubscriptionOrder item) => Task.Run(() => Console.WriteLine($"Approved: {item.AdId}"));
        protected Task Publish(SubscriptionOrder item) => Task.Run(() => Console.WriteLine($"Published: {item.AdId}"));
        protected Task Unpublish(SubscriptionOrder item) => Task.Run(() => Console.WriteLine($"Unpublished: {item.AdId}"));
        protected Task OnRemove(SubscriptionOrder item) => Task.Run(() => Console.WriteLine($"Removed: {item.AdId}"));
        private void HandleRejection(string reason)
        {
            Console.WriteLine("Rejection Reason: " + reason);
            // Send to API or handle in state
        }
        protected Task RequestChanges(SubscriptionOrder item)
        {
            Console.WriteLine($"Requested changes for: {item.AdId}");
            OpenRejectDialog();
            return Task.CompletedTask;
        }
        private void OpenRejectDialog()
        {
            var parameters = new DialogParameters
            {
                { "Title", "Reject Verification" },
                { "Description", "Please enter a reason before rejecting" },
                { "ButtonTitle", "Reject" },
                { "OnRejected", EventCallback.Factory.Create<string>(this, HandleRejection) }
            };
            var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };
            var dialog = DialogService.Show<RejectVerificationDialog>("", parameters, options);
        }




        protected void HandlePageChange(int newPage)
        {
            currentPage = newPage;
            StateHasChanged();
        }

        protected void HandlePageSizeChange(int newPageSize)
        {
            pageSize = newPageSize;
            currentPage = 1;
            StateHasChanged();
        }
        public void OnEdit(SubscriptionOrder item)
        {
            Navigation.NavigateTo("/manage/services/editform");
        }
        public void OnPreview(SubscriptionOrder item)
        {
        }
        protected async Task ShowConfirmation(string title, string description, string buttonTitle, Func<Task> onConfirmedAction)
        {
            var parameters = new DialogParameters
        {
            { "Title", title },
            { "Descrption", description },
            { "ButtonTitle", buttonTitle },
            { "OnConfirmed", EventCallback.Factory.Create(this, onConfirmedAction) }
        };

            var options = new DialogOptions
            {
                CloseButton = false,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = DialogService.Show<ConfirmationDialog>("", parameters, options);
            var result = await dialog.Result;
        }
    }
}