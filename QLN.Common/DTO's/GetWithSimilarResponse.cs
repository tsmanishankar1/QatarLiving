using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class GetWithSimilarResponse<T>
    {
        public T Detail { get; set; } = default!;
        public IEnumerable<T> Similar { get; set; } = Enumerable.Empty<T>();
        public int TotalSimilar => Similar.Count();
    }
}
