using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public enum CompanySize
    {
        [Display(Name = "0–10")]
        Size_0_10 = 1,

        [Display(Name = "11–50")]
        Size_11_50 = 2,

        [Display(Name = "51–200")]
        Size_51_200 = 3,

        [Display(Name = "201–500")]
        Size_201_500 = 4,

        [Display(Name = "500+")]
        Size_500_Plus = 5
    }

    public enum CompanyType
    {
        SME = 1,
        Enterprise = 2,
        MNC = 3,
        Government = 4
    }
    public enum CompanyStatus
    {
        Active = 1,
        Blocked = 2,
        Suspended = 3,
        Unblocked = 4
    }
    public enum Category
    {
        Preloved = 1,
        Deals = 2,
        Stores = 3,
        Services = 4
    }
}
