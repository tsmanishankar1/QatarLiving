using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{


    public class ReorderSlotRequestDto
    {
        [Required]
        public int CategoryId { get; set; }

        [Required]
        public int SubCategoryId { get; set; }

        [Required]
        [Range(1, 13)]
        public int FromSlot { get; set; }

        [Required]
        [Range(1, 13)]
        public int ToSlot { get; set; }

        public string? UserId { get; set; }

        public string? AuthorName { get; set; }
    }

    public class NewsSlotReorderRequest
    {
        public List<NewsSlotAssignment> SlotAssignments { get; set; } = [];
        public int CategoryId { get; set; }
        public int SubCategoryId { get; set; }
        public string? UserId { get; set; }
    }

    public class NewsSlotAssignment
    {
        public int SlotNumber { get; set; }
        public string? ArticleId { get; set; }

    }


}

