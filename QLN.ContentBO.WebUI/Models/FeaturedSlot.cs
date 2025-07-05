using System.ComponentModel.DataAnnotations;
using QLN.ContentBO.WebUI.Models;

namespace QLN.ContentBO.WebUI.Models
{
    public class FeaturedSlot
    {
        public int SlotNumber { get; set; }
        public EventDTO? Event { get; set; }
    }

}
