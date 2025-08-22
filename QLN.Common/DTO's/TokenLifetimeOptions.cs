using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public sealed class TokenLifetimeOptions
    {
        public TimeSpan AccessToken { get; init; } = TimeSpan.FromHours(1);
        public TimeSpan RefreshToken { get; init; } = TimeSpan.FromHours(2);
    }
}
