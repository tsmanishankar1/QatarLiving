using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.ServiceInterface
{
    public interface IOtpAuthService
    {
        Task<string> RequestOtpAsync(string email);
        Task<string> VerifyOtpAsync(string otp);

    }

}
