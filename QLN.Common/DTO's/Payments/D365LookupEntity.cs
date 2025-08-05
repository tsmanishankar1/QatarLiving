using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLN.Common.DTO_s.Payments
{

    public class D365LookupEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]
        public int Id { get; set; }

        [MaxLength(255)]
        public string D365ItemId { get; set; }

        [MaxLength(255)]
        public string Description { get; set; }

        [MaxLength(50)]
        public ProductType ProductType { get; set; }

        public int Duration { get; set; }

        public int Amount { get; set; }

        public int FeatureBudget { get; set; }

        public int PublishBudget { get; set; }

        public bool Status { get; set; }
    }
}

