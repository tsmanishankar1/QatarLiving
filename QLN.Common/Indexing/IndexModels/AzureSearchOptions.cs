using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Indexing.IndexModels
{
    public class AzureSearchOptions
    {
        public string ServiceEndpoint { get; set; } = string.Empty;
        public string AdminApiKey { get; set; } = string.Empty;
        public string UserIndexName { get; set; } = string.Empty;
    }
}
