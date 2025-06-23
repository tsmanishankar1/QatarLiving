using Microsoft.AspNetCore.Components.Forms;
using QLN.Web.Shared.Models;
using QLN.Web.Shared.Models.QLN.Web.Shared.Models;

namespace QLN.Web.Shared.Services.Interface
{
    public interface ICompanyProfileService
    {
       
        Task<CompanyProfileModel?> GetCompanyProfileAsync(string authToken);
        Task<CompanyProfileModel?> GetCompanyProfileByIdAsync(string id, string authToken);
        Task<bool> UpdateCompanyProfileAsync(CompanyProfileModel model, string authToken);
        Task<bool> CreateCompanyProfileAsync(CompanyProfileModelDto model, string authToken);


    }
}
