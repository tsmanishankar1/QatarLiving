using Microsoft.AspNetCore.Components.Forms;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Models.QLN.Web.Shared.Models;

namespace QLN.Web.Shared.Services.Interface
{
    public interface ICompanyProfileService
    {
        Task<bool> CreateCompanyProfileAsync(
            CompanyModel model,
            IBrowserFile logoFile,
            IBrowserFile documentFile,
            string authToken);
        Task<CompanyProfileModel?> GetCompanyProfileAsync(string authToken);

    }
}
