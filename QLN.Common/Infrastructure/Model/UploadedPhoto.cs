using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Model
{
    public class UploadedPhoto
    {
        public Guid Id { get; set; }
        public string Url { get; set; }
        public bool IsCoverPhoto { get; set; }
    }
}
