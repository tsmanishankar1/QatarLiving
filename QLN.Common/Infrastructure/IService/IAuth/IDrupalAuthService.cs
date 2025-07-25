using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IAuth
{
    public interface IDrupalAuthService
    {
        Task<DrupalAuthResponse> LoginAsync(string username, string password, CancellationToken cancellationToken);
        
        Task LogoutAsync(CancellationToken cancellationToken);
    }
}
