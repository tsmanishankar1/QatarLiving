using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Model
{
    public class PrelovedAd : AdBase
    {               
        public string? Gender { get; set; }       
        public string? Size { get; set; }         
        public string? Colour { get; set; }       
        public string? Inclusion { get; set; }
    }
}
