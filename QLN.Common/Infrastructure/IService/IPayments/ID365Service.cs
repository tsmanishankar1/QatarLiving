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
        Task<List<string>> D365OrdersAsync(D365Order[] order, CancellationToken cancellationToken);
        Task<string> HandleD365OrderAsync(D365Order order, CancellationToken cancellationToken);
        Task<bool> CreateInterimSalesOrder(D365Data order, CancellationToken cancellationToken);

        Task<bool> CreateAndInvoiceSalesOrder(D365Data order, CancellationToken cancellationToken);
        Task SendPaymentInfoD365Async(D365Data data, CancellationToken cancellationToken);
    }
}
