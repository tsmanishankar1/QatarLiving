//using QLN.Common.DTO_s;
//using QLN.Common.Infrastructure.Subscriptions;
//using System;
//using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;
//using System.Text.Json.Serialization;


//namespace QLN.Common.Infrastructure.Model
//{
   
//        public class ServicesCategory
//        {
//            [Key]
//            public long Id { get; set; }

//            [Required]
//                public string Category { get; set; } = default!;
//                public Vertical Vertical { get; set; }
//                public SubVertical SubVertical { get; set; }
//                public long parentId { get; set; }
//                public List<L1Category>? L1Categories { get; set; } = new();
      
//            public List<Services>? Services { get; set; } = new(); 
//        }
//       public class L1Category
//       {
//        [Key]
//        public Guid Id { get; set; }

//        [Required]
//        public string Name { get; set; } = default!;

//        public Guid ServicesCategoryId { get; set; } 
//        public ServicesCategory ServicesCategory { get; set; } = default!;

     

//        public List<Services>? Services { get; set; } = new(); 
//       }
//    public class L2Category
//    {
//        [Key]
//        public Guid Id { get; set; }

//        [Required]
//        public string Name { get; set; } = default!;

//        public Guid L1CategoryId { get; set; } 
//        [ForeignKey(nameof(L1CategoryId))]
//        public L1Category L1Category { get; set; } = default!;

//        public List<Services>? Services { get; set; } = new(); 
//    }


//}

