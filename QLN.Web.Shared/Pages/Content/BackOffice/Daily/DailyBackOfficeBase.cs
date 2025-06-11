using Microsoft.AspNetCore.Components;


namespace QLN.Web.Shared.Pages.Content.BackOffice
{
    public class DailyBackOfficeBase : ComponentBase
    {
        public List<QLN.Web.Shared.Components.BreadCrumb.BreadcrumbItem> breadcrumbItems = new();
        protected async override Task OnInitializedAsync()
        {
             breadcrumbItems = new()
                {
                new() { Label = "Content", Url = $"/content/daily/backoffice", IsLast = true },
                };
        }
    };

    
}
