
using QLN.Common.Infrastructure.InputModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.RepositoryInterface
{
    public interface IAuthRepository
    {
        Task<string> AddUserProfileAsync(UserProfileCreateRequest request);
        Task<string> RequestOtp(string email);
        Task<string> VerifyOtpWithToken(string otp);
    }

}
