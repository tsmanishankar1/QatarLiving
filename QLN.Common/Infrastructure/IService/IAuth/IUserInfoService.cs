using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IAuth
{
    public interface IUserInfoService
    {
        Task<UserQuotaCollection?> GetUserQuotaByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    }
}
