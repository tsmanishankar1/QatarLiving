
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
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _repository;

        public AuthService(IAuthRepository repository)
        {
            _repository = repository;
        }

        public async Task<string> AddUserProfileAsync(UserProfileCreateRequest request)
        {
            return await _repository.AddUserProfileAsync(request);
        }
        public Task<string> RequestOtp(string email)
        {
            return _repository.RequestOtp(email);
        }
        public Task<string> VerifyOtpWithToken(string otp)
        {
            return _repository.VerifyOtpWithToken(otp);
        }
    }

}
