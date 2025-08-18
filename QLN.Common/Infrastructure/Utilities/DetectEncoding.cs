using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.Utilities
{
    public static class EncodingHelper
    {

       public static Encoding DetectEncoding(byte[] bytes)
        {
            if (bytes.Length >= 3 &&
                bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return Encoding.UTF8;

            if (bytes.Length >= 2)
            {
                if (bytes[0] == 0xFE && bytes[1] == 0xFF)
                    return Encoding.BigEndianUnicode; 
                if (bytes[0] == 0xFF && bytes[1] == 0xFE)
                    return Encoding.Unicode; 
            }

         
            return Encoding.UTF8;
        }

    }
    


}
