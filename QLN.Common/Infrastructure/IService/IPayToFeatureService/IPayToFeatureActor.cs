using Dapr.Actors;
using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IPayToFeatureActor
{
    public interface IPayToFeatureActor : IActor
    {
      
        Task<bool> SetDataAsync(PayToFeatureDto data, CancellationToken cancellationToken = default);
        Task<bool> FastSetDataAsync(PayToFeatureDto data, CancellationToken cancellationToken = default);
        Task<PayToFeatureDto?> GetDataAsync(CancellationToken cancellationToken = default);
        Task<bool> SetPayToFeatureDataAsync(PayToFeatureDataDto data, CancellationToken cancellationToken = default);
        Task<PayToFeatureDataDto?> GetPayToFeatureDataAsync(CancellationToken cancellationToken = default);
        Task<bool> SetDatasAsync(PayToFeatureBasicPriceDto data, CancellationToken cancellationToken = default);
        Task<PayToFeatureBasicPriceDto?> GetDatasAsync(CancellationToken cancellationToken = default);
        Task<bool> AddPlanIdAsync(Guid planId, CancellationToken cancellationToken = default);
        Task<List<Guid>> GetAllPlansAsync(CancellationToken cancellationToken = default);
        Task<bool> AddBasicPriceIdAsync(Guid basicPriceId, CancellationToken cancellationToken = default);
        Task<List<Guid>> GetAllBasicPriceIdsAsync(CancellationToken cancellationToken = default);
        Task<bool> AddPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken = default);
        Task<List<Guid>> GetAllPaymentIdsAsync(CancellationToken cancellationToken = default);
    }
}
