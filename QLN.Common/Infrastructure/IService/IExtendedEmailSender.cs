using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService
{
    public interface IExtendedEmailSender<TUser> : IEmailSender<TUser>
       where TUser : class
    {
        Task SendTwoFactorCode(TUser user, string email, string code);
        Task SendOtpEmailAsync(string email, string otp);
    }
}
