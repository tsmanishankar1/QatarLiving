using QLN.Common.DTO_s.Payments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IPayments
{
    public interface ID365Service
    {
        Task<string> HandleD365OrderAsync(D365Order order);

    }
}
