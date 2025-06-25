namespace QLN.Common.DTO_s
{
    public class ClassifiedPostDto
    {
        public string Id { get; set; }
        public string SubVertical { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Guid CategoryId { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string L2Category { get; set; }
        public string Section { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public decimal Price { get; set; }
        public string PriceType { get; set; }
        public string Condition { get; set; }
        public string Color { get; set; }
        public string Capacity { get; set; }
        public string Processor { get; set; }
        public string Coverage { get; set; }
        public string Ram { get; set; }
        public string Resolution { get; set; }
        public int BatteryPercentage { get; set; }
        public string Gender { get; set; }
        public string Size { get; set; }
        public string Storage { get; set; }
        public string SizeValue { get; set; }
        public string CertificateBase64 { get; set; }
        public string CertificateFileName { get; set; }
        public List<AdImageDto> AdImagesBase64 { get; set; }
        public string PhoneNumber { get; set; }
        public string WhatsAppNumber { get; set; }
        public string ContactEmail { get; set; }
        public DateTime ModifiedDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string CountryOfOrigin { get; set; }
        public string Language { get; set; }
        public string Zone { get; set; }
        public string StreetNumber { get; set; }
        public string BuildingNumber { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public List<string> Location { get; set; }
        public bool TearmsAndCondition { get; set; }
        public int Status { get; set; }
        public string AcceptsOffers { get; set; }
    }

    public class AdImageDto
    {
        public string AdImageFileNames { get; set; }
        public string Url { get; set; }
        public int Order { get; set; }
    }
}