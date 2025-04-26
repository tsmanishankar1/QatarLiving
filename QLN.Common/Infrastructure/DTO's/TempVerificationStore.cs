using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.DTO_s
{
    public static class TempVerificationStore
    {
        public static Dictionary<string, string> EmailOtps = new();
        public static Dictionary<string, string> PhoneOtps = new();
        public static HashSet<string> VerifiedEmails = new();
        public static HashSet<string> VerifiedPhoneNumbers = new();
    }
}
