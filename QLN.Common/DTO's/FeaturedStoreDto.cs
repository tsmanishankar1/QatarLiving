using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s
{
    public class FeaturedStoreDto
    {
        public string Vertical { get; set; }              
        public Guid? Id { get; set; }                     
        public string StoreId { get; set; }               
        public string StoreName { get; set; }             
        public string ImageUrl { get; set; }              
        public DateTime StartDate { get; set; }          
        public DateTime EndDate { get; set; }            
        public int? SlotOrder { get; set; }              
        public DateTime? CreatedAt { get; set; }         
        public DateTime? UpdatedAt { get; set; }         
        public string? UserId { get; set; }              
        public string? UserName { get; set; }             
        public bool? IsActive { get; set; } = true;       
    }
}
