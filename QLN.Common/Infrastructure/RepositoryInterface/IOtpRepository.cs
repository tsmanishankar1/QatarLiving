using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.RepositoryInterface
{
    public interface IOtpRepository
    {
        Task<string> RequestOtpAsync(string email);
        Task<string> VerifyOtpAsync(string otp);
    }

}
