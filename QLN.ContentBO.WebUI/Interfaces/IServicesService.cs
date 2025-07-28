using QLN.ContentBO.WebUI.Models;
using static QLN.ContentBO.WebUI.Pages.NewsPage.NewsBase;
namespace QLN.ContentBO.WebUI.Interfaces
{
    public interface IServiceService
    {
        Task<HttpResponseMessage> GetServicesCategories();
    }
}
