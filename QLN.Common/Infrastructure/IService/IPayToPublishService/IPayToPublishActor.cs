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
        Task<bool> SetPayToPublishDataAsync(PayToPublishDataDto data, CancellationToken cancellationToken = default);
        Task<PayToPublishDataDto?> GetPayToPublishDataAsync(CancellationToken cancellationToken = default);
        Task<bool> SetDatasAsync(BasicPriceDto data, CancellationToken cancellationToken = default);
        Task<BasicPriceDto?> GetDatasAsync(CancellationToken cancellationToken = default);
        Task<bool> AddPlanIdAsync(Guid planId, CancellationToken cancellationToken = default);
        Task<List<Guid>> GetAllPlansAsync(CancellationToken cancellationToken = default);
        Task<bool> AddBasicPriceIdAsync(Guid basicPriceId, CancellationToken cancellationToken = default);
        Task<List<Guid>> GetAllBasicPriceIdsAsync(CancellationToken cancellationToken = default);
        Task<bool> AddPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken = default);
        Task<List<Guid>> GetAllPaymentIdsAsync(CancellationToken cancellationToken = default);
    }
}
