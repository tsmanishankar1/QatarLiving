using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using static QLN.ContentBO.WebUI.Models.ClassifiedLanding;
using QLN.ContentBO.WebUI.Components;

namespace QLN.ContentBO.WebUI.Pages.Classified.Modal
{
    public class AddSeasonPickModalBase : QLComponentBase
    {
        [CascadingParameter]
        public IMudDialogInstance MudDialog { get; set; }

        [Parameter]
        public string Title { get; set; } = "Add Seasonal Pick";

        protected string SelectedCategory { get; set; } = string.Empty;
        protected string SelectedSubcategory { get; set; } = string.Empty;
        protected string SelectedSection { get; set; } = string.Empty;
        protected string ImagePreviewUrl { get; set; }
        protected ElementReference fileInput;

        protected readonly List<string> Categories = new()
        {
            "Business & Industrial",
    "Electronics",
    "Fashion & Beauty"
        };

        protected readonly Dictionary<string, List<string>> Subcategories = new()
        {
            { "Business & Industrial", new List<string> { "Machinery, Equipment & Materials" } },
    { "Electronics", new List<string>
        {
            "Cameras",
            "Computers, Software & Accessories",
            "Gaming",
            "Health & Personal Care",
            "Home appliances",
            "Home Entertainment",
            "Wearables"
        }
    },
    { "Fashion & Beauty", new List<string> { "Mens", "Travel", "Womens" } }
        };

        protected readonly Dictionary<string, List<string>> Sections = new()
        {
           // Business & Industrial
    { "Machinery, Equipment & Materials", new List<string>
        {
            "Commercial Equipments",
            "Construction Materials",
            "Heavy Duty Equipments",
            "Portable Building Structures",
            "Safety Equipment",
            "Tools"
        }
    },

    // Electronics
    { "Cameras", new List<string>
        {
            "Camera Accessories",
            "Cameras",
            "Lenses",
            "Video"
        }
    },
    { "Computers, Software & Accessories", new List<string>
        {
            "Computer Networking",
            "Desktops & Laptops",
            "Printers",
            "Keyboards, Mouse & Accessories",
            "Office Equipments",
            "Software & Graphics",
            "Speakers"
        }
    },
    { "Gaming", new List<string>
        {
            "Consoles",
            "Controllers",
            "Games",
            "Gaming Accessories"
        }
    },
    { "Health & Personal Care", new List<string> { "Medical Devices", "OTC Products" } },
    { "Home appliances", new List<string>
        {
            "ACs",
            "Cleaning Appliances",
            "Heaters",
            "Multipurpose Cookers",
            "Ovens, Stoves & Microwaves",
            "Refrigerators",
            "Small Appliances",
            "Washing Machines",
            "Water Dispensers & Purifiers"
        }
    },
    { "Home Entertainment", new List<string>
        {
            "CD/ DVD Players",
            "Projectors",
            "Remotes",
            "Satellite Dish & Receivers",
            "Speakers",
            "TVs"
        }
    },
    { "Wearables", new List<string>
        {
            "Headphones",
            "Mens smart watches",
            "Wearables Accesories",
            "Womens smart watches"
        }
    },

    // Fashion & Beauty
    { "Mens", new List<string>
        {
            "Mens Bags & Wallets",
            "Mens Clothing",
            "Mens Footwear",
            "Mens Personal care",
            "Mens  Accessories",
            "Mens Watches",
            "Perfumes"
        }
    },
    { "Travel", new List<string> { "Luggage & Backpacks", "Travel Accesories" } },
    { "Womens", new List<string>
        {
            "Jewelry",
            "Perfumes",
            "Womens Bags & Purses",
            "Womens Clothing",
            "Womens Footwear",
            "Womens Personal Care",
            "Womens Accessories",
            "Womens Watches"
        }
    }
        };

        protected List<string> GetSubcategories(string category)
        {
            return Subcategories.TryGetValue(category, out var subcats) ? subcats : new List<string>();
        }

        protected List<string> GetSections(string subcategory)
        {
            return Sections.TryGetValue(subcategory, out var sections) ? sections : new List<string>();
        }

        protected void OnFileChange(InputFileChangeEventArgs e)
        {
            var file = e.File;

            ImagePreviewUrl = "data:image;base64," + Convert.ToBase64String(new byte[100]);
        }

        protected bool IsFormValid()
        {
            return !string.IsNullOrEmpty(SelectedCategory) &&
                   !string.IsNullOrEmpty(SelectedSubcategory) &&
                   !string.IsNullOrEmpty(SelectedSection);
        }

        protected void Close() => MudDialog.Cancel();

        protected void Save()
        {
            var newItem = new LandingPageItem
            {
                Category = SelectedCategory,
                Subcategory = SelectedSubcategory,
                Section = SelectedSection,
                ImageUrl = ImagePreviewUrl,
                Title = $"{SelectedCategory} - {SelectedSubcategory}",
                EndDate = DateTime.Now.AddMonths(3)
            };

            MudDialog.Close(DialogResult.Ok(newItem));
        }
    }
}
