using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class DeleteAdResponseDto
    {
        public string Message { get; set; }         
        public List<string> DeletedImages { get; set; } = new();
    }
}
