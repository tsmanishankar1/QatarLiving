
using QLN.Common.Infrastructure.InputModels;
using QLN.Common.Infrastructure.RepositoryInterface;
using QLN.Common.Infrastructure.ServiceInterface;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Service
{
    public class UserProfileService : IUserProfileService
    {
        private readonly IUserProfileRepository _repository;

        public UserProfileService(IUserProfileRepository repository)
        {
            _repository = repository;
        }

        public async Task<string> AddUserProfileAsync(UserProfileCreateRequest request)
        {
            return await _repository.AddUserProfileAsync(request);
        }
    }

}
