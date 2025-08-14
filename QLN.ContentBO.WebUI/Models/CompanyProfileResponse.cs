using System.ComponentModel.DataAnnotations;
namespace QLN.ContentBO.WebUI.Models
{
    public class CompanyProfileResponse
    {
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public List<CompanyProfileItem> Items { get; set; } = new();
    }

    public class CompanyProfileItem
    {
        public Guid Id { get; set; }
        public long? OrderId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string PhoneNumberCountryCode { get; set; } = string.Empty;
        public List<string> BranchLocations { get; set; } = new();
        public string WhatsAppNumber { get; set; } = string.Empty;
        public string WhatsAppCountryCode { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string WebsiteUrl { get; set; } = string.Empty;
        public string FacebookUrl { get; set; } = string.Empty;
        public string InstagramUrl { get; set; } = string.Empty;
        public string CompanyLogo { get; set; } = string.Empty;
        public string StartDay { get; set; } = string.Empty;
        public string EndDay { get; set; } = string.Empty;
        public string StartHour { get; set; } = string.Empty;
        public string EndHour { get; set; } = string.Empty;
        public string UserDesignation { get; set; } = string.Empty;
        public string AuthorisedContactPersonName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime? CrExpiryDate { get; set; }
        public string CoverImage1 { get; set; } = string.Empty;
        public string CoverImage2 { get; set; } = string.Empty;
        public bool IsTherapeuticService { get; set; }
        public string TherapeuticCertificate { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public int CompanyType { get; set; }
        public int CompanySize { get; set; }
        public List<int> NatureOfBusiness { get; set; } = new();
        public string BusinessDescription { get; set; } = string.Empty;
        public long CrNumber { get; set; }
        public string CrDocument { get; set; } = string.Empty;
        public int? CompanyVerificationStatus { get; set; }
        public int Status { get; set; }
        public int Vertical { get; set; }
        public int SubVertical { get; set; }
        public string UserId { get; set; } = string.Empty;
        public bool? IsBasicProfile { get; set; }
        public bool IsActive { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? CreatedUtc { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTime? UpdatedUtc { get; set; }
    }
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
    public enum NatureOfBusiness
    {
        AgricultureAndFarming = 1,
        AutomotiveAndVehicles = 2,
        BankingAndFinancialServices = 3,
        BeautyAndPersonalCare = 4,
        ConstructionAndBuildingMaterials = 5,
        ConsumerElectronicsAndAppliances = 6,
        CreativeAndDesignServices = 7,
        EcommerceAndOnlineBusiness = 8,
        EducationAndTraining = 9,
        EnergyAndUtilities = 10,
        EngineeringAndIndustrialServices = 11,
        EventsAndEntertainment =  12,
        FashionAndApparel = 13,
        FoodAndBeverage = 14,
        FurnitureAndHomeDecor = 15,
        GovernmentAndPublicSector = 16,
        HealthcareAndMedicalServices = 17,
        Hospitality = 18,
        HumanResourcesAndStaffing = 19,
        ImportAndExport = 20,
        InformationTechnology = 21,
        InteriorDesignAndFitOut = 22,
        JewelryAndAccessories = 23,
        LegalAndComplianceServices = 24,
        LogisticsAndSupplyChain = 25,
        Manufacturing = 26,
        MarketingAndAdvertising = 27,
        MediaAndPublishing = 28,
        MiningAndMetals = 29,
        NonProfitAndNGOs = 30,
        OilAndGas = 31,
        PersonalServices = 32,
        PharmaceuticalsAndBiotechnology = 33,
        PrintingAndPublishing = 34,
        RealEstateAndPropertyManagement = 35,
        RecruitmentAndTalentServices = 36,
        Retail = 37,
        SecurityAndSurveillance = 38,
        Telecommunications = 39,
        TextilesAndGarments = 40,
        TransportAndTravelServices = 41,
        VeterinaryAndPetServices = 42,
        WasteManagementAndRecycling = 43,
        WholesaleAndDistribution = 44,
        Other = 9999
    }


}
