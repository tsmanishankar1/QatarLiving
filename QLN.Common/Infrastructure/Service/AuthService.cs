
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
            try
            {
                return await _repository.AddUserProfileAsync(request);
            }
            catch
            {
                throw;
            }
        }
        public Task<string> RequestOtp(string email)
        {
            try
            {
                return _repository.RequestOtp(email);
            }
            catch
            {
                throw;
            }
        }
        public Task<string> VerifyOtpWithToken(string email, string otp)
        {
            try
            {
                return _repository.VerifyOtpWithToken(email,otp);
            }
            catch
            {
                throw;
            }
        }
    }

}
