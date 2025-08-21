using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.DTO_s.Company
{
    public class UserSubscription
    {
        public string Id { get; set; }
        public int Vertical { get; set; }
        public int? SubVertical { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
