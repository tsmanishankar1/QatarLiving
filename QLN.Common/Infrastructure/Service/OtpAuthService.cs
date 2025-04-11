using QLN.Common.Infrastructure.RepositoryInterface;
using QLN.Common.Infrastructure.ServiceInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Service
{
    public class OtpAuthService : IOtpAuthService
    {
        private readonly IOtpRepository _otpRepository;

        public OtpAuthService(IOtpRepository otpRepository)
        {
            _otpRepository = otpRepository;
        }
        public Task<string> RequestOtp(string email)
        {
            return _otpRepository.RequestOtp(email);
        }
        public Task<string> VerifyOtpWithToken(string otp)
        {
            return _otpRepository.VerifyOtpWithToken(otp);
        }
    }
}
