using System.ComponentModel.DataAnnotations;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Models
{
    public class ReportsFormDto
    {
        // ----------------------------
        // Category Selection
        // ----------------------------

        [Required(ErrorMessage = "Category is required.")]
        public string? SelectedCategoryId { get; set; }
        public string? SelectedSubcategoryId { get; set; }
        public string? SelectedSubSubcategoryId { get; set; }
        
        public Dictionary<string, List<string>> ItemFieldFilters { get; set; } = new();


    }
       

}
