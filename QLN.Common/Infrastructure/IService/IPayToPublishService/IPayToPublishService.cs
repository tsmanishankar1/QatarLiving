using QLN.Common.DTO_s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLN.Common.Infrastructure.IService.IPayToPublishService
{
    public interface IPayToPublishService
    {
        Task CreatePlanAsync(PayToPublishRequestDto request, CancellationToken cancellationToken = default);
        Task<PayToPublishPlansResponse> GetPlansByVerticalAndCategoryWithBasicPriceAsync(int verticalTypeId,int categoryId, CancellationToken cancellationToken = default);
        Task<List<PayToPublishWithBasicPriceResponseDto>> GetAllPlansWithBasicPriceAsync(CancellationToken cancellationToken = default);
        Task<bool> UpdatePlanAsync(Guid id, PayToPublishRequestDto request, CancellationToken cancellationToken = default);
        Task<bool> DeletePlanAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Guid> CreatePaymentsAsync(PaymentRequestDto request, string userId, CancellationToken cancellationToken = default);
        Task<bool> HandlePaytopyblishExpiryAsync(string userId, Guid paymentId, CancellationToken cancellationToken = default);
        Task<bool> HandlePaytopyblishExpiryAsync(string userId, CancellationToken cancellationToken = default);
        Task<PaymentDto?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);
        Task<List<PaymentDto>> GetActivePaymentsForUserAsync(string userId, CancellationToken cancellationToken = default);
        Task<List<PaymentDto>> GetExpiredPaymentsForUserAsync(string userId, CancellationToken cancellationToken = default);
        Task<List<PaymentDto>> GetPaymentsByUserIdAsync(string userId, CancellationToken cancellationToken = default);
        Task CreateBasicPriceAsync(BasicPriceRequestDto request, CancellationToken cancellationToken = default);
        //Task<List<BasicPriceResponseDto>> GetBasicPricesByVerticalAndCategoryAsync(int verticalTypeId, int categoryId, CancellationToken cancellationToken = default);
    }
}


