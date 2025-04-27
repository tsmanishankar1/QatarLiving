using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.TokenProvider
{
    public class PhoneTokenProvider<TUser> : TotpSecurityStampBasedTokenProvider<TUser> where TUser : class
    {
        public override Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user)
        {
            return Task.FromResult(true);
        }
    }

    public class EmailTokenProvider<TUser> : TotpSecurityStampBasedTokenProvider<TUser> where TUser : class
    {
        public override Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user)
        {
            return Task.FromResult(true);
        }
    }
}
