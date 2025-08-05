using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Payments
{
    public class FatoraConfig
    {
        public string ApiUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;

        public string BaseUrl { get; set; } = string.Empty;
        public string SuccessPath { get; set; } = string.Empty;
        public string FailurePath { get; set; } = string.Empty;
    }
}
