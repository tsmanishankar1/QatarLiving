using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IPayToFeatureService
{
    public interface IPayToFeatureService
    {
        Task CreatePlanAsync(PayToFeatureRequestDto request, CancellationToken cancellationToken = default);
        Task<PayToFeaturePlansResponse> GetPlansByVerticalAndCategoryWithBasicPriceAsync(int verticalTypeId, int categoryId, CancellationToken cancellationToken = default);
        Task<List<PayToFeatureWithBasicPriceResponseDto>> GetAllPlansWithBasicPriceAsync(CancellationToken cancellationToken = default);
        Task<bool> UpdatePlanAsync(Guid id, PayToFeatureRequestDto request, CancellationToken cancellationToken = default);
        Task<bool> DeletePlanAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Guid> CreatePaymentsAsync(PayToFeaturePaymentRequestDto request, string userId, CancellationToken cancellationToken = default);
        Task<bool> HandlePaytoFeatureExpiryAsync(string  userId, Guid paymentId, CancellationToken cancellationToken = default);
        Task<bool> HandlePaytoFeatureExpiryAsync(string userId, CancellationToken cancellationToken = default);
        Task<PayToFeaturePaymentDto?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);
        Task<List<PayToFeaturePaymentDto>> GetActivePaymentsForUserAsync(string userId, CancellationToken cancellationToken = default);
        Task<List<PayToFeaturePaymentDto>> GetExpiredPaymentsForUserAsync(string userId, CancellationToken cancellationToken = default);
        Task<List<PayToFeaturePaymentDto>> GetPaymentsByUserIdAsync(string userId, CancellationToken cancellationToken = default);
        Task CreateBasicPriceAsync(PayToFeatureBasicPriceRequestDto request, CancellationToken cancellationToken = default);
        //Task<List<BasicPriceResponseDto>> GetBasicPricesByVerticalAndCategoryAsync(int verticalTypeId, int categoryId, CancellationToken cancellationToken = default);
    }
}


