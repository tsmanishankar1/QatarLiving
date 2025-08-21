using QLN.Common.DTO_s.Classifieds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class AdCreatedResponseDto
    {
        public long AdId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Message { get; set; }
    }

    public enum SaveIntent
    {
        SaveAsDraft = 1,
        SaveAndSubmitForApproval = 2
    }

    public class ClassifiedsRequest
    {
        public ClassifiedsItemsDTO Items { get; set; }
        public SaveIntent Intent { get; set; }
    }
}
