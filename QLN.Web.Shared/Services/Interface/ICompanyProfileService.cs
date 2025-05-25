using Microsoft.AspNetCore.Components.Forms;
using QLN.Web.Shared.Models.QLN.Web.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Web.Shared.Services.Interface
{
    public interface ICompanyProfileService
    {
        Task<bool> CreateCompanyProfileAsync(
            CompanyModel model,
            IBrowserFile logoFile,
            IBrowserFile documentFile,
            string authToken);
    }
}
