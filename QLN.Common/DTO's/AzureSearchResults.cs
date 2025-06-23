using Azure.Search.Documents.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class AzureSearchResults<T>
    {
        public List<T> Items { get; set; } = new();

        public long? TotalCount { get; set; }
    }
}
