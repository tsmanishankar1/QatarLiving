using QLN.Common.Infrastructure.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.AdService
{
    public interface IAdService
    {
        Task<IEnumerable<AdCategory>> GetAllAdCategory();
        Task<AdCategory> AddAdCategory(AdCategory category);
    }
}
