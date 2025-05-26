using Dapr.Actors;
using QLN.Common.DTO_s;
using QLN.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IPayToPublishService
{
    public interface IPaymentActor : IActor
    {
        Task<bool> FastSetDataAsync(PaymentDto data, CancellationToken cancellationToken = default);
        Task<bool> SetDataAsync(PaymentDto data, CancellationToken cancellationToken = default);
        Task<PaymentDto?> GetDataAsync(CancellationToken cancellationToken = default);
    }
}
