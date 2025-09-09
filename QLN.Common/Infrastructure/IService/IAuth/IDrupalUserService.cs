using QLN.Common.DTO_s;
using QLN.Common.DTO_s.ClassifiedsBo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IAuth
{
    public interface IDrupalUserService
    {
        Task<List<DrupalUserAutocompleteResponse>?> GetUserAutocompleteFromDrupalAsync(string searchQuery, CancellationToken cancellationToken = default);
        Task<DrupalUserCheckResponse?> GetUserInfoFromDrupalAsync(string email, CancellationToken cancellationToken = default);
    }
}
