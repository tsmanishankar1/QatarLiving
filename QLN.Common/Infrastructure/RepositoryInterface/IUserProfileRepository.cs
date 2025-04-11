
using QLN.Common.Infrastructure.InputModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.RepositoryInterface
{
    public interface IUserProfileRepository
    {
        Task<string> AddUserProfileAsync(UserProfileCreateRequest request);
    }

}
