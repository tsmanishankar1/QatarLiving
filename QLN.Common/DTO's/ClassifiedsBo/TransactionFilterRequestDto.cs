using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.ClassifiedsBo
{
    public class TransactionFilterRequestDto
    {
        public string SubVertical { get; set; } = string.Empty;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public string? SearchText { get; set; }
        public string? TransactionType { get; set; }
        public string? DateCreated { get; set; }
        public string? DatePublished { get; set; }
        public string? DateStart { get; set; }
        public string? DateEnd { get; set; }
        public string? Status { get; set; }
        public string? PaymentMethod { get; set; }
        public string SortBy { get; set; } = "CreationDate";
        public string SortOrder { get; set; } = "desc";
    }

}
