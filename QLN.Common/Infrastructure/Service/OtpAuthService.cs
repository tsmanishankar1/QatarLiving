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

        public Task<string> RequestOtpAsync(string email)
        {
            return _otpRepository.RequestOtpAsync(email);
        }

        public Task<string> VerifyOtpAsync(string otp)
        {
            return _otpRepository.VerifyOtpAsync(otp);
        }
    }

}
