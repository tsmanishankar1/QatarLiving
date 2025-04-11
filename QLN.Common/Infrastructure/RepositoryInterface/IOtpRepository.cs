using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.RepositoryInterface
{
    public interface IOtpRepository
    {
        Task<string> RequestOtp(string email);
        Task<string> VerifyOtpWithToken(string otp);
    }
}
