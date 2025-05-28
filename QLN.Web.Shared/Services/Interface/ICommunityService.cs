using QLN.Web.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Web.Shared.Services.Interface
{
    public interface ICommunityService
    {
        Task<IEnumerable<PostModel>> GetAllAsync();

    }
}
