using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.CustomException
{
    public class DaprServiceException : Exception
    {
        public int StatusCode { get; }
        public string ResponseBody { get; }

        public DaprServiceException(int statusCode, string responseBody)
            : base($"Upstream returned {(statusCode)}: {responseBody}")
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }
    }
}
