using QLN.Common.DTO_s;
using System.ComponentModel.DataAnnotations;


namespace QLN.Common.Infrastructure.DTO_s
{
    public class CompanyProfileDto : BaseCompanyDto
    {
        [Required]
        public string UserDesignation { get; set; }
    }
}