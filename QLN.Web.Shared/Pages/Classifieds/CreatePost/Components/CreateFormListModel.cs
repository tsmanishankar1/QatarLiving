using System.ComponentModel.DataAnnotations;

public class CreateFormListModel
{
    public string Category { get; set; }

    public string Subcategory { get; set; }
    public string Brand { get; set; }
    public string Model { get; set; }

    public string Title { get; set; }

    public string Description { get; set; }

    public string Color { get; set; }
    public string Capacity { get; set; }
    public string Processor { get; set; }
    public string Coverage { get; set; }
    public string Ram { get; set; }
    public string Resolution { get; set; }

    [Range(0, 100, ErrorMessage = "Battery must be between 0 and 100.")]
    public int? BatteryPercentage { get; set; }

    public string Size { get; set; }
    public string Certificate { get; set; }

    [Required(ErrorMessage = "Phone number is required.")]
    public string Phone { get; set; }

    [Required(ErrorMessage = "WhatsApp number is required.")]
    public string WhatsApp { get; set; }

    public string Zone { get; set; }
    public string StreetNumber { get; set; }
    public string BuildingNumber { get; set; }
}
