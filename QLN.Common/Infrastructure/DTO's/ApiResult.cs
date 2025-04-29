using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.DTO_s
{
    public class ApiResult<T>
    {
        public HttpStatusCode StatusCode { get; set; }
        public T? Body { get; set; }
    }

}
