using Dapr.Actors;
using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IPayToPublicActor
{
    public interface IPayToPublishActor : IActor
    {
        Task<bool> SetDataAsync(PayToPublishDto data, CancellationToken cancellationToken = default);
        Task<bool> FastSetDataAsync(PayToPublishDto data, CancellationToken cancellationToken = default);
        Task<PayToPublishDto?> GetDataAsync(CancellationToken cancellationToken = default);
    }
}
