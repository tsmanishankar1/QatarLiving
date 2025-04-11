using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.ServiceInterface
{
    public interface IOtpAuthService
    {
        Task<string> RequestOtp(string email);
        Task<string> VerifyOtpWithToken(string otp);
    }
}
