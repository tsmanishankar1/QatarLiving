using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class CreateNewsArticleResponseDto
{
    public Guid ArticleId { get; set; }
    public string Message { get; set; }
    public List<AssignedSlotDto> AssignedSlots { get; set; } = new();
    public List<SlotShiftDto> SlotShifts { get; set; } = new();
}

public class AssignedSlotDto
{
    public int CategoryId { get; set; }
    public int SubCategoryId { get; set; }
    public string Slot { get; set; }
}

public class SlotShiftDto
{
    public string From { get; set; }
    public string To { get; set; }
}

}
