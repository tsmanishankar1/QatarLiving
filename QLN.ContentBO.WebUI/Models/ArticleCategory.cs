using System.ComponentModel.DataAnnotations;

namespace QLN.ContentBO.WebUI.Models
{
    public class ArticleCategory
    {
        [Required]
        public int CategoryId { get; set; }
        
        [Required]
        public int SubcategoryId { get; set; }

        /// <summary>
        /// Defaults to UnPublished Slot
        /// </summary>
        public int SlotId { get; set; }
    }
}
