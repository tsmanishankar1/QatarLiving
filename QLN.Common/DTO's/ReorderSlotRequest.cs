using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{


    public class ArticleSlotUpdateDTO
    {
        public Guid ArticleId { get; set; }

        public int CategoryId { get; set; }

        public int SubCategoryId { get; set; }

        public int FromSlot { get; set; }

        public int ToSlot { get; set; }

        public bool Confirm { get; set; }

        public string UserId { get; set; }

        public string AuthorName { get; set; }
    }


}

