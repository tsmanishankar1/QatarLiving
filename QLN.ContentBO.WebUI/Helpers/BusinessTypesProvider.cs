using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Helpers
{
    public class BusinessTypesProvider
    {
        private static readonly List<BusinessType> _businessTypes = new()
        {
            new BusinessType { Id = 1, NameOfBusiness = "Agriculture And Farming" },
            new BusinessType { Id = 2, NameOfBusiness = "Automotive And Vehicles" },
            new BusinessType { Id = 3, NameOfBusiness = "Banking And Financial Services" },
            new BusinessType { Id = 4, NameOfBusiness = "Beauty And Personal Care" },
            new BusinessType { Id = 5, NameOfBusiness = "Construction And Building Materials" },
            new BusinessType { Id = 6, NameOfBusiness = "Consumer Electronics And Appliances" },
            new BusinessType { Id = 7, NameOfBusiness = "Creative And Design Services" },
            new BusinessType { Id = 8, NameOfBusiness = "Ecommerce And Online Business" },
            new BusinessType { Id = 9, NameOfBusiness = "Education And Training" },
            new BusinessType { Id = 10, NameOfBusiness = "Energy And Utilities" },
            new BusinessType { Id = 11, NameOfBusiness = "Engineering And Industrial Services" },
            new BusinessType { Id = 12, NameOfBusiness = "Events And Entertainment" },
            new BusinessType { Id = 13, NameOfBusiness = "Fashion And Apparel" },
            new BusinessType { Id = 14, NameOfBusiness = "Food And Beverage" },
            new BusinessType { Id = 15, NameOfBusiness = "Furniture And Home Decor" },
            new BusinessType { Id = 16, NameOfBusiness = "Government And Public Sector" },
            new BusinessType { Id = 17, NameOfBusiness = "Healthcare And Medical Services" },
            new BusinessType { Id = 18, NameOfBusiness = "Hospitality" },
            new BusinessType { Id = 19, NameOfBusiness = "Human Resources And Staffing" },
            new BusinessType { Id = 20, NameOfBusiness = "Import And Export" },
            new BusinessType { Id = 21, NameOfBusiness = "Information Technology" },
            new BusinessType { Id = 22, NameOfBusiness = "Interior Design And Fit Out" },
            new BusinessType { Id = 23, NameOfBusiness = "Jewelry And Accessories" },
            new BusinessType { Id = 24, NameOfBusiness = "Legal And Compliance Services" },
            new BusinessType { Id = 25, NameOfBusiness = "Logistics And Supply Chain" },
            new BusinessType { Id = 26, NameOfBusiness = "Manufacturing" },
            new BusinessType { Id = 27, NameOfBusiness = "Marketing And Advertising" },
            new BusinessType { Id = 28, NameOfBusiness = "Media And Publishing" },
            new BusinessType { Id = 29, NameOfBusiness = "Mining And Metals" },
            new BusinessType { Id = 30, NameOfBusiness = "Non Profit And NGOs" },
            new BusinessType { Id = 31, NameOfBusiness = "Oil And Gas" },
            new BusinessType { Id = 32, NameOfBusiness = "Personal Services" },
            new BusinessType { Id = 33, NameOfBusiness = "Pharmaceuticals And Biotechnology" },
            new BusinessType { Id = 34, NameOfBusiness = "Printing And Publishing" },
            new BusinessType { Id = 35, NameOfBusiness = "Real Estate And Property Management" },
            new BusinessType { Id = 36, NameOfBusiness = "Recruitment And Talent Services" },
            new BusinessType { Id = 37, NameOfBusiness = "Retail" },
            new BusinessType { Id = 38, NameOfBusiness = "Security And Surveillance" },
            new BusinessType { Id = 39, NameOfBusiness = "Telecommunications" },
            new BusinessType { Id = 40, NameOfBusiness = "Textiles And Garments" },
            new BusinessType { Id = 41, NameOfBusiness = "Transport And Travel Services" },
            new BusinessType { Id = 42, NameOfBusiness = "Veterinary And Pet Services" },
            new BusinessType { Id = 43, NameOfBusiness = "Waste Management And Recycling" },
            new BusinessType { Id = 44, NameOfBusiness = "Wholesale And Distribution" },
            new BusinessType { Id = 9999, NameOfBusiness = "Other" }
        };

        public static List<BusinessType> GetAll() => _businessTypes;
    }
}
