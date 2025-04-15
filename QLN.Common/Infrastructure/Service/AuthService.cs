using QLN.Common.Indexing.IndexModels;
using QLN.Common.Infrastructure.InputModels;
using QLN.Common.Infrastructure.RepositoryInterface;
using QLN.Common.Infrastructure.ServiceInterface;

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
        public async Task<string> VerifyOtpAsync(AccountVerification request)
        {
            try
            {
                return await _repository.VerifyOtpAsync(request);
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
        public Task<LoginResponse> VerifyUserLogin(string name, string passwordOrOtp)
        {
            try
            {
                return _repository.VerifyUserLogin(name,passwordOrOtp);
            }
            catch
            {
                throw;
            }
        }
        public async Task<string> RefreshTokenAsync(string oldRefreshToken)
        {
            try
            {
                return await _repository.RefreshTokenAsync(oldRefreshToken);
            }
            catch
            {
                throw;
            }
        }
        public async Task<List<UserIndex>> SearchUsersFromIndexAsync(string? query)
        {
            try
            {
                return await _repository.SearchUsersFromIndexAsync(query);
            }
            catch
            {
                throw;
            }
            
        }
    }
}
