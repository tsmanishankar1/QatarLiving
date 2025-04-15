using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Indexing.IndexModels
{
    public class UserIndex
    {
        [SimpleField(IsKey = true, IsFilterable = true)]
        public string Id { get; set; }

        [SearchableField(IsFilterable = true)]
        public string Firstname { get; set; }

        [SearchableField(IsFilterable = true)]
        public string Lastname { get; set; }

        [SearchableField(IsFilterable = true)]
        public string Gender { get; set; }

        [SearchableField(IsFilterable = true)]
        public string Mobilenumber { get; set; }

        [SearchableField(IsFilterable = false)]
        public string Emailaddress { get; set; }

        [SearchableField(IsFilterable = true)]
        public string Nationality { get; set; }

        [SearchableField(IsFilterable = true, AnalyzerName = "en.lucene")]
        public string? Languagepreferences { get; set; }

        [SearchableField(IsFilterable = true, AnalyzerName = "en.lucene")]
        public string? Location { get; set; }
    }
}
