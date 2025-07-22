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
        Unblocked = 4,
        PendingLicenseApproval = 5,
        NeedChanges =6,
        Rejected = 7
    }
    public enum SubVertical
    {
        Items = 1,
        Deals = 2,
        Stores = 3,
        Preloved = 4,
        Collectibles = 5,
        Services = 6
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
        Other = 45
    }
}
