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
        Task<PayToPublishListResponseDto> GetPlansByVerticalAndCategoryAsync(int verticalTypeId,int categoryId,CancellationToken cancellationToken = default);
        Task<List<PayToPublishResponseDto>> GetAllPlansAsync(CancellationToken cancellationToken = default);
        Task<bool> UpdatePlanAsync(Guid id, PayToPublishRequestDto request, CancellationToken cancellationToken = default);
        Task<bool> DeletePlanAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Guid> CreatePaymentsAsync(PaymentRequestDto request, Guid userId, CancellationToken cancellationToken = default);
        Task<bool> HandlePaytopyblishExpiryAsync(Guid userId, Guid paymentId, CancellationToken cancellationToken = default);
        Task<bool> HandlePaytopyblishExpiryAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<PaymentDto?> GetPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);
        Task<List<PaymentDto>> GetActivePaymentsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<List<PaymentDto>> GetExpiredPaymentsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<List<PaymentDto>> GetPaymentsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}


