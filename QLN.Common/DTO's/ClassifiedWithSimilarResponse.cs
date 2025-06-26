using System.Collections.Generic;
using QLN.Common.DTO_s;

namespace QLN.Common.DTO_s
{
    public class ClassifiedWithSimilarResponse
    {
        public ClassifiedsIndex Detail { get; set; } = new();
        public List<ClassifiedsIndex> Similar { get; set; } = new();
        public int TotalSimilar { get; set; }
    }
}
