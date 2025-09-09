using QLN.Web.Shared.Models;

namespace QLN.Web.Shared.Services.Interface
{
    public interface ICompanyProfileService
    {

        Task<CompanyProfileModel?> GetCompanyProfileAsync();
        Task<CompanyProfileModel?> GetCompanyProfileByIdAsync(string id);
        Task<bool> UpdateCompanyProfileAsync(CompanyProfileModel model);
        Task<bool> CreateCompanyProfileAsync(CompanyProfileModelDto model);


    }
}
